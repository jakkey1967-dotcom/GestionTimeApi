# 📋 Implementación de Sincronización de Agentes de Freshdesk

## ✅ Cambios Implementados

### 1. **Nuevo Servicio: `FreshdeskAgentsSyncService`**
**Archivo:** `GestionTime.Infrastructure/Services/Freshdesk/FreshdeskAgentsSyncService.cs`

**Funcionalidad:**
- Sincroniza TODOS los agentes desde Freshdesk (`GET /api/v2/agents`)
- Hace UPSERT en la tabla `freshdesk_agents_cache`
- Soporta paginación automática
- Implementa rate limiting con delays de 500ms entre páginas
- Retorna métricas detalladas (páginas, registros, duración)

**Métodos principales:**
- `SyncAllAsync()` - Sincronización completa de agentes
- `GetStatusAsync()` - Estado actual de la cache (total, activos, fechas)
- `EnsureTableExistsAsync()` - Crea la tabla si no existe
- `UpsertAgentsAsync()` - Inserta o actualiza agentes

---

### 2. **Extensión de DTOs**
**Archivo:** `GestionTime.Domain/Freshdesk/FreshdeskTicketDto.cs`

**Cambios en `FreshdeskAgentDto`:**
```csharp
public class FreshdeskAgentDto
{
    public long Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }              // ✅ NUEVO
    public bool? Available { get; set; }           // ✅ NUEVO
    public string? Language { get; set; }          // ✅ NUEVO
    public string? TimeZone { get; set; }          // ✅ NUEVO
    public DateTime? CreatedAt { get; set; }       // ✅ NUEVO
    public DateTime? UpdatedAt { get; set; }       // ✅ NUEVO
    public DateTime? LastLoginAt { get; set; }     // ✅ NUEVO
    public FreshdeskAgentContactDto? Contact { get; set; }  // ✅ NUEVO
}

public class FreshdeskAgentContactDto            // ✅ NUEVO
{
    public bool? Active { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? Mobile { get; set; }
    public string? Phone { get; set; }
}
```

---

### 3. **Nuevo Método en `FreshdeskClient`**
**Archivo:** `GestionTime.Infrastructure/Services/Freshdesk/FreshdeskClient.cs`

**Método agregado:**
```csharp
public async Task<List<FreshdeskAgentDto>> GetAgentsPageAsync(
    int page = 1, 
    int perPage = 100, 
    CancellationToken ct = default)
```

**Características:**
- Llama a `/api/v2/agents?per_page={perPage}&page={page}`
- Logging detallado de requests/responses
- Manejo de rate limits (429)
- Retry automático configurable

---

### 4. **Nuevos Endpoints en API**
**Archivo:** `Controllers/FreshdeskController.cs`

#### **a) POST `/api/v1/integrations/freshdesk/agents/sync`**
- **Requiere:** Role ADMIN
- **Función:** Sincroniza todos los agentes desde Freshdesk
- **Respuesta:**
  ```json
  {
    "success": true,
    "pagesFetched": 3,
    "agentsUpserted": 25,
    "durationMs": 1850,
    "sampleFirst3": [
      {
        "agent_id": 12345,
        "name": "John Doe",
        "email": "john@company.com"
      }
    ],
    "startedAt": "2024-01-30T10:00:00Z",
    "completedAt": "2024-01-30T10:00:02Z"
  }
  ```

#### **b) GET `/api/v1/integrations/freshdesk/agents/status`**
- **Requiere:** Usuario autenticado
- **Función:** Obtiene estado de la cache de agentes
- **Respuesta:**
  ```json
  {
    "success": true,
    "totalAgents": 25,
    "activeAgents": 22,
    "maxUpdatedAt": "2024-01-30T09:30:00Z",
    "maxSyncedAt": "2024-01-30T10:00:02Z"
  }
  ```

---

### 5. **Registro de Servicio en DI**
**Archivo:** `Program.cs`

**Línea agregada:**
```csharp
builder.Services.AddScoped<GestionTime.Infrastructure.Services.Freshdesk.FreshdeskAgentsSyncService>();
```

---

### 6. **Base de Datos**

#### **Nueva Tabla: `freshdesk_agents_cache`**
**Script:** `scripts/create-freshdesk-agents-table.sql`

**Estructura:**
```sql
CREATE TABLE pss_dvnx.freshdesk_agents_cache (
  agent_id              bigint PRIMARY KEY,
  agent_email           text NOT NULL,
  agent_name            text NULL,
  agent_type            text NULL,
  is_active             boolean NULL,
  language              text NULL,
  time_zone             text NULL,
  mobile                text NULL,
  phone                 text NULL,
  last_login_at         timestamptz NULL,
  freshdesk_created_at  timestamptz NULL,
  freshdesk_updated_at  timestamptz NULL,
  raw                   jsonb NOT NULL,
  synced_at             timestamptz NOT NULL DEFAULT NOW()
);
```

**Índices:**
- `ix_fd_agents_email` - Búsqueda por email
- `ix_fd_agents_active` - Filtro de agentes activos
- `ix_fd_agents_updated_at` - Ordenamiento por fecha de actualización
- `ix_fd_agents_synced_at` - Auditoría de sincronización

---

### 7. **Scripts de Prueba y Verificación**

#### **a) `scripts/test-freshdesk-agents.ps1`**
- Test completo de sincronización
- Login como ADMIN
- Ejecuta sincronización
- Verifica estado final
- Muestra sample de agentes

#### **b) `scripts/verify-freshdesk-agents.sql`**
- Estadísticas generales
- Distribución por tipo, idioma, timezone
- Últimas sincronizaciones
- Agentes activos recientemente
- Tamaño de tabla e índices

#### **c) `scripts/test-freshdesk-agents-permissions.ps1`**
- Verifica permisos del API Key
- Informa si el agente actual es admin
- Guía de troubleshooting

---

## 🔄 Flujo de Sincronización

```
1. POST /api/v1/integrations/freshdesk/agents/sync (como ADMIN)
   ↓
2. FreshdeskAgentsSyncService.SyncAllAsync()
   ↓
3. Loop paginado:
   - FreshdeskClient.GetAgentsPageAsync(page)
   - FreshdeskAgentsSyncService.UpsertAgentsAsync(agents)
   - Delay 500ms (rate limiting)
   ↓
4. Retorna AgentsSyncResult con métricas
```

---

## 📊 Diferencias con `/api/v2/agents/me`

| Feature | `/agents/me` | `/agents` |
|---------|-------------|-----------|
| **Endpoint** | `GET /api/v2/agents/me` | `GET /api/v2/agents` |
| **Requiere Admin** | ❌ No | ✅ Sí |
| **Retorna** | 1 agente (actual) | Todos los agentes |
| **Tabla** | `freshdesk_agent_me_cache` | `freshdesk_agents_cache` |
| **Uso** | Identidad del API Key | Listar todos los técnicos |
| **Sincronización** | Cualquier usuario | Solo ADMIN |

---

## ⚙️ Configuración Requerida

### Permisos en Freshdesk
El API Key debe pertenecer a un agente con rol **Account Administrator** para poder:
- Listar todos los agentes (`GET /api/v2/agents`)
- Ver información completa de cada agente

### Variables de Entorno
Ya configuradas en `appsettings.json`:
```json
{
  "Freshdesk": {
    "BaseUrl": "https://your-domain.freshdesk.com",
    "ApiKey": "your-api-key-here",
    "PerPage": 100
  }
}
```

---

## 🧪 Cómo Probar

### 1. **Verificar Permisos**
```powershell
.\scripts\test-freshdesk-agents-permissions.ps1
```

### 2. **Crear Tabla**
```sql
-- En PostgreSQL
\i scripts/create-freshdesk-agents-table.sql
```

### 3. **Ejecutar Sincronización**
```powershell
.\scripts\test-freshdesk-agents.ps1
```

### 4. **Verificar Resultados**
```sql
-- En PostgreSQL
\i scripts/verify-freshdesk-agents.sql
```

---

## 🔍 Swagger

Los nuevos endpoints aparecerán en Swagger (`/swagger/index.html`):

- **POST** `/api/v1/integrations/freshdesk/agents/sync` 🔒 (ADMIN)
- **GET** `/api/v1/integrations/freshdesk/agents/status` 🔒 (Autenticado)

---

## 📝 Notas Importantes

1. **Rate Limiting:** Freshdesk limita a 50 requests/minuto. El servicio implementa delays automáticos.

2. **Permisos:** Solo usuarios con role ADMIN pueden sincronizar. Cualquier usuario autenticado puede consultar el status.

3. **Idempotencia:** La sincronización usa UPSERT, por lo que es seguro ejecutarla múltiples veces.

4. **Cache Local:** Los datos se guardan en PostgreSQL para reducir llamadas a la API de Freshdesk.

5. **Sincronización Automática:** Si deseas sincronizar periódicamente, puedes agregar un job en `FreshdeskSyncBackgroundService`.

---

## 🎯 Próximos Pasos

Si quieres sincronización automática periódica:
1. Editar `FreshdeskSyncBackgroundService.cs`
2. Agregar task para `FreshdeskAgentsSyncService.SyncAllAsync()`
3. Configurar intervalo (ej: cada 24 horas)

Ejemplo:
```csharp
// En FreshdeskSyncBackgroundService.ExecuteAsync()
var agentsSyncTask = Task.Run(async () =>
{
    while (!stoppingToken.IsCancellationRequested)
    {
        await _agentsSyncService.SyncAllAsync(stoppingToken);
        await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
    }
});
```

---

## ✅ Resumen

- ✅ Servicio `FreshdeskAgentsSyncService` creado
- ✅ Endpoints REST agregados (`/agents/sync`, `/agents/status`)
- ✅ Tabla `freshdesk_agents_cache` definida
- ✅ Scripts de prueba y verificación creados
- ✅ DTOs extendidos para soportar campos completos
- ✅ Método `GetAgentsPageAsync()` agregado al client
- ✅ Servicio registrado en DI (`Program.cs`)
- ✅ Documentación completa

**Los endpoints ahora aparecerán en Swagger.** 🎉
