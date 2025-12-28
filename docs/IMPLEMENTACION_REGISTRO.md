# ?? Implementación Registro de Usuarios con Verificación de Email

## ? Archivos Ya Creados

### Backend (API)
- ? `RegisterRequest.cs` - DTO para solicitar registro
- ? `RegisterResponse.cs` - DTO de respuesta
- ? `VerifyEmailRequest.cs` - DTO para verificar email
- ? `IEmailService.cs` - Actualizado con SendRegistrationEmailAsync
- ? `FakeEmailService.cs` - Actualizado con envío de email de registro
- ? `CODIGO_REGISTRO_COPIAR.txt` - Código de endpoints listo

---

## ?? Paso 1: Agregar Endpoints al AuthController.cs

**Archivo:** `C:\GestionTime\src\GestionTime.Api\Controllers\AuthController.cs`

1. Abre el archivo
2. Ve al final (antes del último `}`)
3. Copia el código de: `C:\GestionTime\src\GestionTime.Api\docs\CODIGO_REGISTRO_COPIAR.txt`
4. Pégalo antes del cierre de la clase
5. Guarda

**Nota:** El código requiere una tabla temporal para guardar los datos de registro. Por simplicidad, usa MemoryCache como los reset tokens.

---

## ?? Modificación Necesaria en el Código

En lugar de usar `db.TempRegistrationData` (que requeriría crear una nueva tabla), vamos a usar el mismo sistema de caché que usamos para reset password.

### Versión Simplificada (usando MemoryCache):

Reemplaza las líneas en el endpoint `register`:

```csharp
// EN LUGAR DE:
db.TempRegistrationData.Add(...);
await db.SaveChangesAsync();

// USA:
// Crear una clave única en el caché
var cacheKey = $"register:{token}";
var cacheOptions = new MemoryCacheEntryOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
};
// Guardar en caché usando el servicio existente
// Necesitas modificar ResetTokenService para soportar datos adicionales
```

---

## ?? Actualizar ResetTokenService

**Archivo:** `C:\GestionTime\src\GestionTime.Api\Services\ResetTokenService.cs`

Agregar métodos:

```csharp
public void SaveTokenWithData(string key, string jsonData)
{
    var options = new MemoryCacheEntryOptions
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
    };
    _cache.Set(key, jsonData, options);
}

public string? GetTokenData(string key)
{
    if (_cache.TryGetValue(key, out string data))
    {
        return data;
    }
    return null;
}

public void RemoveTokenByKey(string key)
{
    _cache.Remove(key);
}
```

---

## ?? Código Completo de Endpoints (Versión Final)

```csharp
// ========================================
// REGISTRO DE USUARIOS
// ========================================

[HttpPost("register")]
[AllowAnonymous]
public async Task<IActionResult> Register(
    [FromBody] RegisterRequest req,
    [FromServices] Services.ResetTokenService tokenSvc,
    [FromServices] Services.IEmailService emailSvc)
{
    var email = (req.Email ?? "").Trim().ToLowerInvariant();
    logger.LogInformation("Solicitud de registro para {Email}", email);

    var existingUser = await db.Users.SingleOrDefaultAsync(u => u.Email == email);

    if (existingUser != null)
    {
        logger.LogWarning("Email ya registrado: {Email}", email);
        return BadRequest(new RegisterResponse(false, null, "El email ya está registrado."));
    }

    var token = tokenSvc.GenerateToken();
    
    var tempData = new
    {
        Email = email,
        FullName = req.FullName ?? "",
        Password = req.Password ?? "",
        Empresa = req.Empresa ?? ""
    };
    
    var jsonData = System.Text.Json.JsonSerializer.Serialize(tempData);
    tokenSvc.SaveTokenWithData($"register:{token}", jsonData);
    
    logger.LogInformation("Código de verificación: {Token} para {Email}", token, email);

    try
    {
        await emailSvc.SendRegistrationEmailAsync(email, token);
        logger.LogInformation("Email de verificación enviado");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error enviando email");
        return StatusCode(500, new RegisterResponse(false, null, "Error al enviar el correo."));
    }

    return Ok(new RegisterResponse(true, "Código enviado a tu correo.", null));
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

    var jsonData = tokenSvc.GetTokenData($"register:{token}");

    if (jsonData == null)
    {
        logger.LogWarning("Token inválido o expirado");
        return BadRequest(new RegisterResponse(false, null, "Código inválido o expirado."));
    }

    var data = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(jsonData);
    var storedEmail = data.GetProperty("Email").GetString();

    if (storedEmail != email)
    {
        logger.LogWarning("Email no coincide");
        return BadRequest(new RegisterResponse(false, null, "Código inválido."));
    }

    var existingUser = await db.Users.SingleOrDefaultAsync(u => u.Email == email);
    if (existingUser != null)
    {
        return BadRequest(new RegisterResponse(false, null, "El email ya está registrado."));
    }

    try
    {
        var fullName = data.GetProperty("FullName").GetString() ?? "";
        var password = data.GetProperty("Password").GetString() ?? "";
        var empresa = data.GetProperty("Empresa").GetString();

        var newUser = new GestionTime.Domain.Auth.User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FullName = fullName,
            Empresa = empresa,
            Enabled = true,
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(newUser);

        var userRole = await db.Roles.SingleOrDefaultAsync(r => r.Name == "User");
        if (userRole != null)
        {
            db.UserRoles.Add(new GestionTime.Domain.Auth.UserRole
            {
                Id = Guid.NewGuid(),
                UserId = newUser.Id,
                RoleId = userRole.Id
            });
        }

        await db.SaveChangesAsync();
        tokenSvc.RemoveTokenByKey($"register:{token}");

        logger.LogInformation("Usuario registrado: {Email}", email);

        return Ok(new RegisterResponse(true, "Registro exitoso. Ya puedes iniciar sesión.", null));
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error creando usuario");
        return StatusCode(500, new RegisterResponse(false, null, "Error al crear el usuario."));
    }
}
```

---

## ?? Probar

### Paso 1: Solicitar Código
```
POST /api/v1/auth/register
{
  "email": "nuevo@example.com",
  "fullName": "Usuario Nuevo",
  "password": "password123",
  "empresa": "Mi Empresa"
}
```

**Esperado:**
- Status: 200 OK
- En consola: Código de 6 dígitos

### Paso 2: Verificar y Crear Usuario
```
POST /api/v1/auth/verify-email
{
  "email": "nuevo@example.com",
  "token": "123456",
  "fullName": "Usuario Nuevo",
  "password": "password123",
  "empresa": "Mi Empresa"
}
```

**Esperado:**
- Status: 200 OK
- Usuario creado con rol "User"

---

## ?? Checklist

### Backend
- [ ] Actualizar `ResetTokenService.cs` con métodos adicionales
- [ ] Agregar endpoints al `AuthController.cs`
- [ ] Compilar sin errores
- [ ] Ejecutar API
- [ ] Probar en Swagger

### Frontend (Desktop)
- [ ] Actualizar `RegisterPage.xaml` para 2 pasos
- [ ] Actualizar `RegisterPage.xaml.cs` con lógica
- [ ] Probar flujo completo

---

¿Quieres que continúe con la implementación del frontend o prefieres terminar primero el backend?
