# 🔧 INSTRUCCIONES PASO A PASO: Fix Refresh Endpoint

## 📍 UBICACIÓN
**Archivo:** `C:\GestionTime\GestionTimeApi\Controllers\AuthController.cs`

---

## PASO 1: Agregar el Modelo RefreshRequest

**Buscar:** El final del archivo (después de `ForcePasswordChangeRequest`)

**Agregar ANTES del último `}`:**

```csharp
// 🆕 NUEVO: Modelo para recibir refresh token desde desktop (JSON body)
public record RefreshRequest
{
    public string? RefreshToken { get; init; }
}
```

---

## PASO 2: Reemplazar el Método [HttpPost("refresh")]

**Buscar esta línea:**
```csharp
[HttpPost("refresh")]
public async Task<IActionResult> Refresh()
```

**REEMPLAZAR TODO EL MÉTODO (desde `[HttpPost("refresh")]` hasta el `}` que cierra ese método) CON:**

```csharp
/// <summary>Refresca el access token usando un refresh token válido. Soporta cookies (web) y JSON body (desktop).</summary>
[HttpPost("refresh")]
public async Task<IActionResult> Refresh([FromBody] RefreshRequest? bodyRequest)
{
    string? rawRefresh = null;
    bool isDesktopClient = false;
    
    // 1️⃣ Intentar obtener desde cookie (clientes web)
    if (Request.Cookies.TryGetValue("refresh_token", out var cookieToken))
    {
        rawRefresh = cookieToken;
        logger.LogDebug("Refresh token obtenido desde cookie (cliente web)");
    }
    // 2️⃣ Intentar obtener desde body JSON (clientes desktop)
    else if (bodyRequest != null && !string.IsNullOrWhiteSpace(bodyRequest.RefreshToken))
    {
        rawRefresh = bodyRequest.RefreshToken;
        isDesktopClient = true;
        logger.LogDebug("Refresh token obtenido desde body JSON (cliente desktop)");
    }
    
    // Validar que se recibió un token
    if (string.IsNullOrWhiteSpace(rawRefresh))
    {
        logger.LogWarning("Intento de refresh sin token (ni cookie ni body JSON)");
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

    logger.LogInformation("Refresh exitoso para UserId: {UserId} (Roles: {Roles}, Desktop: {IsDesktop})", 
        token.User.Id, string.Join(", ", roles), isDesktopClient);

    // 3️⃣ Respuesta según el tipo de cliente
    if (isDesktopClient)
    {
        // Cliente desktop: devolver tokens en JSON body
        return Ok(new
        {
            accessToken = newAccess,
            refreshToken = newRawRefresh,
            expiresAt = newRefreshExpires
        });
    }
    else
    {
        // Cliente web: devolver tokens en cookies (comportamiento original)
        SetAccessCookie(newAccess);
        SetRefreshCookie(newRawRefresh, newRefreshExpires);

        return Ok(new
        {
            message = "ok",
            expiresAt = DateTime.UtcNow.AddMinutes(15)
        });
    }
}
```

---

## PASO 3: Verificar y Guardar

1. ✅ Verificar que no hay errores de sintaxis
2. ✅ Guardar el archivo (`Ctrl+S`)
3. ✅ Compilar el proyecto para verificar

```bash
cd C:\GestionTime\GestionTimeApi
dotnet build
```

---

## PASO 4: Desplegar a Render

```bash
git add Controllers/AuthController.cs
git commit -m "fix: soporte JSON body en endpoint /auth/refresh para desktop"
git push origin main
```

Render detectará el push y desplegará automáticamente.

---

## 🧪 VERIFICACIÓN

### Después del despliegue, verificar en los logs del desktop:

**ANTES (fallaba):**
```
[WRN] ❌ Error refrescando token: Unauthorized
[WRN] ⚠️ Refresh token expirado, usuario debe hacer login
```

**DESPUÉS (debe funcionar):**
```
[INF] 🔄 Token próximo a expirar, refrescando...
[INF] HTTP POST /api/v1/auth/refresh -> 200 en XXms
[INF] ✅ Token refrescado exitosamente
[INF] AUTH: Token expira en 15.0 minutos (a las XX:XX:XX)
```

---

## ⚠️ IMPORTANTE

- ✅ **NO eliminar** el código de cookies (para mantener compatibilidad web)
- ✅ **NO cambiar** la lógica de validación del token
- ✅ **NO modificar** el comportamiento de rotación
- ✅ Solo **AGREGAR** soporte para JSON body

---

## 📊 CAMBIOS RESUMIDOS

| Línea | Antes | Después |
|-------|-------|---------|
| Firma del método | `Refresh()` | `Refresh([FromBody] RefreshRequest? bodyRequest)` |
| Obtención del token | Solo cookies | Cookies O JSON body |
| Respuesta web | Cookies | Cookies (sin cambios) |
| Respuesta desktop | N/A | JSON con tokens |

---

**Tiempo estimado:** 5 minutos  
**Riesgo:** Bajo (solo agrega funcionalidad, no cambia existente)  
**Testing:** Automático en desktop al hacer login y esperar 10 minutos

---

*Última actualización: 2026-01-06*
