# 📋 TAREAS PENDIENTES

## ✅ COMPLETADO RECIENTEMENTE (2025-01-28)

### Fix de Presencia - Triple Mejora ✅
1. ✅ **Logout:** Revoca todas las sesiones activas → Usuario offline inmediatamente
2. ✅ **Login:** Limpia sesiones antiguas → Solo UNA sesión activa por usuario
3. ✅ **Health Check:** Actualiza presencia automáticamente → Todos los roles se mantienen online

**Archivos modificados:**
- `Controllers/AuthController.cs` - Método `Logout()` con revocación de sesiones
- `Controllers/AuthController.cs` - Método `LoginDesktop()` con limpieza de sesiones antiguas
- `Controllers/HealthController.cs` - Actualización automática de presencia en `/health`

**Problemas resueltos:**
1. **Logout:** Usuario permanecía online 30 segundos después de salir
2. **Login:** Sesiones antiguas acumuladas (múltiples sesiones simultáneas)
3. **Presencia:** `/health` no actualizaba `LastSeenAt` → Ahora lo hace automáticamente

**Ventajas del nuevo sistema:**
- ✅ **Cero cambios en Desktop** - Ya llama a `/health`, no requiere modificaciones
- ✅ **Universal** - Funciona para USER, EDITOR y ADMIN
- ✅ **Backward compatible** - Respuesta sigue siendo `{"status":"ok"}`
- ✅ **Eficiente** - Una sola petición actualiza presencia y health check
- ✅ **Transparente** - Side effect interno, no afecta API pública

**Próximos pasos:**
1. Reiniciar el backend: `dotnet run`
2. Probar login con `wsanchez@global-retail.com` (verificar limpieza de sesiones antiguas)
3. Probar logout (verificar que aparece offline inmediatamente)
4. Verificar que `/health` actualiza presencia cada vez que se llama
5. Ejecutar script de diagnóstico: `.\Scripts\Debug-PresenceSystem.ps1`

**Ver detalles completos:** `LOGOUT_PRESENCE_FIXED.md`

---

## 🚧 EN PAUSA - Sincronización de Ticket Headers de Freshdesk

**Estado:** ❌ NO FUNCIONA - Error 401 Unauthorized

### Problema Actual
```
Error de Freshdesk: Status=Unauthorized, Body={"code":"invalid_credentials","message":"You have to be logged in to perform this action."}
```

### Diagnóstico
- ✅ El endpoint `/api/v1/integrations/freshdesk/sync/ticket-headers` existe y compila
- ✅ El código de autenticación Basic Auth es correcto (`ApiKey:X` en Base64)
- ✅ El comando `curl` con `$env:FRESHDESK_API_KEY` funciona correctamente
- ❌ **El API Key configurado en `appsettings.Development.json` es DIFERENTE al de PowerShell**

### Lo que falta por hacer
1. **Corregir el API Key:**
   - Ejecutar: `$env:FRESHDESK_API_KEY` en PowerShell para ver el valor correcto
   - Copiar ese valor exacto a `appsettings.Development.json` en la sección `Freshdesk.ApiKey`
   - O configurar la variable de entorno `FRESHDESK_API_KEY` a nivel de sistema

2. **Crear las tablas de BD:**
   ```sql
   -- Ejecutar en PostgreSQL local:
   CREATE TABLE IF NOT EXISTS pss_dvnx.freshdesk_ticket_header (
     ticket_id            BIGINT PRIMARY KEY,
     subject              TEXT NOT NULL,
     status               INT NULL,
     priority             INT NULL,
     type                 TEXT NULL,
     requester_id         BIGINT NULL,
     responder_id         BIGINT NULL,
     group_id             BIGINT NULL,
     company_id           BIGINT NULL,
     created_at           TIMESTAMPTZ NOT NULL,
     updated_at           TIMESTAMPTZ NOT NULL,
     company_name         TEXT NULL,
     tags                 JSONB NULL,
     custom_fields        JSONB NULL
   );

   CREATE TABLE IF NOT EXISTS pss_dvnx.freshdesk_sync_state (
     scope               TEXT PRIMARY KEY,
     last_sync_at         TIMESTAMPTZ NULL,
     last_updated_since   TIMESTAMPTZ NULL,
     last_max_updated_at  TIMESTAMPTZ NULL,
     last_result_count    INT NULL,
     last_error           TEXT NULL
   );

   INSERT INTO pss_dvnx.freshdesk_sync_state(scope)
   VALUES ('ticket_headers')
   ON CONFLICT (scope) DO NOTHING;
   ```

3. **Descomentar código temporal:**
   - En `FreshdeskTicketHeaderSyncService.cs` hay código comentado con `⚠️ TEMPORAL`
   - Una vez corregido el API Key y creadas las tablas, descomentar:
     - `GetSyncStateAsync()` (línea ~54)
     - `SaveSyncStateAsync()` (líneas ~148 y ~172)

### Archivos Relacionados
- `Controllers/FreshdeskController.cs` - Endpoint `/api/v1/integrations/freshdesk/sync/ticket-headers`
- `GestionTime.Infrastructure/Services/Freshdesk/FreshdeskTicketHeaderSyncService.cs` - Servicio de sincronización
- `GestionTime.Infrastructure/Services/Freshdesk/FreshdeskClient.cs` - Cliente HTTP de Freshdesk API
- `scripts/test-ticket-headers-sync.ps1` - Script de prueba
- `scripts/create-freshdesk-ticket-header-tables.sql` - SQL para crear tablas
- `appsettings.Development.json` - Configuración del API Key

### Verificación Rápida
```powershell
# Ver API Key de PowerShell
Write-Host "PowerShell: $env:FRESHDESK_API_KEY"

# Ver API Key en appsettings
$json = Get-Content appsettings.Development.json | ConvertFrom-Json
Write-Host "appsettings: $($json.Freshdesk.ApiKey)"

# Deben ser IGUALES
```

### Referencias
- Documentación: `docs/FRESHDESK_TICKET_HEADER_SYNC.md`
- Error logs del último intento: Ver sección de logs más arriba

---

## ✅ COMPLETADAS ANTERIORMENTE

### Login Desktop
- ✅ Endpoint `/api/v1/auth/login-desktop` creado
- ✅ Retorna `accessToken` en el body JSON
- ✅ Compatible con PowerShell 5.1 (manejo de certificados SSL)

### Freshdesk Agent/Companies Sync
- ✅ Sincronización de agentes funciona
- ✅ Sincronización de compañías funciona
- ✅ Search from view funciona

---

**Fecha:** 2025-01-27  
**Próximos pasos:** Corregir API Key de Freshdesk y crear tablas de BD.
