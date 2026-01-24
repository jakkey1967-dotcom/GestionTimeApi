# ═══════════════════════════════════════════════════════════════════════════════
# CHECKLIST COMPLETO - Implementación de Sistema de Presencia
# ═══════════════════════════════════════════════════════════════════════════════

## 🔧 BACKEND (C:\GestionTime\GestionTimeApi)

### ✅ Fase 1: Preparación
- [ ] **CRÍTICO: Hacer backup de la base de datos**
  ```bash
  cd C:\GestionTime\GestionTimeApi
  .\scripts\Backup-Database.ps1
  ```

### ✅ Fase 2: Migración de Base de Datos
- [ ] Ejecutar migración SQL en **DESARROLLO** primero
  ```sql
  -- Archivo: docs\SQL-Migration-AddLastSeenAt.sql
  SET search_path TO pss_dvnx;
  ALTER TABLE pss_dvnx.users ADD COLUMN IF NOT EXISTS last_seen_at TIMESTAMP WITH TIME ZONE;
  CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_users_last_seen_at ON pss_dvnx.users(last_seen_at) WHERE last_seen_at IS NOT NULL;
  ```

- [ ] Verificar que funciona en desarrollo
  ```sql
  SELECT id, email, full_name, last_seen_at FROM pss_dvnx.users LIMIT 5;
  ```

- [ ] Ejecutar migración SQL en **PRODUCCIÓN** (después de probar)

### ✅ Fase 3: Actualizar Código Backend
- [ ] **Archivo:** `GestionTime.Domain\Auth\User.cs`
  ```csharp
  // Agregar después de línea 11:
  public DateTime? LastSeenAt { get; set; }
  
  // Agregar al final:
  public bool IsOnline => LastSeenAt.HasValue && 
      LastSeenAt.Value >= DateTime.UtcNow.AddMinutes(-2);
  ```

- [ ] **Archivo:** `Controllers\AdminUsersController.cs`
  
  **A) Actualizar endpoint GET** (línea ~62):
  ```csharp
  .Select(u => new { u.Id, u.Email, u.FullName, u.Enabled, u.LastSeenAt })
  ```
  
  **B) Actualizar resultado** (línea ~77):
  ```csharp
  var result = users.Select(u => new
  {
      u.Id,
      u.Email,
      u.FullName,
      u.Enabled,
      u.LastSeenAt,  // ✅ NUEVO
      roles = rolesByUser.TryGetValue(u.Id, out var rr) ? rr : Array.Empty<string>()
  });
  ```
  
  **C) Agregar endpoint de ping** (al final del archivo):
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
  
  **D) Agregar using** (al principio):
  ```csharp
  using System.Security.Claims;
  ```

### ✅ Fase 4: Compilar y Probar Backend
- [ ] Compilar proyecto
  ```bash
  cd C:\GestionTime\GestionTimeApi
  dotnet build
  ```

- [ ] Ejecutar en desarrollo
  ```bash
  dotnet run
  ```

- [ ] Probar endpoint GET /api/v1/admin/users (debe incluir `lastSeenAt`)
- [ ] Probar endpoint GET /api/v1/admin/ping

### ✅ Fase 5: Desplegar Backend a Producción
- [ ] Commit y push de cambios
  ```bash
  git add .
  git commit -m "feat: Agregar soporte de presencia (last_seen_at)"
  git push
  ```

- [ ] Desplegar en Render/Azure/etc.
- [ ] Verificar que funciona en producción

---

## 💻 FRONTEND (C:\GestionTime\GestionTimeDesktop)

### ✅ YA ESTÁ IMPLEMENTADO ✅

Los siguientes archivos **YA están listos** en el frontend:

1. **Models/Dtos/UsersListResponse.cs**
   - ✅ Ya tiene `LastSeenAt` property
   - ✅ Ya tiene lógica `IsOnline`

2. **Services/Presence/PresenceService.cs**
   - ✅ Ya tiene `GetUsersAsync()` con caché
   - ✅ Ya tiene `PingAsync()` para enviar heartbeat

3. **ViewModels/UsersOnlineViewModel.cs**
   - ✅ Ya tiene lógica de refresh automático (15s)
   - ✅ Ya mapea correctamente `UserCardItem`

4. **Views/UsersOnlineWindow.xaml**
   - ✅ Ya tiene UI con indicadores visuales (círculo verde/gris)
   - ✅ Ya muestra "Online" / "Offline"

### ✅ Lo Único Pendiente en Frontend:
- [ ] **Actualizar versión** en `Directory.Build.props`:
  ```xml
  <AppVersion>1.5.1-beta</AppVersion>
  <AppVersionNumeric>1.5.1.0</AppVersionNumeric>
  ```

- [ ] **Compilar y probar** que funciona con backend actualizado
  ```bash
  cd C:\GestionTime\GestionTimeDesktop
  dotnet build
  dotnet run
  ```

- [ ] **Crear instalador** para distribución
  ```bash
  .\Build-Installer.ps1
  ```

---

## 🧪 TESTING

### Backend:
- [ ] GET /api/v1/admin/users devuelve `lastSeenAt`
- [ ] GET /api/v1/admin/ping actualiza `last_seen_at`
- [ ] Verificar en base de datos que se actualiza el timestamp
  ```sql
  SELECT email, last_seen_at FROM pss_dvnx.users ORDER BY last_seen_at DESC NULLS LAST;
  ```

### Frontend:
- [ ] Login como usuario ADMIN
- [ ] Ventana de usuarios se abre automáticamente
- [ ] Usuarios con actividad < 2 min aparecen con círculo verde (Online)
- [ ] Usuarios con actividad > 2 min aparecen con círculo gris (Offline)
- [ ] Se actualiza automáticamente cada 15 segundos

---

## 🚨 ROLLBACK (Si algo sale mal)

### Backend:
```sql
-- Revertir migración
SET search_path TO pss_dvnx;
DROP INDEX IF EXISTS pss_dvnx.idx_users_last_seen_at;
ALTER TABLE pss_dvnx.users DROP COLUMN IF EXISTS last_seen_at;
```

### Frontend:
- Volver a versión anterior (1.5.0-beta)
- Los usuarios pueden seguir usando la app sin problemas

---

## 📊 ORDEN DE EJECUCIÓN RECOMENDADO

```
1️⃣ BACKUP de base de datos                    ← BACKEND
2️⃣ Actualizar código del backend              ← BACKEND
3️⃣ Desplegar backend a producción             ← BACKEND
4️⃣ Ejecutar migración SQL en producción       ← BACKEND
5️⃣ Verificar que backend funciona             ← BACKEND
6️⃣ Actualizar versión del frontend            ← FRONTEND
7️⃣ Compilar y probar frontend                 ← FRONTEND
8️⃣ Crear instalador                           ← FRONTEND
9️⃣ Publicar release en GitHub                 ← FRONTEND
🔟 Notificar a usuarios (opcional)             ← FRONTEND
```

---

## ⏱️ TIEMPO ESTIMADO

| Tarea | Tiempo |
|-------|--------|
| Backup de BD | 2-5 min |
| Actualizar código backend | 10 min |
| Probar en desarrollo | 15 min |
| Desplegar backend | 5-10 min |
| Ejecutar migración SQL | 5 min |
| Actualizar frontend | 5 min |
| Testing completo | 20 min |
| **TOTAL** | **~1 hora** |

---

## ✅ CHECKLIST FINAL

Antes de considerar terminado:

- [ ] ✅ Backup de BD guardado y verificado
- [ ] ✅ Migración SQL exitosa en desarrollo
- [ ] ✅ Backend desplegado y funcionando
- [ ] ✅ Migración SQL exitosa en producción
- [ ] ✅ Endpoint GET /api/v1/admin/users devuelve `lastSeenAt`
- [ ] ✅ Endpoint GET /api/v1/admin/ping funciona
- [ ] ✅ Frontend muestra estado Online/Offline correctamente
- [ ] ✅ Testing completo realizado
- [ ] ✅ Documentación actualizada
- [ ] ✅ Release notes creados

---

**Fecha:** 2025-01-21  
**Versión Backend:** 1.5.1-beta  
**Versión Frontend:** 1.5.1-beta
