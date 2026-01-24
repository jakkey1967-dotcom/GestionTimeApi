# ═══════════════════════════════════════════════════════════════════════════════
# 📋 RESUMEN COMPLETO - Sistema de Usuarios Online (PENDIENTE)
# ═══════════════════════════════════════════════════════════════════════════════
# Fecha: 2025-01-21
# Estado: FRONTEND COMPLETO ✅ | BACKEND PENDIENTE ⏳
# Para Retomar: Fin de semana
# ═══════════════════════════════════════════════════════════════════════════════

## ✅ LO QUE YA ESTÁ HECHO (FRONTEND)

### **1. Ventana de Usuarios Online**
- ✅ **Archivo:** `Views\UsersOnlineWindow.xaml`
- ✅ **Archivo:** `Views\UsersOnlineWindow.xaml.cs`
- ✅ **ViewModel:** `ViewModels\UsersOnlineViewModel.cs`
- ✅ **UI Completa:** Tarjetas con nombre, email, rol, indicador online/offline
- ✅ **Polling Automático:** Se actualiza cada 15 segundos

### **2. DTOs y Modelos**
- ✅ **Archivo:** `Models\Dtos\UsersListResponse.cs`
  - Campo `LastSeenAt` agregado
  - Lógica `IsOnline` implementada (< 2 minutos = Online)
  - Cambio de `Id` de `int` a `Guid` (compatible con backend)
  - Campo `roles` como array `string[]`

### **3. Servicios**
- ✅ **Archivo:** `Services\Presence\PresenceService.cs`
  - Método `GetUsersAsync()` con caché de 15 segundos
  - Método `PingAsync()` para enviar heartbeat al backend
  - Endpoints correctos: `/api/v1/admin/users` y `/api/v1/admin/ping`

### **4. Servicio de Administración**
- ✅ **Archivo:** `Services\Admin\AdminUsersService.cs`
  - Método `UpdateUserRoleAsync()` para cambiar roles
  - Endpoint: `PUT /api/v1/admin/users/{id}/roles`

### **5. Scripts y Documentación**
- ✅ **Script:** `Scripts\Test-AdminUsersEndpoint.ps1` - Para probar endpoint
- ✅ **Script:** `Scripts\Change-UserRole.ps1` - Para cambiar roles
- ✅ **Doc:** `Docs\CAMBIAR_ROL_USUARIOS.md` - Guía completa

---

## ⏳ LO QUE FALTA (BACKEND)

### **Estado Actual del Backend:**
- ❌ Campo `last_seen_at` NO existe en tabla `users`
- ❌ Endpoint `GET /api/v1/admin/ping` NO existe
- ❌ Endpoint `GET /api/v1/admin/users` NO incluye `lastSeenAt` en la respuesta

### **Por Eso:**
- ⚠️ La ventana de usuarios se abre pero TODOS aparecen como "Offline"
- ⚠️ No hay forma de detectar usuarios online porque falta `last_seen_at`

---

## 📂 ARCHIVOS CREADOS PARA EL FIN DE SEMANA

### **En Backend (C:\GestionTime\GestionTimeApi):**

1. **`docs\SQL-Backup-Render-Interno.sql`**
   - Backup rápido (tabla temporal)
   - Ejecutar ANTES de cualquier cambio

2. **`docs\SQL-Migration-AddLastSeenAt.sql`**
   - Migración para agregar columna `last_seen_at`
   - Crear índice para optimizar búsquedas

3. **`docs\IMPLEMENTAR-PRESENCIA-BACKEND.md`**
   - Instrucciones paso a paso
   - Código C# para actualizar `User.cs`
   - Código para actualizar `AdminUsersController.cs`
   - Endpoint `GET /api/v1/admin/ping`

4. **`docs\CHECKLIST-IMPLEMENTACION-PRESENCIA.md`**
   - Checklist completo con todos los pasos
   - Orden de ejecución recomendado
   - Scripts de rollback

5. **`docs\SISTEMA-ROLES-EXPLICACION.md`**
   - Documentación del sistema de roles
   - Estructura de base de datos
   - Endpoints disponibles

6. **`docs\SQL-ChangePSantosToAdmin.sql`**
   - Script para cambiar rol de psantos@global-retail.com a ADMIN

7. **`scripts\Backup-Render.ps1`**
   - Script PowerShell para hacer backup remoto

8. **`scripts\Backup-Simple.ps1`**
   - Script simplificado para backup local

---

## 🎯 PLAN PARA EL FIN DE SEMANA

### **OPCIÓN A: Backup + Migración en Render (Recomendado)**

#### **Paso 1: Conectar a Render Web Shell**
```sh
# En el Web Shell de Render, ejecutar:
PGPASSWORD=BvCDRFguh9SljJJUZOzGpdvpxgf18qnI psql -h dpg-d57tobm3jp1c73b6knog-a.oregon-postgres.render.com -U gestiontime pss_dvnx
```

#### **Paso 2: Ejecutar Backup**
```sql
-- Copiar y pegar contenido de:
C:\GestionTime\GestionTimeApi\docs\SQL-Backup-Render-Interno.sql
```

#### **Paso 3: Ejecutar Migración**
```sql
-- Copiar y pegar contenido de:
C:\GestionTime\GestionTimeApi\docs\SQL-Migration-AddLastSeenAt.sql
```

#### **Paso 4: Actualizar Código Backend**
```
Seguir instrucciones en:
C:\GestionTime\GestionTimeApi\docs\IMPLEMENTAR-PRESENCIA-BACKEND.md
```

---

### **OPCIÓN B: Hacerlo Todo Paso a Paso con Checklist**

```
Abrir y seguir:
C:\GestionTime\GestionTimeApi\docs\CHECKLIST-IMPLEMENTACION-PRESENCIA.md
```

---

## 🗂️ ARCHIVOS A MODIFICAR EN BACKEND

### **1. GestionTime.Domain\Auth\User.cs** (Línea ~11)
```csharp
// Agregar:
public DateTime? LastSeenAt { get; set; }

// Al final agregar:
public bool IsOnline => LastSeenAt.HasValue && 
    LastSeenAt.Value >= DateTime.UtcNow.AddMinutes(-2);
```

### **2. Controllers\AdminUsersController.cs**

**A) Línea ~62 - Actualizar SELECT:**
```csharp
.Select(u => new { u.Id, u.Email, u.FullName, u.Enabled, u.LastSeenAt })
```

**B) Línea ~77 - Actualizar resultado:**
```csharp
var result = users.Select(u => new
{
    u.Id,
    u.Email,
    u.FullName,
    u.Enabled,
    u.LastSeenAt,  // ✅ AGREGAR
    roles = rolesByUser.TryGetValue(u.Id, out var rr) ? rr : Array.Empty<string>()
});
```

**C) Al final del archivo - Agregar endpoint ping:**
```csharp
[HttpGet("ping")]
[Authorize]
public async Task<IActionResult> Ping()
{
    var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
    if (string.IsNullOrEmpty(userEmail))
        return Unauthorized();

    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
    if (user == null)
        return NotFound();

    user.LastSeenAt = DateTime.UtcNow;
    await db.SaveChangesAsync();

    _logger.LogDebug("Ping de {Email}", userEmail);
    return Ok(new { message = "Ping registrado", lastSeenAt = user.LastSeenAt });
}
```

**D) Agregar using al principio:**
```csharp
using System.Security.Claims;
```

---

## 🚨 ROLLBACK (Si algo sale mal)

### **Revertir Migración SQL:**
```sql
SET search_path TO pss_dvnx;

-- Eliminar índice
DROP INDEX IF EXISTS pss_dvnx.idx_users_last_seen_at;

-- Eliminar columna
ALTER TABLE pss_dvnx.users DROP COLUMN IF EXISTS last_seen_at;
```

### **Restaurar desde Backup:**
```sql
-- SOLO SI ES NECESARIO
DROP TABLE pss_dvnx.users;
ALTER TABLE pss_dvnx.users_backup_20250121 RENAME TO users;
```

---

## 🔍 VERIFICACIÓN DESPUÉS DE IMPLEMENTAR

### **1. Backend:**
```sh
# Probar endpoint GET /api/v1/admin/users
curl -H "Authorization: Bearer TOKEN" https://gestiontimeapi.onrender.com/api/v1/admin/users

# Debe incluir "lastSeenAt" en la respuesta
```

### **2. Frontend:**
```powershell
# Ejecutar aplicación
cd C:\GestionTime\GestionTimeDesktop
dotnet run

# Hacer login como ADMIN (psantos@global-retail.com)
# Verificar que la ventana de usuarios muestra:
#   - Círculo VERDE para usuarios con actividad < 2 min
#   - Círculo GRIS para usuarios con actividad > 2 min
```

---

## 📊 ESTADO ACTUAL

| Componente | Estado | Notas |
|------------|--------|-------|
| **Frontend Desktop** | ✅ COMPLETO | Listo para funcionar cuando backend esté actualizado |
| **Backend API** | ⏳ PENDIENTE | Necesita migración SQL + actualizar código |
| **Base de Datos** | ⏳ PENDIENTE | Necesita columna `last_seen_at` |
| **Documentación** | ✅ COMPLETA | Todos los archivos creados |
| **Scripts** | ✅ COMPLETOS | Listos para ejecutar |

---

## ⏱️ TIEMPO ESTIMADO (Fin de Semana)

| Tarea | Tiempo |
|-------|--------|
| Backup de BD | 5 min |
| Migración SQL | 5 min |
| Actualizar código backend | 15 min |
| Compilar y probar backend | 10 min |
| Desplegar backend | 10 min |
| Probar frontend | 10 min |
| **TOTAL** | **~1 hora** |

---

## 🎯 PRÓXIMOS PASOS (Orden Recomendado)

1. ✅ **Leer este documento completo**
2. ⏳ **Fin de semana:** Hacer backup en Render
3. ⏳ **Fin de semana:** Ejecutar migración SQL
4. ⏳ **Fin de semana:** Actualizar código backend
5. ⏳ **Fin de semana:** Desplegar backend
6. ⏳ **Fin de semana:** Probar frontend
7. ✅ **Lunes:** Si todo funciona, actualizar versión y publicar

---

## 📞 CONTACTO PARA DUDAS

Todos los archivos de documentación están en:
- **Backend:** `C:\GestionTime\GestionTimeApi\docs\`
- **Scripts:** `C:\GestionTime\GestionTimeApi\scripts\`
- **Checklist:** `CHECKLIST-IMPLEMENTACION-PRESENCIA.md`

---

## ✅ VENTAJAS DE ESTA IMPLEMENTACIÓN

1. ✅ **Retrocompatible** - Versiones viejas del frontend seguirán funcionando
2. ✅ **Segura** - Solo agrega columna nullable, no rompe nada
3. ✅ **Rollback fácil** - Backup en tabla temporal
4. ✅ **Cero downtime** - La migración se hace en caliente
5. ✅ **Frontend ya listo** - Solo falta actualizar backend

---

**Fecha de creación:** 2025-01-21  
**Para retomar:** Fin de semana  
**Prioridad:** Media (funcionalidad nueva, no crítica)  
**Riesgo:** Bajo (migración segura con backup)

═══════════════════════════════════════════════════════════════════════════════
