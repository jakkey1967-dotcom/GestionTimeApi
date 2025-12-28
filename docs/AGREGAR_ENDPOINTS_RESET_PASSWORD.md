# ?? Código para Agregar al AuthController.cs

Este archivo contiene el código de los nuevos endpoints de recuperación de contraseña que debes agregar al archivo `AuthController.cs`.

## ?? Ubicación

Archivo: `C:\GestionTime\src\GestionTime.Api\Controllers\AuthController.cs`

## ?? Instrucciones

1. Abre el archivo `AuthController.cs`
2. Busca el final de la clase (antes del último `}`)
3. Agrega el siguiente código después del método `SetRefreshCookie` y antes del cierre de la clase

## ?? Código a Agregar

```csharp
    // ========================================
    // RECUPERACIÓN DE CONTRASEÑA
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
        [FromBody] ResetPasswordRequest req,
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
```

## ?? Ejemplo de Posicionamiento

```csharp
public class AuthController(
    GestionTimeDbContext db, 
    JwtService jwt, 
    RefreshTokenService refreshSvc,
    ILogger<AuthController> logger) : ControllerBase
{
    // ... código existente ...

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

    // ?? AGREGAR AQUÍ EL CÓDIGO NUEVO ??

    // ========================================
    // RECUPERACIÓN DE CONTRASEÑA
    // ========================================
    
    [HttpPost("forgot-password")]
    ... (código completo arriba) ...
    
} // ? Cierre de la clase
```

## ? Archivos Creados

Los siguientes archivos ya fueron creados automáticamente:

### Contratos (DTOs)
- ? `C:\GestionTime\src\GestionTime.Api\Contracts\Auth\ForgotPasswordRequest.cs`
- ? `C:\GestionTime\src\GestionTime.Api\Contracts\Auth\ResetPasswordRequest.cs`
- ? `C:\GestionTime\src\GestionTime.Api\Contracts\Auth\ForgotPasswordResponse.cs`

### Servicios
- ? `C:\GestionTime\src\GestionTime.Api\Services\IEmailService.cs`
- ? `C:\GestionTime\src\GestionTime.Api\Services\FakeEmailService.cs`
- ? `C:\GestionTime\src\GestionTime.Api\Services\ResetTokenService.cs`

## ?? Registrar Servicios en Program.cs

También necesitas registrar los nuevos servicios en `Program.cs`:

```csharp
// Agregar antes de var app = builder.Build();

// Memory Cache (si no está ya agregado)
builder.Services.AddMemoryCache();

// Servicios de recuperación de contraseña
builder.Services.AddScoped<GestionTime.Api.Services.ResetTokenService>();
builder.Services.AddScoped<GestionTime.Api.Services.IEmailService, GestionTime.Api.Services.FakeEmailService>();
```

## ?? Probar con Swagger

Una vez agregado el código, compila y ejecuta la API, luego prueba:

### 1. Solicitar Código
```
POST /api/v1/auth/forgot-password
{
  "email": "test@example.com"
}
```

**Respuesta esperada:**
```json
{
  "success": true,
  "message": "Código de verificación enviado a tu correo.",
  "error": null
}
```

El código se imprimirá en la consola del servidor.

### 2. Resetear Contraseña
```
POST /api/v1/auth/reset-password
{
  "token": "123456",
  "email": "test@example.com",
  "newPassword": "nueva123"
}
```

**Respuesta esperada:**
```json
{
  "success": true,
  "message": "Contraseña actualizada correctamente.",
  "error": null
}
```
