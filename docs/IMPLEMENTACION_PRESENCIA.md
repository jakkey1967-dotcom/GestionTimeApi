# 👥 Sistema de Presencia (Usuarios Online) - Implementación Completa

## 📋 Resumen

Se ha implementado un sistema completo de presencia (usuarios online) con cambios mínimos en el backend. El sistema permite:

- ✅ Tracking de sesiones de usuarios en tiempo real
- ✅ Actualización automática de presencia mediante middleware
- ✅ Consulta pública de usuarios online (cualquier autenticado)
- ✅ Endpoint admin para desconectar usuarios (kick)
- ✅ Logout desktop con revocación de sesión

---

## 🗂️ Archivos Creados/Modificados

### **Archivos Nuevos (7)**

| Archivo | Descripción |
|---------|-------------|
| `GestionTime.Domain/Auth/UserSession.cs` | Entidad para sesiones de usuario |
| `Middleware/PresenceMiddleware.cs` | Middleware para actualizar presencia |
| `Controllers/PresenceController.cs` | Endpoint público GET /users |
| `Controllers/AdminPresenceController.cs` | Endpoint admin POST /kick |
| `GestionTime.Infrastructure/Migrations/20260124090758_AddUserSessionsForPresence.cs` | Migración EF Core |
| `docs/IMPLEMENTACION_PRESENCIA.md` | Esta documentación |

### **Archivos Modificados (6)**

| Archivo | Cambios |
|---------|---------|
| `GestionTime.Domain/Auth/User.cs` | Agregado `List<UserSession> Sessions` |
| `GestionTime.Infrastructure/Persistence/GestionTimeDbContext.cs` | Configuración tabla user_sessions |
| `Security/JwtService.cs` | Sobrecarga con `sessionId` opcional |
| `Controllers/AuthController.cs` | Login-desktop crea sesión, logout-desktop |
| `Program.cs` | Registro middleware `UsePresenceTracking()` |

---

## 🗄️ Base de Datos

### **Nueva Tabla: `user_sessions`**

```sql
CREATE TABLE pss_dvnx.user_sessions (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES pss_dvnx.users(id) ON DELETE CASCADE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL,
    last_seen_at TIMESTAMP WITH TIME ZONE NOT NULL,
    revoked_at TIMESTAMP WITH TIME ZONE NULL, -- NULL = activa
    device_id VARCHAR(100),
    device_name VARCHAR(200),
    ip VARCHAR(45),
    user_agent VARCHAR(500)
);

-- Índices para performance
CREATE INDEX idx_sessions_user_id ON user_sessions(user_id);
CREATE INDEX idx_sessions_last_seen ON user_sessions(last_seen_at);
CREATE INDEX idx_sessions_user_active ON user_sessions(user_id, revoked_at);
```

### **Migración Aplicada**

```bash
dotnet ef migrations add AddUserSessionsForPresence
# Aplicar automáticamente en startup (Program.cs)
```

---

## 🔐 JWT Claims

### **Claim "sid" (Session ID)**

Se agrega automáticamente en login-desktop:

```csharp
claims.Add(new Claim("sid", sessionId.ToString()));
```

**Ejemplo de JWT decodificado:**
```json
{
  "nameid": "123e4567-e89b-12d3-a456-426614174000",
  "email": "admin@gestiontime.com",
  "name": "admin@gestiontime.com",
  "role": "ADMIN",
  "sid": "abc12345-6789-0def-1234-567890abcdef", // ✅ Session ID
  "exp": 1737716400,
  "iss": "GestionTimeAPI",
  "aud": "GestionTimeClient"
}
```

---

## 📡 Endpoints

### **1. POST `/api/v1/auth/login-desktop`** (Modificado)

**Cambios:**
- ✅ Crea sesión en `user_sessions`
- ✅ Incluye claim `sid` en JWT
- ✅ Retorna `sessionId` en JSON (debug)

**Request:**
```http
POST /api/v1/auth/login-desktop
Content-Type: application/json

{
  "email": "admin@gestiontime.com",
  "password": "Admin123!"
}
```

**Response:**
```json
{
  "message": "ok",
  "userName": "Admin Usuario",
  "userEmail": "admin@gestiontime.com",
  "userRole": "ADMIN",
  "mustChangePassword": false,
  "daysUntilPasswordExpires": 90,
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "abc123xyz...",
  "expiresAt": "2025-01-31T10:00:00Z",
  "sessionId": "abc12345-6789-0def-1234-567890abcdef"  // ✅ NUEVO
}
```

---

### **2. GET `/api/v1/presence/users`** (NUEVO)

Consulta lista completa de usuarios con presencia.

**Autorización:** Cualquier usuario autenticado

**Response:**
```json
[
  {
    "userId": "123e4567-e89b-12d3-a456-426614174000",
    "fullName": "Admin Usuario",
    "email": "admin@gestiontime.com",
    "role": "ADMIN",
    "lastSeenAt": "2025-01-24T09:05:30Z",
    "isOnline": true
  },
  {
    "userId": "987e6543-e21b-12d3-a456-426614174001",
    "fullName": "Editor Test",
    "email": "editor@gestiontime.com",
    "role": "EDITOR",
    "lastSeenAt": "2025-01-24T08:50:00Z",
    "isOnline": false
  }
]
```

**Lógica de `isOnline`:**
```
isOnline = Enabled && (lastSeenAt >= UtcNow - 2 minutos)
```

**Ordenamiento:**
1. `rolePriority` (ADMIN=0, EDITOR=1, USER=2)
2. `isOnline` descendente (online primero)
3. `fullName` ascendente (alfabético)

---

### **3. POST `/api/v1/admin/presence/users/{userId}/kick`** (NUEVO)

Revoca todas las sesiones activas de un usuario.

**Autorización:** Solo `ADMIN`

**Request:**
```http
POST /api/v1/admin/presence/users/987e6543-e21b-12d3-a456-426614174001/kick
Authorization: Bearer <admin_jwt>
```

**Response:**
```json
{
  "success": true,
  "message": "Se revocaron 2 sesión(es) activa(s)",
  "sessionsRevoked": 2,
  "userEmail": "editor@gestiontime.com"
}
```

**Logs generados:**
```
[WRN] Admin admin@gestiontime.com revocó 2 sesiones del usuario editor@gestiontime.com (UserId: 987...)
```

---

### **4. POST `/api/v1/auth/logout-desktop`** (NUEVO)

Logout para aplicación desktop.

**Autorización:** Usuario autenticado

**Request:**
```http
POST /api/v1/auth/logout-desktop
Authorization: Bearer <jwt_with_sid>
Content-Type: application/json

{
  "refreshToken": "abc123xyz..."  // Opcional
}
```

**Response:**
```json
{
  "message": "bye"
}
```

**Acciones:**
1. Revoca sesión actual (por claim `sid`)
2. Revoca refresh token (si se proporciona)

---

## 🔄 Middleware de Presencia

### **Ubicación en Pipeline**

```csharp
app.UseCors("WebClient");
app.UseAuthentication();      // ← Primero autenticar
app.UseAuthorization();
app.UsePresenceTracking();    // ← Después de auth
app.MapControllers();
```

### **Lógica de Throttle**

```
IF (User autenticado && tiene claim "sid"):
    IF (session.LastSeenAt < UtcNow - 30 segundos):
        UPDATE user_sessions SET last_seen_at = UtcNow
```

**Beneficio:** Reduce writes a BD (máximo 1 update cada 30s por sesión)

### **Logs Generados**

```
[TRC] Presencia actualizada para sesión abc12345... (último visto hace 45s)
[WRN] Sesión no encontrada o revocada: xyz98765...
```

---

## 🧪 Flujo de Uso Desktop

### **Escenario: Login → Actividad → Logout**

```
1️⃣ LOGIN
   POST /api/v1/auth/login-desktop
   → Crea sesión (LastSeenAt = UtcNow)
   → Retorna JWT con claim "sid"

2️⃣ ACTIVIDAD NORMAL (cada request con JWT)
   GET /api/v1/partes-trabajo
   → Middleware detecta "sid"
   → Actualiza LastSeenAt si >30s

3️⃣ CONSULTAR USUARIOS ONLINE
   GET /api/v1/presence/users
   → Ve todos los usuarios
   → isOnline = true si LastSeenAt < 2 min

4️⃣ ADMIN KICK (opcional)
   POST /api/v1/admin/presence/users/{userId}/kick
   → Revoca todas sesiones del usuario
   → Usuario ve error 401 en próximo request

5️⃣ LOGOUT
   POST /api/v1/auth/logout-desktop
   → Revoca sesión actual
   → Revoca refresh token
```

---

## 🔒 Seguridad

| Aspecto | Implementación |
|---------|----------------|
| **Autorización** | `GET /users` → cualquier autenticado, `POST /kick` → solo ADMIN |
| **CSRF** | JWT en header (no cookies), inmune a CSRF |
| **Session Hijacking** | `sid` en JWT firmado, validación en middleware |
| **Revocación** | Campo `RevokedAt` con índice para performance |
| **Logs** | Auditoría completa de kicks, logins, logouts |

---

## 📊 Performance

### **Índices Críticos**

```sql
idx_sessions_user_id       -- Para buscar sesiones por usuario
idx_sessions_last_seen     -- Para filtrar usuarios online
idx_sessions_user_active   -- Composite (user_id, revoked_at)
```

### **Query de Presencia (GET /users)**

```sql
-- Simplificado (EF genera similar)
SELECT 
    u.id,
    u.full_name,
    u.email,
    MAX(s.last_seen_at) as last_seen_at,
    (MAX(s.last_seen_at) >= NOW() - INTERVAL '2 minutes') as is_online
FROM users u
LEFT JOIN user_sessions s ON u.id = s.user_id AND s.revoked_at IS NULL
WHERE u.enabled = true
GROUP BY u.id
ORDER BY 
    CASE role WHEN 'ADMIN' THEN 0 WHEN 'EDITOR' THEN 1 ELSE 2 END,
    is_online DESC,
    u.full_name ASC;
```

**Tiempo estimado:** <50ms con 1000 usuarios (índices aplicados)

---

## 🧪 Testing

### **Pruebas Manuales**

#### **1. Verificar sesión creada en login**

```bash
# Login
curl -X POST http://localhost:2501/api/v1/auth/login-desktop \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@gestiontime.com","password":"Admin123!"}' \
  | jq '.sessionId'

# Verificar en BD
psql -d pss_dvnx -c "SELECT id, user_id, created_at, last_seen_at FROM pss_dvnx.user_sessions;"
```

#### **2. Verificar actualización de presencia**

```bash
# Hacer requests cada 5 segundos con JWT
for i in {1..10}; do
  curl -X GET http://localhost:2501/api/v1/presence/users \
    -H "Authorization: Bearer <jwt>"
  sleep 5
done

# Ver logs de middleware
[TRC] Presencia actualizada para sesión abc12345... (último visto hace 35s)
```

#### **3. Verificar kick de admin**

```bash
# Como admin
curl -X POST http://localhost:2501/api/v1/admin/presence/users/<userId>/kick \
  -H "Authorization: Bearer <admin_jwt>"

# Como usuario kickeado (siguiente request)
curl -X GET http://localhost:2501/api/v1/partes-trabajo \
  -H "Authorization: Bearer <user_jwt>"
# → 401 Unauthorized (sesión revocada)
```

---

## 🐛 Troubleshooting

### **Problema: `isOnline` siempre false**

**Causa:** Middleware no está actualizando `LastSeenAt`

**Solución:**
1. Verificar que JWT tiene claim `sid`:
   ```bash
   jwt.io → pegar accessToken → verificar "sid" en payload
   ```

2. Ver logs de middleware:
   ```
   [TRC] Presencia actualizada...  ✅ OK
   [WRN] Sesión no encontrada...   ❌ Problema
   ```

3. Verificar que sesión existe y no está revocada:
   ```sql
   SELECT * FROM pss_dvnx.user_sessions WHERE id = '<sid>' AND revoked_at IS NULL;
   ```

---

### **Problema: Usuarios no aparecen en `/presence/users`**

**Causa:** Usuario no habilitado o sin sesiones

**Solución:**
```sql
-- Verificar usuario habilitado
SELECT id, email, enabled FROM pss_dvnx.users WHERE email = 'test@example.com';

-- Verificar sesiones activas
SELECT * FROM pss_dvnx.user_sessions 
WHERE user_id = '<user_id>' AND revoked_at IS NULL;
```

---

### **Problema: Error 403 en kick**

**Causa:** Usuario no es ADMIN

**Solución:**
```sql
-- Verificar rol
SELECT u.email, r.name as role
FROM pss_dvnx.users u
JOIN pss_dvnx.user_roles ur ON u.id = ur.user_id
JOIN pss_dvnx.roles r ON ur.role_id = r.id
WHERE u.email = 'admin@gestiontime.com';
```

---

## 📦 Despliegue

### **Checklist Render**

- [x] Migración aplicada automáticamente
- [x] Variable `DB_SCHEMA=pss_dvnx` configurada
- [x] Middleware registrado en pipeline
- [ ] Verificar logs de aplicación de migración
- [ ] Probar login desktop desde WinUI
- [ ] Verificar GET /presence/users funciona

### **Verificación Post-Deploy**

```bash
# 1. Health check
curl https://gestiontimeapi.onrender.com/health | jq

# 2. Login desktop
curl -X POST https://gestiontimeapi.onrender.com/api/v1/auth/login-desktop \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@gestiontime.com","password":"Admin123!"}' \
  | jq '.sessionId'

# 3. Consultar usuarios
curl -X GET https://gestiontimeapi.onrender.com/api/v1/presence/users \
  -H "Authorization: Bearer <jwt>" \
  | jq 'map({email, role, isOnline})'
```

---

## 📚 Referencias Técnicas

| Concepto | Implementación |
|----------|----------------|
| **Throttle** | Middleware solo actualiza si >30s |
| **Online Threshold** | 2 minutos desde `LastSeenAt` |
| **Claim "sid"** | Session ID en JWT payload |
| **Cascade Delete** | Sesiones se borran si user deleted |
| **Max lastSeen** | Si múltiples sesiones, usa la más reciente |

---

## 🎉 Resultado Final

| Antes | Después |
|-------|---------|
| ❌ Sin tracking de sesiones | ✅ UserSession con migración |
| ❌ Sin presencia online | ✅ GET /presence/users |
| ❌ Admin sin control | ✅ POST /kick revoca sesiones |
| ❌ Logout solo web | ✅ logout-desktop funcional |
| ❌ Sin middleware | ✅ Middleware con throttle |

---

**Commit:** `c28f83c` - "feat: implementar sistema de presencia con UserSession, middleware y endpoints admin/public"

**Fecha:** 2025-01-24  
**Versión:** 1.2.0  
**Estado:** ✅ Implementado y pusheado a GitHub
