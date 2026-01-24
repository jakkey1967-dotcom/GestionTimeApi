# 🔧 FIX: Endpoint de Refresh para Desktop

## 🎯 Problema Identificado

El endpoint `/api/v1/auth/refresh` **solo acepta cookies**, pero el cliente desktop envía el refresh token en el **body JSON**.

### ❌ Código Actual (Solo Cookies)

```csharp
[HttpPost("refresh")]
public async Task<IActionResult> Refresh()
{
    // ❌ Solo busca en cookies
    if (!Request.Cookies.TryGetValue("refresh_token", out var rawRefresh) || string.IsNullOrWhiteSpace(rawRefresh))
    {
        logger.LogWarning("Intento de refresh sin token");
        return Unauthorized(new { message = "No refresh token" });
    }
    
    // ...resto del código...
}
```

### ✅ Solución: Soporte para Ambos Métodos

```csharp
// 🆕 NUEVO: Modelo para recibir refresh token en body JSON
public record RefreshRequest
{
    public string? RefreshToken { get; init; }
}

[HttpPost("refresh")]
public async Task<IActionResult> Refresh([FromBody] RefreshRequest? bodyRequest)
{
    string? rawRefresh = null;
    bool isDesktopClient = false;
    
    // 1️⃣ Intentar obtener desde cookie (para clientes web)
    if (Request.Cookies.TryGetValue("refresh_token", out var cookieToken))
    {
        rawRefresh = cookieToken;
        logger.LogDebug("Refresh token obtenido desde cookie (cliente web)");
    }
    // 2️⃣ Intentar obtener desde body JSON (para clientes desktop)
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

## 📝 Instrucciones de Implementación

### Paso 1: Agregar el Modelo de Request

En `AuthController.cs`, después de las otras clases de modelo (al final del archivo):

```csharp
// Al final del archivo, junto a otros modelos (ForcePasswordChangeRequest, etc.)
public record RefreshRequest
{
    public string? RefreshToken { get; init; }
}
```

### Paso 2: Reemplazar el Método Refresh

Buscar el método `[HttpPost("refresh")]` actual y reemplazarlo completamente con el código de arriba.

---

## ✅ Resultado Esperado

### Para Cliente Web (Cookies)

**Request:**
```
POST /api/v1/auth/refresh HTTP/1.1
Cookie: refresh_token=eyJhbGc...
```

**Response:**
```json
{
  "message": "ok",
  "expiresAt": "2026-01-06T19:00:00Z"
}
```
*(Los tokens se devuelven en cookies Set-Cookie)*

### Para Cliente Desktop (JSON Body)

**Request:**
```
POST /api/v1/auth/refresh HTTP/1.1
Content-Type: application/json

{
  "refreshToken": "eyJhbGc..."
}
```

**Response:**
```json
{
  "accessToken": "eyJhbGc...",
  "refreshToken": "eyJhbGc...",
  "expiresAt": "2026-01-20T19:00:00Z"
}
```

---

## 🧪 Testing

### Test 1: Cliente Web (Sin Cambios)

```bash
# Debe seguir funcionando igual que antes
curl -X POST https://gestiontimeapi.onrender.com/api/v1/auth/refresh \
  -H "Cookie: refresh_token=TOKEN_VALIDO"
```

**Esperado:** `200 OK` con cookies

### Test 2: Cliente Desktop (Nuevo)

```bash
# Ahora debe funcionar con JSON body
curl -X POST https://gestiontimeapi.onrender.com/api/v1/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"TOKEN_VALIDO"}'
```

**Esperado:** `200 OK` con tokens en JSON

### Test 3: Sin Token

```bash
# Debe fallar correctamente
curl -X POST https://gestiontimeapi.onrender.com/api/v1/auth/refresh
```

**Esperado:** `401 Unauthorized` - "No refresh token"

---

## 📊 Comparación

| Aspecto | Antes | Después |
|---------|-------|---------|
| **Web (cookies)** | ✅ Funciona | ✅ Funciona (sin cambios) |
| **Desktop (JSON)** | ❌ Falla 401 | ✅ Funciona |
| **Seguridad** | ✅ OK | ✅ OK (misma lógica) |
| **Retrocompatibilidad** | N/A | ✅ 100% compatible |

---

## 🔒 Consideraciones de Seguridad

1. ✅ **Validación idéntica** para ambos métodos
2. ✅ **Token rotation** funciona igual
3. ✅ **Logs diferenciados** (web vs desktop)
4. ✅ **No expone información** adicional
5. ✅ **Compatibilidad hacia atrás** preservada

---

## 📝 Logs Esperados

### Cliente Web:
```
[DBG] Refresh token obtenido desde cookie (cliente web)
[DBG] Procesando refresh token (hash: a1b2c3d4...)
[INF] Refresh exitoso para UserId: 123 (Roles: USER, Desktop: False)
```

### Cliente Desktop:
```
[DBG] Refresh token obtenido desde body JSON (cliente desktop)
[DBG] Procesando refresh token (hash: e5f6g7h8...)
[INF] Refresh exitoso para UserId: 456 (Roles: ADMIN, Desktop: True)
```

### Sin Token:
```
[WRN] Intento de refresh sin token (ni cookie ni body JSON)
```

---

## ⚡ Pasos Rápidos

1. **Abrir:** `C:\GestionTime\GestionTimeApi\Controllers\AuthController.cs`
2. **Buscar:** `[HttpPost("refresh")]`
3. **Reemplazar** el método completo con el código de arriba
4. **Agregar** el modelo `RefreshRequest` al final del archivo
5. **Guardar** y **compilar**
6. **Desplegar** a Render
7. **Testear** con la app desktop

---

## 🎉 Resultado Final

- ✅ Desktop podrá refrescar tokens automáticamente
- ✅ Web seguirá funcionando sin cambios
- ✅ UX mejorada: sesiones de 7-30 días sin re-login
- ✅ Logs claros para debugging
- ✅ Compatible con ambos tipos de clientes

**Tiempo estimado:** 5-10 minutos de implementación

---

*Fecha: 2026-01-06*  
*Estado: ✅ Solución lista para implementar*
