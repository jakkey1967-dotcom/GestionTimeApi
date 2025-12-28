using BCrypt.Net;
using GestionTime.Api.Contracts.Auth;
using GestionTime.Api.Security;
using GestionTime.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GestionTime.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(
    GestionTimeDbContext db, 
    JwtService jwt, 
    RefreshTokenService refreshSvc,
    ILogger<AuthController> logger) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var email = (req.Email ?? "").Trim().ToLowerInvariant();
        logger.LogInformation("Intento de login para {Email}", email);

        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .SingleOrDefaultAsync(u => u.Email == email);

        if (user is null || !user.Enabled)
        {
            logger.LogWarning("Login fallido para {Email}: usuario no encontrado o deshabilitado", email);
            return Unauthorized(new { message = "Credenciales inválidas" });
        }

        bool ok;
        try
        {
            ok = BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash);
        }
        catch (BCrypt.Net.SaltParseException ex)
        {
            logger.LogError(ex, "Error de BCrypt al verificar password para {Email}", email);
            ok = false;
        }

        if (!ok)
        {
            logger.LogWarning("Login fallido para {Email}: contraseña incorrecta", email);
            return Unauthorized(new { message = "Credenciales inválidas" });
        }

        // ✅ VERIFICAR SI DEBE CAMBIAR CONTRASEÑA
        if (user.ShouldChangePassword)
        {
            logger.LogInformation(" Usuario {Email} debe cambiar contraseña - MustChange: {MustChange}, IsExpired: {IsExpired}", 
                email, user.MustChangePassword, user.IsPasswordExpired);
            
            return Ok(new 
            { 
                message = "password_change_required",
                mustChangePassword = true,
                passwordExpired = user.IsPasswordExpired,
                daysUntilExpiration = user.DaysUntilPasswordExpires,
                userName = !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : user.Email?.Split('@')[0] ?? "Usuario"
            });
        }

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToArray();

        // Access token (JWT)
        var accessToken = jwt.CreateAccessToken(user.Id, user.Email, roles);
        logger.LogDebug("Token de acceso generado para {UserId}", user.Id);

        // Refresh token (raw + hash)
        var (rawRefresh, refreshHash, refreshExpires) = refreshSvc.Create();

        db.RefreshTokens.Add(new GestionTime.Domain.Auth.RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshHash,
            ExpiresAt = refreshExpires,
            RevokedAt = null
        });

        await db.SaveChangesAsync();

        SetAccessCookie(accessToken);
        SetRefreshCookie(rawRefresh, refreshExpires);

        logger.LogInformation("Login exitoso para {Email} (UserId: {UserId}, Roles: {Roles})", 
            email, user.Id, string.Join(", ", roles));

        // Obtener rol (primer rol o "Usuario" por defecto)
        var role = roles.FirstOrDefault() ?? "Usuario";
        
        // Obtener nombre del usuario
        var userName = !string.IsNullOrWhiteSpace(user.FullName) 
            ? user.FullName 
            : user.Email?.Split('@')[0] ?? "Usuario";

        return Ok(new 
        { 
            message = "ok",
            userName = userName,
            userEmail = user.Email,
            userRole = role,
            mustChangePassword = false,
            daysUntilPasswordExpires = user.DaysUntilPasswordExpires
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        if (!Request.Cookies.TryGetValue("refresh_token", out var rawRefresh) || string.IsNullOrWhiteSpace(rawRefresh))
        {
            logger.LogWarning("Intento de refresh sin token");
            return Unauthorized(new { message = "No refresh token" });
        }

        var hash = RefreshTokenService.Hash(rawRefresh);
        logger.LogDebug("Procesando refresh token (hash: {HashPrefix}...)", hash[..8]);

        var token = await db.RefreshTokens
            .Include(t => t.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
            .SingleOrDefaultAsync(t => t.TokenHash == hash);

        if (token is null || token.RevokedAt != null || token.ExpiresAt <= DateTime.UtcNow || !token.User.Enabled)
        {
            logger.LogWarning("Refresh token inválido, revocado o expirado");
            return Unauthorized(new { message = "Refresh inválido" });
        }

        // Rotación: revoca el antiguo
        token.RevokedAt = DateTime.UtcNow;

        var roles = token.User.UserRoles.Select(ur => ur.Role.Name).ToArray();
        var newAccess = jwt.CreateAccessToken(token.User.Id, token.User.Email, roles);

        var (newRawRefresh, newHash, newRefreshExpires) = refreshSvc.Create();

        db.RefreshTokens.Add(new GestionTime.Domain.Auth.RefreshToken
        {
            UserId = token.User.Id,
            TokenHash = newHash,
            ExpiresAt = newRefreshExpires,
            RevokedAt = null
        });

        await db.SaveChangesAsync();

        SetAccessCookie(newAccess);
        SetRefreshCookie(newRawRefresh, newRefreshExpires);

        logger.LogInformation("Token refrescado exitosamente para {UserId}", token.User.Id);

        return Ok(new { message = "ok" });
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? "unknown";
        var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray();
        
        logger.LogDebug("Consulta /me para {Email}", email);
        
        return Ok(new MeResponse(email, roles));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        logger.LogInformation("Logout solicitado{UserInfo}", 
            userId != null ? $" por UserId: {userId}" : "");

        // Revoca el refresh actual (si existe)
        if (Request.Cookies.TryGetValue("refresh_token", out var rawRefresh) && !string.IsNullOrWhiteSpace(rawRefresh))
        {
            var hash = RefreshTokenService.Hash(rawRefresh);

            var token = await db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash);
            if (token is not null && token.RevokedAt == null)
            {
                token.RevokedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
                logger.LogDebug("Refresh token revocado");
            }
        }

        Response.Cookies.Delete("access_token");
        Response.Cookies.Delete("refresh_token", new CookieOptions
        {
            Path = "/api/v1/auth/refresh"
        });

        logger.LogInformation("Logout completado");

        return Ok(new { message = "bye" });
    }

    private void SetAccessCookie(string jwtToken)
    {
        Response.Cookies.Append("access_token", jwtToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.AddMinutes(15),
            Path = "/"
        });
    }

    private void SetRefreshCookie(string refreshRaw, DateTime refreshExpiresUtc)
    {
        Response.Cookies.Append("refresh_token", refreshRaw, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = new DateTimeOffset(refreshExpiresUtc),
            Path = "/api/v1/auth/refresh"
        });
    }


    // ========================================
    // RECUPERACIÓN DE CONTRASEÑA
    // ========================================
    // 
    // IMPORTANTE: Este NO es un archivo compilable.
    // Es solo código para COPIAR y PEGAR en AuthController.cs
    //
    // INSTRUCCIONES:
    // 1. Copia TODO este archivo (Ctrl+A, Ctrl+C)
    // 2. Abre: C:\GestionTime\src\GestionTime.Api\Controllers\AuthController.cs
    // 3. Ve al final del archivo (después de SetRefreshCookie, antes del último })
    // 4. Pega el código (Ctrl+V)
    // 5. Guarda (Ctrl+S)
    // ========================================

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest req,
        [FromServices] Services.ResetTokenService resetTokenSvc,
        [FromServices] Services.IEmailService emailSvc)
    {
        var email = (req.Email ?? "").Trim().ToLowerInvariant();
        logger.LogInformation("Solicitud de recuperación de contraseña para {Email}", email);

        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email);

        if (user is null || !user.Enabled)
        {
            // Por seguridad, siempre responder éxito
            logger.LogWarning("Solicitud de recuperación para email no existente o deshabilitado: {Email}", email);
            return Ok(new ForgotPasswordResponse(
                true, 
                "Si el email existe, recibirás un código de verificación.", 
                null
            ));
        }

        // Generar código de 6 dígitos
        var token = resetTokenSvc.GenerateToken();
        
        // Guardar en caché
        resetTokenSvc.SaveToken(token, user.Id);
        
        logger.LogInformation("Código de recuperación generado para {Email}: {Token}", email, token);

        // Enviar email
        try
        {
            await emailSvc.SendPasswordResetEmailAsync(user.Email, token);
            logger.LogInformation("Email de recuperación enviado a {Email}", email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al enviar email de recuperación a {Email}", email);
            return StatusCode(500, new ForgotPasswordResponse(
                false,
                null,
                "Error al enviar el correo. Intenta nuevamente."
            ));
        }

        return Ok(new ForgotPasswordResponse(
            true,
            "Código de verificación enviado a tu correo.",
            null
        ));
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ForgotPasswordResetRequest req,
        [FromServices] Services.ResetTokenService resetTokenSvc)
    {
        var token = (req.Token ?? "").Trim();
        var email = (req.Email ?? "").Trim().ToLowerInvariant();
        var newPassword = req.NewPassword ?? "";

        logger.LogInformation("Intento de reset de contraseña para {Email} con token {TokenPrefix}...", 
            email, token.Length > 3 ? token.Substring(0, 3) : "***");

        // Validar token
        var userId = resetTokenSvc.ValidateAndGetUserId(token);

        if (userId == null)
        {
            logger.LogWarning("Token inválido o expirado: {Token}", token);
            return BadRequest(new ForgotPasswordResponse(
                false,
                null,
                "Código inválido o expirado."
            ));
        }

        // Validar usuario
        var user = await db.Users.FindAsync(userId.Value);

        if (user is null || user.Email != email || !user.Enabled)
        {
            logger.LogWarning("Usuario no encontrado, email no coincide o deshabilitado para UserId: {UserId}", userId);
            return BadRequest(new ForgotPasswordResponse(
                false,
                null,
                "Código inválido."
            ));
        }

        // Validar longitud de contraseña
        if (newPassword.Length < 6)
        {
            return BadRequest(new ForgotPasswordResponse(
                false,
                null,
                "La contraseña debe tener al menos 6 caracteres."
            ));
        }

        // Actualizar contraseña
        try
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await db.SaveChangesAsync();

            // Eliminar token usado
            resetTokenSvc.RemoveToken(token);

            logger.LogInformation("Contraseña reseteada exitosamente para {Email} (UserId: {UserId})", 
                email, user.Id);

            return Ok(new ForgotPasswordResponse(
                true,
                "Contraseña actualizada correctamente.",
                null
            ));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al actualizar contraseña para {Email}", email);
            return StatusCode(500, new ForgotPasswordResponse(
                false,
                null,
                "Error al actualizar la contraseña."
            ));
        }
    }



    // ========================================
    // REGISTRO DE USUARIOS - COPIAR ESTE CÓDIGO
    // Agregar al final de AuthController.cs (antes del cierre de clase)
    // ========================================

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest req,
        [FromServices] Services.EmailVerificationTokenService tokenService,
        [FromServices] Services.IEmailService emailSvc)
    {
        var email = (req.Email ?? "").Trim().ToLowerInvariant();
        var fullName = (req.FullName ?? "").Trim();
        var password = req.Password ?? "";

        logger.LogInformation("Solicitud de registro para {Email}", email);

        // Validaciones
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(password))
        {
            return BadRequest(new RegisterResponse(false, null, "Todos los campos son requeridos."));
        }

        var existingUser = await db.Users.SingleOrDefaultAsync(u => u.Email == email);

        if (existingUser != null)
        {
            logger.LogWarning("Email ya registrado: {Email}", email);
            return BadRequest(new RegisterResponse(false, null, "El email ya está registrado."));
        }

        try
        {
            // ✅ CREAR USUARIO INMEDIATAMENTE (pero sin confirmar email)
            var newUser = new GestionTime.Domain.Auth.User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                FullName = fullName,
                Enabled = true,
                EmailConfirmed = false  // Requiere activación por email
            };

            db.Users.Add(newUser);

            // Asignar rol de usuario
            var userRole = await db.Roles.SingleOrDefaultAsync(r => r.Name == "User");
            if (userRole != null)
            {
                db.UserRoles.Add(new GestionTime.Domain.Auth.UserRole
                {
                    UserId = newUser.Id,
                    RoleId = userRole.Id
                });
            }

            await db.SaveChangesAsync();

            logger.LogInformation("✅ Usuario creado exitosamente (sin activar): {Email}", email);

            // ✅ GENERAR TOKEN DE ACTIVACIÓN SEGURO
            var activationToken = tokenService.GenerateVerificationToken(newUser.Id, email);
            
            logger.LogInformation("Token de activación generado para {Email}", email);

            // ✅ ENVIAR EMAIL DE ACTIVACIÓN
            try
            {
                await emailSvc.SendActivationEmailAsync(newUser, activationToken);
                logger.LogInformation("📧 Email de activación enviado a {Email}", email);

                return Ok(new RegisterResponse(
                    true, 
                    "Registro exitoso. Revisa tu email para activar tu cuenta.", 
                    null
                ));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "⚠️ Error enviando email de activación (usuario creado)");
                
                return Ok(new RegisterResponse(
                    true, 
                    "Usuario registrado. Error enviando email de verificación. Contacta con soporte si no recibes el email.", 
                    null
                ));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Error creando usuario");
            return StatusCode(500, new RegisterResponse(false, null, "Error al crear el usuario."));
        }
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] VerifyEmailRequest req,
        [FromServices] Services.ResetTokenService tokenSvc)
    {
        var token = (req.Token ?? "").Trim();
        var email = (req.Email ?? "").Trim().ToLowerInvariant();

        logger.LogInformation("Verificación de email: {Email}", email);

        // ✅ VALIDAR TOKEN
        var storedToken = tokenSvc.GetToken($"verify:{email}");

        if (storedToken == null || storedToken != token)
        {
            logger.LogWarning("Token inválido o expirado para {Email}", email);
            return BadRequest(new { success = false, message = "Código inválido o expirado." });
        }

        // ✅ BUSCAR USUARIO (ya debe existir porque se creó en /register)
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            logger.LogWarning("Usuario no encontrado: {Email}", email);
            return NotFound(new { success = false, message = "Usuario no encontrado." });
        }

        if (user.EmailConfirmed)
        {
            logger.LogInformation("Email ya verificado: {Email}", email);
            return Ok(new { success = true, message = "Email ya verificado." });
        }

        try
        {
            // ✅ MARCAR EMAIL COMO VERIFICADO
            user.EmailConfirmed = true;
            await db.SaveChangesAsync();

            // Eliminar token usado
            tokenSvc.RemoveToken($"verify:{email}");

            logger.LogInformation("✅ Email verificado exitosamente: {Email}", email);

            return Ok(new { success = true, message = "Email verificado exitosamente. Ya puedes iniciar sesión." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Error verificando email");
            return StatusCode(500, new { success = false, message = "Error al verificar email." });
        }
    }

    // Cerrar la clase AuthController

    // ========================================
    // CAMBIO OBLIGATORIO DE CONTRASEÑA
    // ========================================
    
    [HttpPost("change-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        var email = (req.Email ?? "").Trim().ToLowerInvariant();
        var currentPassword = req.CurrentPassword ?? "";
        var newPassword = req.NewPassword ?? "";

        logger.LogInformation("Solicitud de cambio de contraseña para {Email}", email);

        // Validaciones básicas
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
        {
            return BadRequest(new ChangePasswordResponse
            {
                Success = false,
                Error = "Todos los campos son requeridos"
            });
        }

        if (newPassword.Length < 6)
        {
            return BadRequest(new ChangePasswordResponse
            {
                Success = false,
                Error = "La nueva contraseña debe tener al menos 6 caracteres"
            });
        }

        if (currentPassword == newPassword)
        {
            return BadRequest(new ChangePasswordResponse
            {
                Success = false,
                Error = "La nueva contraseña debe ser diferente a la actual"
            });
        }

        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email);

        if (user is null || !user.Enabled)
        {
            logger.LogWarning("Cambio de contraseña fallido para {Email}: usuario no encontrado o deshabilitado", email);
            return Unauthorized(new ChangePasswordResponse
            {
                Success = false,
                Error = "Usuario no encontrado"
            });
        }

        // Verificar contraseña actual
        bool currentPasswordValid;
        try
        {
            currentPasswordValid = BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash);
        }
        catch (BCrypt.Net.SaltParseException ex)
        {
            logger.LogError(ex, "Error de BCrypt al verificar contraseña actual para {Email}", email);
            currentPasswordValid = false;
        }

        if (!currentPasswordValid)
        {
            logger.LogWarning("Cambio de contraseña fallido para {Email}: contraseña actual incorrecta", email);
            return Unauthorized(new ChangePasswordResponse
            {
                Success = false,
                Error = "Contraseña actual incorrecta"
            });
        }

        try
        {
            // Actualizar contraseña y campos de control
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.PasswordChangedAt = DateTime.UtcNow;
            user.MustChangePassword = false;

            await db.SaveChangesAsync();

            logger.LogInformation("Contraseña cambiada exitosamente para {Email} (UserId: {UserId})", 
                email, user.Id);

            return Ok(new ChangePasswordResponse
            {
                Success = true,
                Message = "Contraseña actualizada correctamente"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al cambiar contraseña para {Email}", email);
            return StatusCode(500, new ChangePasswordResponse
            {
                Success = false,
                Error = "Error interno del servidor"
            });
        }
    }

    [HttpPost("force-password-change")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> ForcePasswordChange([FromBody] ForcePasswordChangeRequest req)
    {
        var targetEmail = (req.Email ?? "").Trim().ToLowerInvariant();
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        logger.LogInformation("Admin {AdminId} solicita cambio forzado de contraseña para {TargetEmail}", 
            adminId, targetEmail);

        var targetUser = await db.Users.SingleOrDefaultAsync(u => u.Email == targetEmail);

        if (targetUser is null)
        {
            return NotFound(new ChangePasswordResponse
            {
                Success = false,
                Error = "Usuario no encontrado"
            });
        }

        try
        {
            targetUser.MustChangePassword = true;
            await db.SaveChangesAsync();

            logger.LogInformation("Cambio de contraseña forzado activado para {TargetEmail} por admin {AdminId}", 
                targetEmail, adminId);

            return Ok(new ChangePasswordResponse
            {
                Success = true,
                Message = $"Usuario {targetEmail} debe cambiar su contraseña en el próximo login"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al forzar cambio de contraseña para {TargetEmail}", targetEmail);
            return StatusCode(500, new ChangePasswordResponse
            {
                Success = false,
                Error = "Error interno del servidor"
            });
        }
    }

    // ========================================
    // ACTIVACIÓN POR ENLACE DE EMAIL
    // ========================================
    
    [HttpGet("activate/{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> ActivateAccount(
        string token,
        [FromServices] Services.EmailVerificationTokenService tokenService)
    {
        logger.LogInformation("Solicitud de activación con token: {TokenPrefix}...", token?.Substring(0, 8) ?? "null");

        if (string.IsNullOrEmpty(token))
        {
            return Content(GenerateActivationResultPage(false, "Token inválido", "El enlace de activación no es válido."), "text/html");
        }

        try
        {
            // Validar token
            var tokenData = tokenService.ValidateVerificationToken(token);
            
            if (tokenData == null)
            {
                logger.LogWarning("Token de activación inválido o expirado: {Token}", token);
                return Content(GenerateActivationResultPage(false, "Enlace expirado", 
                    "Este enlace de activación ha expirado o no es válido. Por favor, solicita un nuevo enlace desde la aplicación."), "text/html");
            }

            // Buscar usuario
            var user = await db.Users.SingleOrDefaultAsync(u => u.Id == tokenData.UserId && u.Email == tokenData.Email);
            
            if (user == null)
            {
                logger.LogWarning("Usuario no encontrado para token de activación: UserId={UserId}, Email={Email}", 
                    tokenData.UserId, tokenData.Email);
                return Content(GenerateActivationResultPage(false, "Usuario no encontrado", 
                    "No se pudo encontrar el usuario asociado a este enlace."), "text/html");
            }

            if (user.EmailConfirmed)
            {
                logger.LogInformation("Usuario ya activado: {Email}", user.Email);
                // Consumir token aunque ya esté activado
                tokenService.ConsumeToken(token);
                return Content(GenerateActivationResultPage(true, "¡Cuenta ya activa!", 
                    $"Tu cuenta {user.Email} ya estaba activada. Puedes iniciar sesión normalmente."), "text/html");
            }

            // Activar usuario
            user.EmailConfirmed = true;
            await db.SaveChangesAsync();

            // Consumir token
            tokenService.ConsumeToken(token);

            logger.LogInformation("✅ Usuario activado exitosamente: {Email} (UserId: {UserId})", user.Email, user.Id);

            return Content(GenerateActivationResultPage(true, "¡Cuenta activada exitosamente!", 
                $"Tu cuenta {user.Email} ha sido activada. Ya puedes iniciar sesión en la aplicación GestionTime."), "text/html");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error activando cuenta con token: {Token}", token);
            return Content(GenerateActivationResultPage(false, "Error del servidor", 
                "Ocurrió un error al activar tu cuenta. Por favor, intenta nuevamente más tarde."), "text/html");
        }
    }

    /// <summary>
    /// Genera una página HTML de resultado de activación
    /// </summary>
    private string GenerateActivationResultPage(bool success, string title, string message)
    {
        var statusClass = success ? "success" : "error";
        var icon = success ? "✅" : "❌";
        var buttonText = success ? "Abrir GestionTime" : "Intentar nuevamente";
        var buttonAction = success ? "window.open('gestiontime://login', '_self'); window.close();" : "window.history.back();";
        var autoClose = success ? "setTimeout(() => { window.close(); }, 5000);" : "";

        return $@"<!DOCTYPE html>
<html lang=""es"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{title} - GestionTime</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            margin: 0;
            padding: 0;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
        }}
        .container {{
            background: white;
            border-radius: 15px;
            padding: 40px;
            max-width: 500px;
            text-align: center;
            box-shadow: 0 10px 30px rgba(0,0,0,0.2);
        }}
        .logo {{
            max-width: 200px;
            width: 200px;
            height: auto;
            margin: 0 auto 20px auto;
            display: block;
        }}
        .icon {{
            font-size: 64px;
            margin: 20px 0;
        }}
        h1 {{
            color: #333;
            margin: 20px 0;
            font-size: 28px;
            font-weight: 600;
        }}
        .message {{
            color: #666;
            font-size: 16px;
            line-height: 1.6;
            margin: 20px 0 30px 0;
        }}
        .button {{
            display: inline-block;
            padding: 15px 30px;
            background: linear-gradient(135deg, #0B8C99 0%, #0A7A85 100%);
            color: white;
            text-decoration: none;
            border-radius: 25px;
            font-weight: 600;
            font-size: 16px;
            border: none;
            cursor: pointer;
            transition: all 0.3s ease;
            box-shadow: 0 3px 15px rgba(11, 140, 153, 0.3);
        }}
        .button:hover {{
            transform: translateY(-2px);
            box-shadow: 0 5px 20px rgba(11, 140, 153, 0.4);
        }}
        .success .icon {{ color: #28a745; }}
        .error .icon {{ color: #dc3545; }}
        .footer {{
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid #eee;
            font-size: 14px;
            color: #888;
        }}
    </style>
</head>
<body>
    <div class=""container {statusClass}"">
        <img src=""/images/LogoOscuro.png"" alt=""GestionTime"" class=""logo"" />
        
        <div class=""icon"">{icon}</div>
        
        <h1>{title}</h1>
        
        <p class=""message"">{message}</p>
        
        <button class=""button"" onclick=""{buttonAction}"">
            {buttonText}
        </button>
        
        <div class=""footer"">
            <p>© 2025 GestionTime - Sistema de Gestión de Tiempo</p>
        </div>
    </div>
    
    <script>
        // Auto-cerrar después de 5 segundos si es exitoso
        {autoClose}
    </script>
</body>
</html>";
    }

    // Cerrar la clase AuthController

}

public record ForcePasswordChangeRequest
{
    public string Email { get; init; } = "";
}
