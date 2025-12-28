# ?? INSTRUCCIONES RÁPIDAS - Agregar Endpoints al AuthController

## ? Paso a Paso (2 minutos)

### 1. Abrir AuthController.cs
```
Archivo: C:\GestionTime\src\GestionTime.Api\Controllers\AuthController.cs
```

### 2. Ir al final del archivo
- Presiona `Ctrl + End` para ir al final
- Busca el último método `SetRefreshCookie`
- Verás el cierre de clase: `}`

Deberías ver esto:

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
} // ? AQUÍ está el cierre de la clase
```

### 3. Posicionar el cursor
- Coloca el cursor DESPUÉS del último `}` del método `SetRefreshCookie`
- Coloca el cursor ANTES del cierre de la clase (el último `}`)
- Presiona `Enter` dos veces para dar espacio

### 4. Abrir el archivo con el código a copiar
```
Archivo: C:\GestionTime\src\GestionTime.Api\Controllers\ENDPOINTS_TO_ADD.cs
```

### 5. Copiar TODO el contenido
- Abre `ENDPOINTS_TO_ADD.cs`
- Presiona `Ctrl + A` (seleccionar todo)
- Presiona `Ctrl + C` (copiar)

### 6. Pegar en AuthController.cs
- Vuelve a `AuthController.cs`
- Asegúrate de estar en la línea correcta (después de `SetRefreshCookie` y antes del cierre)
- Presiona `Ctrl + V` (pegar)

### 7. Verificar la estructura
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
        // ... código del endpoint ...
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest req,
        [FromServices] Services.ResetTokenService resetTokenSvc)
    {
        // ... código del endpoint ...
    }
} // ? Cierre de la clase
```

### 8. Guardar
- Presiona `Ctrl + S`

---

## ?? Registrar Servicios en Program.cs

### 1. Abrir Program.cs
```
Archivo: C:\GestionTime\src\GestionTime.Api\Program.cs
```

### 2. Buscar donde se construye la app
- Presiona `Ctrl + F`
- Busca: `var app = builder.Build();`

### 3. Agregar ANTES de esa línea
Agrega estas 4 líneas ANTES de `var app = builder.Build();`:

```csharp
// Memory Cache
builder.Services.AddMemoryCache();

// Servicios de recuperación de contraseña
builder.Services.AddScoped<GestionTime.Api.Services.ResetTokenService>();
builder.Services.AddScoped<GestionTime.Api.Services.IEmailService, GestionTime.Api.Services.FakeEmailService>();
```

Debería quedar algo así:

```csharp
// ... otros servicios ...

// Memory Cache
builder.Services.AddMemoryCache();

// Servicios de recuperación de contraseña
builder.Services.AddScoped<GestionTime.Api.Services.ResetTokenService>();
builder.Services.AddScoped<GestionTime.Api.Services.IEmailService, GestionTime.Api.Services.FakeEmailService>();

var app = builder.Build();

// ... resto del código ...
```

### 4. Guardar
- Presiona `Ctrl + S`

---

## ? Compilar y Ejecutar

### 1. Abrir terminal en la carpeta de la API
```powershell
cd C:\GestionTime\src\GestionTime.Api
```

### 2. Compilar
```powershell
dotnet build
```

**Esperado:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

Si hay errores:
- Verifica que copiaste TODO el código
- Verifica que no hay llaves `{}` mal cerradas
- Verifica que está después de `SetRefreshCookie` y antes del cierre de clase

### 3. Ejecutar
```powershell
dotnet run
```

**Esperado:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:2501
```

---

## ?? Probar en Swagger

### 1. Abrir Swagger
```
https://localhost:2501/swagger
```

### 2. Verificar que aparecen los endpoints
Deberías ver:
- ? `POST /api/v1/auth/forgot-password`
- ? `POST /api/v1/auth/reset-password`

### 3. Probar Endpoint 1: forgot-password
1. Click en `POST /api/v1/auth/forgot-password`
2. Click "Try it out"
3. Body:
```json
{
  "email": "test@example.com"
}
```
4. Click "Execute"

**Esperado:**
- Status: 200 OK
- En la consola de la API verás:
```
========================
FAKE EMAIL
Para: test@example.com
Asunto: Recuperación de Contraseña
Código: 456789
========================
```

### 4. Probar Endpoint 2: reset-password
1. Click en `POST /api/v1/auth/reset-password`
2. Click "Try it out"
3. Body (usa el código que apareció en la consola):
```json
{
  "token": "456789",
  "email": "test@example.com",
  "newPassword": "nueva123"
}
```
4. Click "Execute"

**Esperado:**
- Status: 200 OK
- Response:
```json
{
  "success": true,
  "message": "Contraseña actualizada correctamente.",
  "error": null
}
```

---

## ?? Solución de Problemas

### Problema: Build falla

**Error típico:**
```
CS1513: } esperado
```

**Solución:**
1. Abre `AuthController.cs`
2. Ve al final del archivo
3. Cuenta las llaves de apertura `{` y cierre `}`
4. Debe haber exactamente UNA llave `}` al final (cierre de clase)
5. Si hay dos `}}`, borra una

### Problema: Endpoints no aparecen en Swagger

**Causas:**
1. No guardaste el archivo (`Ctrl + S`)
2. No reiniciaste la API
3. El código está fuera de la clase

**Solución:**
1. Guardar `AuthController.cs`
2. Detener la API (`Ctrl + C`)
3. Ejecutar de nuevo: `dotnet run`
4. Refrescar Swagger

### Problema: Error "Services.ResetTokenService not found"

**Causa:** No registraste los servicios en `Program.cs`

**Solución:**
1. Abre `Program.cs`
2. Verifica que agregaste las 4 líneas ANTES de `var app = builder.Build();`
3. Guarda y reinicia la API

---

## ?? Resumen Visual

```
AuthController.cs:
??????????????????????????????????????
? public class AuthController        ?
? {                                  ?
?     [HttpPost("login")]            ?
?     public async Task Login(...)   ?
?     { ... }                        ?
?                                    ?
?     [HttpPost("refresh")]          ?
?     public async Task Refresh(...) ?
?     { ... }                        ?
?                                    ?
?     [HttpPost("logout")]           ?
?     public async Task Logout(...)  ?
?     { ... }                        ?
?                                    ?
?     private void SetAccessCookie   ?
?     { ... }                        ?
?                                    ?
?     private void SetRefreshCookie  ?
?     { ... }                        ?
?     } // ? Fin del método          ?
?                                    ?
?     // ?? PEGAR AQUÍ ??            ?
?                                    ?
?     [HttpPost("forgot-password")]  ? ? Nuevo
?     public async Task             ?
?     ForgotPassword(...)            ?
?     { ... }                        ?
?                                    ?
?     [HttpPost("reset-password")]   ? ? Nuevo
?     public async Task             ?
?     ResetPassword(...)             ?
?     { ... }                        ?
?                                    ?
? } // ? Cierre de la clase         ?
??????????????????????????????????????
```

---

## ?? Tiempo Total: 2 minutos

1. Copy/Paste código (30 seg)
2. Registrar servicios (30 seg)
3. Compilar (30 seg)
4. Probar en Swagger (30 seg)

---

¡Listo! Después de estos pasos, los endpoints aparecerán en Swagger y todo funcionará! ??
