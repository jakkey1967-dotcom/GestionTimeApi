# ?? Pasos Finales - Completar Implementación

**Solo te faltan 2 pasos simples para completar todo!**

---

## ? Archivos Ya Creados Automáticamente

### Contratos (DTOs)
- ? `C:\GestionTime\src\GestionTime.Api\Contracts\Auth\ForgotPasswordRequest.cs`
- ? `C:\GestionTime\src\GestionTime.Api\Contracts\Auth\ResetPasswordRequest.cs`
- ? `C:\GestionTime\src\GestionTime.Api\Contracts\Auth\ForgotPasswordResponse.cs`

### Servicios
- ? `C:\GestionTime\src\GestionTime.Api\Services\IEmailService.cs`
- ? `C:\GestionTime\src\GestionTime.Api\Services\FakeEmailService.cs`
- ? `C:\GestionTime\src\GestionTime.Api\Services\ResetTokenService.cs`

---

## ?? PASO 1: Agregar Endpoints al AuthController.cs

### 1.1 Abrir el archivo
```
C:\GestionTime\src\GestionTime.Api\Controllers\AuthController.cs
```

### 1.2 Buscar el final de la clase
Busca la línea que contiene `private void SetRefreshCookie(...)` y ve hasta el final de ese método.

Encontrarás algo así:

```csharp
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
} // ? Aquí está el cierre de la clase
```

### 1.3 Copiar el código de los endpoints
Abre este archivo:
```
C:\GestionTime\src\GestionTime.Api\docs\AGREGAR_ENDPOINTS_RESET_PASSWORD.md
```

Copia todo el código que está bajo "## ?? Código a Agregar"

### 1.4 Pegar el código
Pega el código **después** del método `SetRefreshCookie()` y **antes** del cierre de la clase (antes del último `}`).

Debería quedar así:

```csharp
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

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest req,
        [FromServices] Services.ResetTokenService resetTokenSvc,
        [FromServices] Services.IEmailService emailSvc)
    {
        // ... resto del código ...
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest req,
        [FromServices] Services.ResetTokenService resetTokenSvc)
    {
        // ... resto del código ...
    }
}
```

### 1.5 Guardar el archivo
Presiona `Ctrl + S` para guardar.

---

## ?? PASO 2: Registrar Servicios en Program.cs

### 2.1 Abrir el archivo
```
C:\GestionTime\src\GestionTime.Api\Program.cs
```

### 2.2 Buscar donde se registran los servicios
Busca una línea similar a:
```csharp
var app = builder.Build();
```

### 2.3 Agregar ANTES de `var app = builder.Build();`

```csharp
// Memory Cache (si no está ya agregado)
builder.Services.AddMemoryCache();

// Servicios de recuperación de contraseña
builder.Services.AddScoped<GestionTime.Api.Services.ResetTokenService>();
builder.Services.AddScoped<GestionTime.Api.Services.IEmailService, GestionTime.Api.Services.FakeEmailService>();
```

Debería quedar algo así:

```csharp
// ... otros servicios ...

// Memory Cache (si no está ya agregado)
builder.Services.AddMemoryCache();

// Servicios de recuperación de contraseña
builder.Services.AddScoped<GestionTime.Api.Services.ResetTokenService>();
builder.Services.AddScoped<GestionTime.Api.Services.IEmailService, GestionTime.Api.Services.FakeEmailService>();

var app = builder.Build();

// ... resto del código ...
```

### 2.4 Guardar el archivo
Presiona `Ctrl + S` para guardar.

---

## ??? PASO 3: Compilar y Ejecutar

### 3.1 Abrir terminal en la carpeta del proyecto API
```powershell
cd C:\GestionTime\src\GestionTime.Api
```

### 3.2 Compilar
```powershell
dotnet build
```

**Resultado esperado:** ? Build succeeded. 0 Warning(s). 0 Error(s).

### 3.3 Ejecutar
```powershell
dotnet run
```

**Resultado esperado:** 
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:2501
```

---

## ?? PASO 4: Probar con Swagger

### 4.1 Abrir Swagger
Abre en tu navegador:
```
https://localhost:2501/swagger
```

### 4.2 Probar Endpoint 1: Solicitar Código

1. Busca `POST /api/v1/auth/forgot-password`
2. Click en "Try it out"
3. En el body, ingresa:
```json
{
  "email": "test@example.com"
}
```
4. Click "Execute"

**Resultado esperado:**
- Status: 200 OK
- Response body:
```json
{
  "success": true,
  "message": "Código de verificación enviado a tu correo.",
  "error": null
}
```

**IMPORTANTE:** Mira la consola donde está corriendo la API. Verás algo así:
```
========================
FAKE EMAIL
Para: test@example.com
Asunto: Recuperación de Contraseña
Código: 456789
========================
```

**Copia ese código (ej: 456789)** para el siguiente paso.

### 4.3 Probar Endpoint 2: Cambiar Contraseña

1. Busca `POST /api/v1/auth/reset-password`
2. Click en "Try it out"
3. En el body, ingresa (usando el código que copiaste):
```json
{
  "token": "456789",
  "email": "test@example.com",
  "newPassword": "nueva123"
}
```
4. Click "Execute"

**Resultado esperado:**
- Status: 200 OK
- Response body:
```json
{
  "success": true,
  "message": "Contraseña actualizada correctamente.",
  "error": null
}
```

---

## ?? PASO 5: Probar desde la App Desktop

### 5.1 Ejecutar la aplicación desktop
```powershell
cd C:\GestionTime\GestionTime.Desktop
dotnet run
```

### 5.2 Probar el flujo completo

1. En la pantalla de login, click en "¿Olvidaste tu contraseña?"
2. Ingresa un email válido (de un usuario que existe en tu DB)
3. Click "Solicitar código"
4. Ve a los logs de la API y copia el código de 6 dígitos
5. En la app desktop, ingresa:
   - Código: (el que copiaste)
   - Nueva contraseña: nueva123
   - Repetir contraseña: nueva123
6. Click "Cambiar contraseña"
7. Deberías ver mensaje de éxito y redirección al login
8. Haz login con la nueva contraseña

---

## ? Checklist de Verificación

- [ ] Endpoints agregados al `AuthController.cs`
- [ ] Servicios registrados en `Program.cs`
- [ ] `dotnet build` exitoso
- [ ] API corriendo en `https://localhost:2501`
- [ ] Swagger accesible
- [ ] Test 1: `forgot-password` responde 200 OK
- [ ] Test 1: Código visible en consola de API
- [ ] Test 2: `reset-password` responde 200 OK con código válido
- [ ] Test 2: Responde 400 Bad Request con código inválido
- [ ] Test 3: Desktop app muestra ambos pasos
- [ ] Test 3: Desktop app envía requests correctamente
- [ ] Test 4: Flujo completo funciona end-to-end
- [ ] Test 5: Puedes hacer login con la nueva contraseña

---

## ?? Problemas Comunes

### Problema: Build falla con errores de compilación

**Solución:**
- Verifica que copiaste todo el código completo de los endpoints
- Verifica que los imports están correctos en `AuthController.cs`
- Asegúrate de que no hay llaves `{}` mal cerradas

### Problema: Error 404 en los endpoints

**Solución:**
- Verifica que los servicios están registrados en `Program.cs`
- Reinicia la API completamente

### Problema: El código no se muestra en la consola

**Solución:**
- Verifica que `FakeEmailService` está registrado
- Verifica que no hay errores en los logs
- Mira la pestaña "Output" en Visual Studio

### Problema: Error 400 "Código inválido o expirado"

**Posibles causas:**
- El código expiró (más de 1 hora)
- El código fue usado anteriormente
- El email no coincide
- El código tiene typo

**Solución:**
- Solicita un nuevo código
- Verifica que estás usando el email correcto
- Copia el código directamente sin espacios

---

## ?? Documentación Adicional

### Documentos Creados
- `C:\GestionTime\src\GestionTime.Api\docs\AGREGAR_ENDPOINTS_RESET_PASSWORD.md` - Código de endpoints
- `C:\GestionTime\src\GestionTime.Api\docs\RESUMEN_IMPLEMENTACION_COMPLETA.md` - Resumen completo
- `C:\GestionTime\GestionTime.Desktop\Helpers\IMPLEMENTACION_RESET_PASSWORD_COMPLETO.md` - Implementación frontend
- `C:\GestionTime\GestionTime.Desktop\Helpers\IMPLEMENTACION_ENVIO_EMAILS.md` - Guía de emails

### Para Producción (Futuro)
Ver: `C:\GestionTime\GestionTime.Desktop\Helpers\IMPLEMENTACION_ENVIO_EMAILS.md`

Deberás:
1. Reemplazar `FakeEmailService` con `SmtpEmailService` o `SendGridEmailService`
2. Configurar credenciales de email en `appsettings.json`
3. Probar envío de emails reales
4. Agregar rate limiting
5. Considerar usar Redis en lugar de MemoryCache

---

## ?? Resumen

**Total de tiempo estimado:** 5-10 minutos

1. ?? Copiar código de endpoints al `AuthController.cs` (2 min)
2. ?? Registrar servicios en `Program.cs` (1 min)
3. ??? Compilar y ejecutar (1 min)
4. ?? Probar con Swagger (3 min)
5. ?? Probar desde Desktop App (3 min)

**¡Ya casi terminas! Solo te faltan estos 2 pasos simples y todo estará funcionando!** ??
