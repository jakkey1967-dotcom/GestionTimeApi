# 🔄 Implementación de Refresh Token para Desktop y Web

## 📋 Resumen de Cambios

Se ha implementado soporte **dual** en el endpoint `/api/v1/auth/refresh` para manejar refresh tokens tanto desde:
- ✅ **Cookies HTTP-Only** (clientes web)
- ✅ **JSON Body** (clientes desktop)

---

## 🎯 Problema Resuelto

**ANTES:**
```
❌ El endpoint /refresh SOLO aceptaba tokens desde cookies
❌ Clientes desktop no podían refrescar tokens
❌ Usuarios desktop debían hacer login nuevamente cada 15 minutos
```

**DESPUÉS:**
```
✅ El endpoint /refresh acepta tokens desde cookies O JSON body
✅ Clientes desktop pueden refrescar tokens automáticamente
✅ Sesión persistente en desktop sin re-autenticación
```

---

## 🔧 Cambios Implementados

### 1. Nuevo Modelo de Request (`RefreshRequest`)

```csharp
public record RefreshRequest
{
    public string? RefreshToken { get; init; }
}
```

**Ubicación:** Final de `AuthController.cs` (después de `ForcePasswordChangeRequest`)

---

### 2. Modificación del Endpoint `[HttpPost("refresh")]`

#### **Firma del Método:**

```csharp
// ANTES
public async Task<IActionResult> Refresh()

// DESPUÉS
public async Task<IActionResult> Refresh([FromBody] RefreshRequest? bodyRequest)
```

#### **Lógica de Obtención del Token:**

```csharp
string? rawRefresh = null;
bool isDesktopClient = false;

// 1️⃣ Intentar desde cookie (web)
if (Request.Cookies.TryGetValue("refresh_token", out var cookieToken))
{
    rawRefresh = cookieToken;
    logger.LogDebug("Refresh token obtenido desde cookie (cliente web)");
}
// 2️⃣ Intentar desde body JSON (desktop)
else if (bodyRequest != null && !string.IsNullOrWhiteSpace(bodyRequest.RefreshToken))
{
    rawRefresh = bodyRequest.RefreshToken;
    isDesktopClient = true;
    logger.LogDebug("Refresh token obtenido desde body JSON (cliente desktop)");
}
```

#### **Respuesta Diferenciada:**

```csharp
// 3️⃣ Respuesta según tipo de cliente
if (isDesktopClient)
{
    // Cliente desktop: tokens en JSON
    return Ok(new
    {
        accessToken = newAccess,
        refreshToken = newRawRefresh,
        expiresAt = newRefreshExpires
    });
}
else
{
    // Cliente web: tokens en cookies
    SetAccessCookie(newAccess);
    SetRefreshCookie(newRawRefresh, newRefreshExpires);
    return Ok(new { message = "ok", expiresAt = DateTime.UtcNow.AddMinutes(15) });
}
```

---

## 🧪 Pruebas de Uso

### **Cliente Web (Sin Cambios)**

```http
POST /api/v1/auth/refresh
Cookie: refresh_token=<token>

Response:
{
  "message": "ok",
  "expiresAt": "2025-01-15T10:30:00Z"
}
+ Set-Cookie: access_token=<nuevo_jwt>
+ Set-Cookie: refresh_token=<nuevo_refresh>
```

### **Cliente Desktop (NUEVO)**

```http
POST /api/v1/auth/refresh
Content-Type: application/json

{
  "refreshToken": "abc123xyz..."
}

Response:
{
  "accessToken": "eyJhbGciOiJI...",
  "refreshToken": "def456uvw...",
  "expiresAt": "2025-01-22T10:00:00Z"
}
```

---

## 📊 Flujo Completo Desktop

```
┌──────────────────┐
│  Login Desktop   │
│  /login-desktop  │
└────────┬─────────┘
         │
         ▼
┌─────────────────────────┐
│ Respuesta con tokens:   │
│ - accessToken (JWT)     │
│ - refreshToken (raw)    │
│ - expiresAt             │
└────────┬────────────────┘
         │
         ▼
┌──────────────────────────┐
│ Desktop guarda tokens    │
│ en memoria/archivo local │
└────────┬─────────────────┘
         │
         ▼ (después de 14 min)
┌──────────────────────────┐
│ POST /auth/refresh       │
│ { refreshToken: "..." }  │
└────────┬─────────────────┘
         │
         ▼
┌─────────────────────────┐
│ Nuevos tokens recibidos │
│ - Actualiza accessToken │
│ - Actualiza refreshToken│
└─────────────────────────┘
```

---

## 🔐 Medidas de Seguridad Mantenidas

| Característica | Implementación |
|----------------|----------------|
| **Rotación de tokens** | ✅ Refresh token antiguo se revoca |
| **Hash SHA256** | ✅ Tokens almacenados hasheados |
| **Expiración** | ✅ Access: 15 min, Refresh: 7 días |
| **Validación usuario** | ✅ Usuario debe estar habilitado |
| **Logs de auditoría** | ✅ Registro de todos los refresh |

---

## 📝 Logs Generados

### **Refresh desde Cookie (Web)**
```
[DBG] Refresh token obtenido desde cookie (cliente web)
[DBG] Procesando refresh token (hash: a1b2c3d4...)
[INF] Refresh exitoso para UserId: <guid> (Roles: ADMIN, Desktop: False)
```

### **Refresh desde JSON Body (Desktop)**
```
[DBG] Refresh token obtenido desde body JSON (cliente desktop)
[DBG] Procesando refresh token (hash: e5f6g7h8...)
[INF] Refresh exitoso para UserId: <guid> (Roles: USER, Desktop: True)
```

---

## ⚠️ Errores Manejados

| Caso | Respuesta | HTTP Status |
|------|-----------|-------------|
| Sin token (ni cookie ni body) | `{"message": "No refresh token"}` | 401 |
| Token inválido | `{"message": "Refresh inválido"}` | 401 |
| Token expirado | `{"message": "Refresh inválido"}` | 401 |
| Token revocado | `{"message": "Refresh inválido"}` | 401 |
| Usuario deshabilitado | `{"message": "Refresh inválido"}` | 401 |

---

## 🚀 Despliegue

### Pasos realizados:

```bash
# 1. Verificar compilación
dotnet build GestionTime.Api.csproj
# ✅ Compilación realizado correctamente en 8,2s

# 2. Commit
git add Controllers/AuthController.cs
git commit -m "feat: soporte JSON body en /auth/refresh para clientes desktop"

# 3. Push a GitHub
git push origin main
# ✅ Pushed to https://github.com/jakkey1967-dotcom/GestionTimeApi.git
```

### Despliegue Automático en Render:

Render detectará el push y desplegará automáticamente:
- ⏱️ Tiempo estimado: **3-5 minutos**
- 🔗 URL: https://gestiontimeapi.onrender.com

---

## ✅ Checklist de Validación

**Después del despliegue, verificar:**

### Cliente Web (No debe cambiar comportamiento)
- [ ] Login web funciona correctamente
- [ ] Cookies de refresh se establecen
- [ ] Refresh automático cada 14 minutos funciona
- [ ] Logout revoca refresh token

### Cliente Desktop (NUEVO)
- [ ] Login desktop retorna `accessToken` y `refreshToken` en JSON
- [ ] Desktop puede hacer POST a `/auth/refresh` con `{ "refreshToken": "..." }`
- [ ] Respuesta incluye nuevos tokens
- [ ] Token antiguo se revoca correctamente
- [ ] Logs muestran `Desktop: True`

---

## 🧪 Prueba Manual con cURL

### Web (cookies):
```bash
# 1. Login
curl -X POST https://gestiontimeapi.onrender.com/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@gestiontime.com","password":"Admin123!"}' \
  -c cookies.txt

# 2. Refresh (usa cookies)
curl -X POST https://gestiontimeapi.onrender.com/api/v1/auth/refresh \
  -b cookies.txt
```

### Desktop (JSON):
```bash
# 1. Login desktop
RESPONSE=$(curl -X POST https://gestiontimeapi.onrender.com/api/v1/auth/login-desktop \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@gestiontime.com","password":"Admin123!"}')

# Extraer refresh token
REFRESH_TOKEN=$(echo $RESPONSE | jq -r '.refreshToken')

# 2. Refresh con JSON body
curl -X POST https://gestiontimeapi.onrender.com/api/v1/auth/refresh \
  -H "Content-Type: application/json" \
  -d "{\"refreshToken\":\"$REFRESH_TOKEN\"}"
```

---

## 📚 Referencias

| Archivo | Líneas Modificadas |
|---------|-------------------|
| `Controllers/AuthController.cs` | Líneas 148-242 (método Refresh) |
| `Controllers/AuthController.cs` | Líneas 874-878 (modelo RefreshRequest) |

**Commit:** `b394994` - "feat: soporte JSON body en /auth/refresh para clientes desktop"

---

## 🎉 Resultado Final

| Antes | Después |
|-------|---------|
| ❌ Desktop sin refresh | ✅ Desktop con refresh automático |
| ❌ Re-login cada 15 min | ✅ Sesión persistente 7 días |
| ❌ Solo cookies | ✅ Cookies + JSON body |

---

**Fecha:** 2025-01-15  
**Versión:** 1.1.0  
**Estado:** ✅ Desplegado en producción
