# ?? SOLUCIÓN: Endpoints No Aparecen en Swagger

## ?? Problema
Los endpoints `POST /api/v1/auth/forgot-password` y `POST /api/v1/auth/reset-password` NO aparecen en Swagger.

## ? Causa
**Falta agregar el código de los endpoints al `AuthController.cs`**

Los archivos de servicios y DTOs ya están creados, pero el código de los 2 endpoints nuevos aún no está en el controlador.

---

## ?? SOLUCIÓN RÁPIDA (2 minutos)

### Paso 1: Copiar el código de los endpoints

**Archivo con el código listo:**
```
C:\GestionTime\src\GestionTime.Api\Controllers\ENDPOINTS_TO_ADD.cs
```

1. Abre ese archivo
2. Presiona `Ctrl + A` (seleccionar todo)
3. Presiona `Ctrl + C` (copiar)

### Paso 2: Pegar en AuthController.cs

**Archivo a modificar:**
```
C:\GestionTime\src\GestionTime.Api\Controllers\AuthController.cs
```

1. Abre ese archivo
2. Presiona `Ctrl + End` (ir al final)
3. Busca el método `SetRefreshCookie` (el último método)
4. Coloca el cursor DESPUÉS del cierre de ese método `}`
5. Coloca el cursor ANTES del cierre de la clase (el último `}`)
6. Presiona `Ctrl + V` (pegar)
7. Presiona `Ctrl + S` (guardar)

**Debe quedar así:**

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

    // ?? CÓDIGO PEGADO AQUÍ ??

    // ========================================
    // RECUPERACIÓN DE CONTRASEÑA
    // ========================================

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(...)
    {
        // ... código ...
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(...)
    {
        // ... código ...
    }

} // ? Cierre de la clase
```

### Paso 3: Registrar servicios en Program.cs

**Archivo a modificar:**
```
C:\GestionTime\src\GestionTime.Api\Program.cs
```

1. Abre ese archivo
2. Busca la línea: `var app = builder.Build();`
3. ANTES de esa línea, agrega:

```csharp
// Memory Cache
builder.Services.AddMemoryCache();

// Servicios de recuperación de contraseña
builder.Services.AddScoped<GestionTime.Api.Services.ResetTokenService>();
builder.Services.AddScoped<GestionTime.Api.Services.IEmailService, GestionTime.Api.Services.FakeEmailService>();
```

4. Guarda con `Ctrl + S`

### Paso 4: Compilar y ejecutar

```powershell
cd C:\GestionTime\src\GestionTime.Api
dotnet build
dotnet run
```

### Paso 5: Verificar en Swagger

Abre: https://localhost:2501/swagger

**Deberías ver:**
- ? `POST /api/v1/auth/forgot-password`
- ? `POST /api/v1/auth/reset-password`

---

## ?? Documentación Completa

Para instrucciones detalladas con capturas y troubleshooting:
```
C:\GestionTime\src\GestionTime.Api\docs\INSTRUCCIONES_COPIAR_ENDPOINTS.md
```

---

## ?? Resumen

| Archivo | Acción | Estado |
|---------|--------|--------|
| `ENDPOINTS_TO_ADD.cs` | Copiar TODO | ? Listo |
| `AuthController.cs` | Pegar al final (antes del `}`) | ? Pendiente |
| `Program.cs` | Agregar 4 líneas | ? Pendiente |
| Build + Run | Compilar y ejecutar | ? Pendiente |
| Swagger | Verificar endpoints | ? Pendiente |

---

**Tiempo total:** 2 minutos ??
