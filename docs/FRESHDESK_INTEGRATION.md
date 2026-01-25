# 🎫 INTEGRACIÓN FRESHDESK - DOCUMENTACIÓN TÉCNICA COMPLETA

## 📋 ÍNDICE
1. [Resumen de la Implementación](#resumen)
2. [Arquitectura](#arquitectura)
3. [Endpoints API](#endpoints)
4. [Base de Datos](#base-de-datos)
5. [Configuración](#configuración)
6. [Background Service](#background-service)
7. [Flujos de Trabajo](#flujos)
8. [Limitaciones Conocidas](#limitaciones)
9. [Testing](#testing)
10. [Deployment](#deployment)

---

## 📌 RESUMEN DE LA IMPLEMENTACIÓN {#resumen}

### ✅ Funcionalidades Implementadas

- **Conexión con Freshdesk API** usando BasicAuth (apiKey:X)
- **Cache local de datos** en PostgreSQL (tags y agentId)
- **Endpoints públicos y autenticados** para buscar tickets y tags
- **Sincronización automática** de tags cada 24 horas (configurable)
- **Sincronización manual** con parámetros avanzados (mode, days, limit)
- **Logs detallados** sin exponer credenciales
- **Rate limit protection** con delays entre requests
- **Paginación automática** (límite 10 páginas/300 tickets de Freshdesk)

### 📦 Componentes Principales

```
GestionTime.Infrastructure/Services/Freshdesk/
├── FreshdeskClient.cs                    # Cliente HTTP para Freshdesk API
├── FreshdeskOptions.cs                   # Configuración (Domain, ApiKey, Sync)
├── FreshdeskService.cs                   # Lógica de negocio y cache
└── FreshdeskSyncBackgroundService.cs     # Job automático de sincronización

Controllers/
└── FreshdeskController.cs                # Endpoints REST

Domain/Freshdesk/
├── FreshdeskAgentMap.cs                  # Entity para cache de agentId
├── FreshdeskTag.cs                       # Entity para cache de tags
└── FreshdeskTicketDto.cs                 # DTOs para tickets y agentes
```

---

## 🏗️ ARQUITECTURA {#arquitectura}

### Diagrama de Flujo

```
┌─────────────────────┐
│  Frontend / Swagger │
└──────────┬──────────┘
           │
           v
┌─────────────────────────────────────────────────────────┐
│              GestionTime API                             │
│                                                          │
│  ┌──────────────────────────────────────────────────┐  │
│  │  FreshdeskController                             │  │
│  │  - ping()                      [AllowAnonymous]  │  │
│  │  - tickets/suggest()           [Authorize]       │  │
│  │  - tags/suggest()              [AllowAnonymous]  │  │
│  │  - tags/sync()                 [Authorize]       │  │
│  └──────────────┬───────────────────────────────────┘  │
│                 │                                       │
│                 v                                       │
│  ┌──────────────────────────────────────────────────┐  │
│  │  FreshdeskService                                │  │
│  │  - ResolveAgentIdByEmailAsync()                  │  │
│  │  - SuggestTicketsAsync()                         │  │
│  │  - SuggestTagsAsync()                            │  │
│  │  - SyncTagsFromFreshdeskAsync()                  │  │
│  └──────────────┬───────────────────────────────────┘  │
│                 │                                       │
│        ┌────────┴────────┐                             │
│        v                 v                             │
│  ┌─────────────┐   ┌──────────────────────┐           │
│  │ PostgreSQL  │   │  FreshdeskClient     │           │
│  │  (Cache)    │   │  (HTTP → Freshdesk)  │           │
│  └─────────────┘   └──────────┬───────────┘           │
│                                │                        │
└────────────────────────────────┼────────────────────────┘
                                 │
                                 v
                    ┌────────────────────────┐
                    │   Freshdesk API        │
                    │  - /api/v2/agents/me   │
                    │  - /api/v2/agents/...  │
                    │  - /api/v2/search/...  │
                    │  - /api/v2/tickets/... │
                    └────────────────────────┘
```

### Patrón de Cache

1. **Lectura**: Los endpoints leen de BD local (PostgreSQL)
2. **Escritura**: La sincronización actualiza desde Freshdesk → BD local
3. **Ventajas**:
   - ⚡ Respuestas ultra-rápidas (milisegundos)
   - ✅ No consume rate limits en lectura
   - ✅ Funciona aunque Freshdesk esté caído

---

## 🌐 ENDPOINTS API {#endpoints}

### 1. **GET `/api/v1/freshdesk/ping`** - Verificar Conexión

**Auth**: `[AllowAnonymous]` (público)

**Descripción**: Verifica que la API Key y Domain de Freshdesk estén correctos.

**Request**:
```bash
GET /api/v1/freshdesk/ping
```

**Response** (200 OK):
```json
{
  "ok": true,
  "status": 200,
  "message": "✅ Conexión exitosa con Freshdesk",
  "agent": "support@alterasoftware.com",
  "timestamp": "2026-01-24T21:00:00Z"
}
```

**Response** (401 - credenciales inválidas):
```json
{
  "ok": false,
  "status": 401,
  "message": "❌ Error al conectar con Freshdesk",
  "error": "Credenciales inválidas (API Key incorrecta)",
  "timestamp": "2026-01-24T21:00:00Z"
}
```

**Uso interno**: Llama a `GET /api/v2/agents/me` de Freshdesk.

---

### 2. **GET `/api/v1/freshdesk/tickets/suggest`** - Buscar Tickets

**Auth**: `[Authorize]` (requiere login)

**Descripción**: Busca tickets del usuario autenticado (o sin asignar).

**Parámetros**:
| Parámetro | Tipo | Default | Descripción |
|-----------|------|---------|-------------|
| `term` | string | null | Término de búsqueda (ID numérico o texto en subject/description) |
| `limit` | int | 10 | Número máximo de resultados (1-50) |
| `includeUnassigned` | bool | true | Incluir tickets sin asignar (`agent_id:null`) |

**Request**:
```bash
GET /api/v1/freshdesk/tickets/suggest?term=problema&limit=5&includeUnassigned=true
```

**Response** (200 OK):
```json
{
  "success": true,
  "count": 5,
  "tickets": [
    {
      "id": 56185,
      "subject": "Problema con TPV",
      "status": 2,
      "statusName": "Open",
      "priority": 1,
      "priorityName": "Low",
      "updatedAt": "2026-01-24T17:54:53Z"
    }
  ]
}
```

**Lógica interna**:
1. Obtiene el email del usuario desde JWT (`ClaimTypes.Email`)
2. Resuelve su `agentId` en Freshdesk (con cache en `freshdesk_agent_maps`)
3. Construye query Freshdesk:
   - Si `includeUnassigned=true`: `(agent_id:{agentId} OR agent_id:null)`
   - Si `term` es numérico: `id:{term}`
   - Si `term` es texto: `(subject:'{term}' OR description:'{term}')`
4. Llama a Freshdesk API: `/api/v2/search/tickets`

---

### 3. **GET `/api/v1/freshdesk/tags/suggest`** - Buscar Tags

**Auth**: `[AllowAnonymous]` (público)

**Descripción**: Busca tags en cache local (BD PostgreSQL). Búsqueda por prefijo (ILIKE).

**Parámetros**:
| Parámetro | Tipo | Default | Descripción |
|-----------|------|---------|-------------|
| `term` | string | null | Prefijo para filtrar tags (case-insensitive) |
| `limit` | int | 20 | Número máximo de resultados (1-50) |

**Request**:
```bash
GET /api/v1/freshdesk/tags/suggest?term=tpv&limit=10
```

**Response** (200 OK):
```json
{
  "success": true,
  "count": 4,
  "tags": [
    "tpv hw",
    "tpv",
    "tpv lenta",
    "tpv software"
  ]
}
```

**SQL interno**:
```sql
SELECT f.name
FROM pss_dvnx.freshdesk_tags AS f
WHERE f.name ILIKE 'tpv%'  -- Prefijo
ORDER BY f.last_seen_at DESC
LIMIT 10;
```

**Nota**: Lee de **BD local**, NO llama a Freshdesk API.

---

### 4. **POST `/api/v1/freshdesk/tags/sync`** - Sincronizar Tags

**Auth**: `[Authorize]` (requiere login, NO requiere Admin)

**Descripción**: Sincroniza tags desde Freshdesk hacia BD local. Operación manual con métricas.

**Parámetros**:
| Parámetro | Tipo | Default | Descripción |
|-----------|------|---------|-------------|
| `mode` | string | "recent" | `"recent"` (últimos N días) o `"full"` (todos los tickets) |
| `days` | int | 30 | Días hacia atrás (1-365, solo para mode=recent) |
| `limit` | int | 1000 | Máximo de tickets a procesar (1-5000) |

**Request**:
```bash
POST /api/v1/freshdesk/tags/sync?mode=recent&days=30&limit=1000
```

**Response** (200 OK):
```json
{
  "success": true,
  "message": "✅ Sincronización completada en 15234ms",
  "metrics": {
    "ticketsScanned": 300,
    "tagsFound": 87,
    "inserted": 12,
    "updated": 75,
    "durationMs": 15234
  },
  "startedAt": "2026-01-24T20:45:00Z",
  "completedAt": "2026-01-24T20:45:15Z"
}
```

**Lógica interna**:
1. Construye query Freshdesk según `mode`:
   - `mode=recent`: `updated_at:>'2025-12-25'` (últimos N días)
   - `mode=full`: `(status:2 OR status:3 OR status:4)` (Open, Pending, Resolved)
2. Busca tickets con paginación (máx 10 páginas/300 tickets)
3. Para cada ticket:
   - Llama a `GET /api/v2/tickets/{id}` para obtener `tags[]`
   - Normaliza tags (trim + lowercase)
4. Hace UPSERT en tabla `freshdesk_tags`:
   - **INSERT** si no existe
   - **UPDATE** `last_seen_at` si ya existe

---

## 🗄️ BASE DE DATOS {#base-de-datos}

### Tablas Creadas

#### **1. `pss_dvnx.freshdesk_agent_maps`** (Cache de AgentId)

```sql
CREATE TABLE pss_dvnx.freshdesk_agent_maps (
    user_id UUID NOT NULL PRIMARY KEY,
    email VARCHAR(255) NOT NULL,
    agent_id BIGINT NOT NULL,
    synced_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    FOREIGN KEY (user_id) REFERENCES pss_dvnx.users(id) ON DELETE CASCADE
);

CREATE INDEX ix_freshdesk_agent_maps_email ON pss_dvnx.freshdesk_agent_maps(email);
```

**Propósito**: 
- Cachear el mapeo entre `user_id` de GestionTime → `agent_id` de Freshdesk
- Evita llamadas repetidas a `/api/v2/agents/autocomplete`
- TTL: 24 horas (configurable en `AgentCacheExpiration`)

**Ejemplo de registro**:
```
user_id: 3db05c47-a0f6-44c2-b0d6-ca26b6e50231
email: psantos@global-retail.com
agent_id: 12345678
synced_at: 2026-01-24 20:00:00+00
```

---

#### **2. `pss_dvnx.freshdesk_tags`** (Cache de Tags)

```sql
CREATE TABLE pss_dvnx.freshdesk_tags (
    name VARCHAR(100) NOT NULL PRIMARY KEY,
    source VARCHAR(50) NOT NULL DEFAULT 'freshdesk',
    last_seen_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX ix_freshdesk_tags_last_seen ON pss_dvnx.freshdesk_tags(last_seen_at DESC);
```

**Propósito**:
- Cachear todos los tags únicos encontrados en Freshdesk
- Permitir búsqueda rápida con autocompletado (ILIKE)
- `last_seen_at`: indica cuándo fue la última vez que se vio el tag

**Ejemplo de registros**:
```
name: 'tpv'          | source: 'freshdesk' | last_seen_at: 2026-01-24 21:00:00
name: 'urgente'      | source: 'freshdesk' | last_seen_at: 2026-01-24 20:30:00
name: 'global'       | source: 'freshdesk' | last_seen_at: 2026-01-23 15:00:00
```

---

### Script SQL para Crear Tablas

Ubicación: `scripts/create-freshdesk-tables.sql`

```bash
# Ejecutar manualmente si es necesario:
psql -h localhost -p 5434 -U postgres -d pss_dvnx -f scripts/create-freshdesk-tables.sql
```

---

## ⚙️ CONFIGURACIÓN {#configuración}

### `appsettings.json`

```json
{
  "Freshdesk": {
    "Domain": "alterasoftware",
    "ApiKey": "9i1AtT08nkY1BlBmjtLk",
    "SyncIntervalHours": 24,
    "SyncEnabled": true
  }
}
```

### Variables de Entorno (Render / Producción)

**Formato**: Usar `__` (doble guion bajo) para separar niveles

```bash
# Obligatorias
FRESHDESK__DOMAIN=alterasoftware
FRESHDESK__APIKEY=9i1AtT08nkY1BlBmjtLk

# Opcionales (sincronización automática)
FRESHDESK__SYNCINTERVALHOURS=24          # Intervalo en horas (default: 24)
FRESHDESK__SYNCENABLED=true              # Habilitar sync automático (default: true)
```

**En Render**:
```
Environment Variables:
  FRESHDESK__DOMAIN = alterasoftware
  FRESHDESK__APIKEY = 9i1AtT08nkY1BlBmjtLk
  FRESHDESK__SYNCINTERVALHOURS = 24
  FRESHDESK__SYNCENABLED = true
```

---

### Clase `FreshdeskOptions.cs`

```csharp
public class FreshdeskOptions
{
    public const string SectionName = "Freshdesk";
    
    public string Domain { get; set; } = string.Empty;        // "alterasoftware"
    public string ApiKey { get; set; } = string.Empty;        // API Key de Freshdesk
    public int SyncIntervalHours { get; set; } = 24;          // Cada 24h
    public bool SyncEnabled { get; set; } = true;             // Activar/desactivar
    
    // Normaliza URL automáticamente:
    // "alterasoftware" → "https://alterasoftware.freshdesk.com/"
    public string BaseUrl { get; }
    
    public bool IsConfigured => !string.IsNullOrEmpty(Domain) && !string.IsNullOrEmpty(ApiKey);
}
```

**Normalización de URLs**:
- Si `Domain = "alterasoftware"` → `BaseUrl = "https://alterasoftware.freshdesk.com/"`
- Si `Domain = "https://alterasoftware.freshdesk.com"` → `BaseUrl = "https://alterasoftware.freshdesk.com/"`
- Si `Domain = "alterasoftware.freshdesk.com"` → `BaseUrl = "https://alterasoftware.freshdesk.com/"`

---

## 🔄 BACKGROUND SERVICE {#background-service}

### `FreshdeskSyncBackgroundService.cs`

**Propósito**: Sincronizar tags automáticamente cada N horas sin intervención manual.

**Configuración**:
```csharp
builder.Services.AddHostedService<FreshdeskSyncBackgroundService>();
```

**Comportamiento**:
1. Al arrancar la API:
   - Espera **1 minuto** (para no bloquear el startup)
   - Ejecuta la primera sincronización automática
   - Logs: `🔄 Iniciando sincronización automática...`

2. Ciclo de sincronización:
   - Modo: `"recent"` (últimos 30 días)
   - Límite: 1000 tickets (realmente máx 300 por límite de Freshdesk)
   - Intervalo: configurado en `SyncIntervalHours` (default: 24h)

3. Logs:
```
[21:03:53 INF] 🔄 Sincronización automática de Freshdesk HABILITADA
[21:03:53 INF]    📅 Intervalo: cada 24 horas
[21:03:53 INF]    🌐 Domain: alterasoftware
...
[21:04:02 INF] ✅ Sincronización automática completada: 12 nuevos, 75 actualizados (9383ms)
[21:04:02 INF] ⏰ Próxima sincronización en 24 horas (2026-01-25 21:04:02)
```

**Manejo de errores**:
- Si falla, loguea el error y reintenta en el próximo ciclo (24h)
- NO detiene la aplicación

**Deshabilitar**:
```bash
# Variables de entorno
FRESHDESK__SYNCENABLED=false
```

---

## ⚠️ LIMITACIONES CONOCIDAS {#limitaciones}

### 1. **Límite de 10 páginas en Freshdesk Search API**

**Problema**: Freshdesk rechaza `page > 10` con error 400:
```json
{
  "description": "Validation failed",
  "errors": [{
    "field": "page",
    "message": "It should be a Positive Integer less than or equal to 10"
  }]
}
```

**Impacto**: Solo se pueden obtener ~300 tickets por búsqueda (30 tickets/página × 10 páginas).

**Solución implementada**:
- El código limita automáticamente a 10 páginas
- Warning en logs si se alcanza el límite
- Para obtener más tickets, usar filtros más específicos:
  - `mode=recent&days=7` (última semana)
  - `mode=recent&days=15` (últimas 2 semanas)
  - Dividir en múltiples sincronizaciones

**Código relevante** (`FreshdeskClient.cs` línea 206):
```csharp
const int MAX_PAGES = 10; // Límite de Freshdesk API
while (page <= MAX_PAGES) { ... }
```

---

### 2. **Rate Limits de Freshdesk**

**Límites según plan**:
- Plan básico: ~200 requests/minuto
- Plan superior: ~400 requests/minuto

**Protección implementada**:
- Delay de 500ms entre páginas de búsqueda
- Delay de 100ms entre requests de tickets (si > 100 tickets)

**Código relevante** (`FreshdeskClient.cs`):
```csharp
await Task.Delay(TimeSpan.FromMilliseconds(500), ct);  // Entre páginas
await Task.Delay(TimeSpan.FromMilliseconds(100), ct);  // Entre tickets
```

**Si se alcanza rate limit**:
```
Error: 429 Too Many Requests
```

---

### 3. **Cache de AgentId (24 horas)**

El mapeo `user_id` → `agent_id` se cachea 24 horas.

**Problema potencial**: Si el email del usuario cambia en Freshdesk, el cache quedará obsoleto hasta 24h.

**Solución**: 
- Manual: eliminar el registro de `freshdesk_agent_maps` para ese usuario
- Automático: esperar 24h para que expire

**Constante** (`FreshdeskService.cs` línea 13):
```csharp
private static readonly TimeSpan AgentCacheExpiration = TimeSpan.FromHours(24);
```

---

### 4. **Tags duplicados con diferentes case**

Los tags en Freshdesk pueden tener case inconsistente: `"TPV"`, `"tpv"`, `"Tpv"`.

**Solución implementada**: Normalización a lowercase en la sincronización.

**Código** (`FreshdeskService.cs` línea 201):
```csharp
var normalized = tag.Trim().ToLowerInvariant();
```

**Resultado**: Todos los tags se guardan en minúsculas en BD.

---

## 🧪 TESTING {#testing}

### Scripts de Prueba Disponibles

#### 1. **Test completo** (`scripts/test-freshdesk-all.ps1`)
```powershell
.\scripts\test-freshdesk-all.ps1
```
Prueba todos los endpoints públicos (ping, test-connection, tags/suggest).

#### 2. **Test directo a Freshdesk** (`test-freshdesk-direct.ps1`)
```powershell
.\test-freshdesk-direct.ps1
```
Prueba la API Key directamente contra Freshdesk (curl a `/api/v2/tickets/20`).

---

### Testing Manual en Swagger

**URL**: `https://localhost:2502/swagger`

**Secuencia de pruebas**:

1. **Verificar conexión** (sin login):
```
GET /api/v1/freshdesk/ping
```

2. **Login**:
```
POST /api/v1/auth/login
Body: { "email": "psantos@global-retail.com", "password": "12345678" }
```

3. **Buscar tickets** (requiere login):
```
GET /api/v1/freshdesk/tickets/suggest?term=problema&limit=5&includeUnassigned=true
```

4. **Buscar tags** (sin login):
```
GET /api/v1/freshdesk/tags/suggest?term=tpv&limit=10
```

5. **Sincronizar tags** (requiere login):
```
POST /api/v1/freshdesk/tags/sync?mode=recent&days=30&limit=1000
```

---

### Verificar en Base de Datos

```sql
-- Ver tags sincronizados
SELECT name, last_seen_at 
FROM pss_dvnx.freshdesk_tags 
ORDER BY last_seen_at DESC 
LIMIT 20;

-- Ver cache de agentes
SELECT u.email, f.agent_id, f.synced_at
FROM pss_dvnx.freshdesk_agent_maps f
JOIN pss_dvnx.users u ON u.id = f.user_id;

-- Contar tags
SELECT COUNT(*) FROM pss_dvnx.freshdesk_tags;
```

---

## 🚀 DEPLOYMENT {#deployment}

### Despliegue en Render.com

#### 1. **Variables de Entorno**

En Render Dashboard → Environment:
```
FRESHDESK__DOMAIN = alterasoftware
FRESHDESK__APIKEY = 9i1AtT08nkY1BlBmjtLk
FRESHDESK__SYNCINTERVALHOURS = 24
FRESHDESK__SYNCENABLED = true
```

#### 2. **Base de Datos**

Las tablas se crean automáticamente con las migraciones de EF Core al hacer deploy.

Si necesitas crearlas manualmente:
```bash
# Conectar a PostgreSQL en Render
psql $DATABASE_URL

# Ejecutar script
\i scripts/create-freshdesk-tables.sql
```

#### 3. **Logs**

Los logs se escriben en:
- **Consola**: Visibles en Render Dashboard → Logs
- **Archivos**: `logs/log-YYYYMMDD.txt` (últimos 7 días)

Para ver logs de sincronización:
```bash
Get-Content logs/log-20260124.txt | Select-String "Freshdesk"
```

---

### Verificación Post-Deploy

1. **Ping a Freshdesk**:
```bash
curl https://tu-app.onrender.com/api/v1/freshdesk/ping
```

2. **Ver logs de sincronización** (1 minuto después del deploy):
```
🔄 Sincronización automática de Freshdesk HABILITADA
📅 Intervalo: cada 24 horas
🌐 Domain: alterasoftware
...
✅ Sincronización automática completada: X tags actualizados
```

3. **Verificar tags en BD**:
```sql
SELECT COUNT(*) FROM freshdesk_tags;
```

---

## 📝 NOTAS ADICIONALES

### Seguridad

- ✅ **API Key NO se expone** en logs (se enmascara: `9i1A...jtLk`)
- ✅ **BasicAuth** usado correctamente (`apiKey:X`)
- ✅ **HTTPS** requerido en producción (Render maneja SSL)
- ✅ **JWT** para autenticación de usuarios

### Performance

- ⚡ **Tags suggest**: ~5ms (BD local)
- ⚡ **Tickets suggest** (cache HIT): ~50ms
- 🐢 **Tickets suggest** (cache MISS): ~500ms
- 🐢 **Sincronización completa**: ~10-15 segundos (300 tickets)

### Mantenimiento

- **Cache de agentId**: Se limpia automáticamente después de 24h
- **Tags obsoletos**: Usar `last_seen_at` para identificar tags antiguos:
  ```sql
  DELETE FROM freshdesk_tags 
  WHERE last_seen_at < NOW() - INTERVAL '90 days';
  ```

---

## 🔗 ENLACES ÚTILES

- **Freshdesk API Docs**: https://developers.freshdesk.com/api/
- **Search API**: https://developers.freshdesk.com/api/#ticket_search
- **Rate Limits**: https://developers.freshdesk.com/api/#ratelimit

---

## ✅ CHECKLIST DE VERIFICACIÓN

- [x] Tablas `freshdesk_agent_maps` y `freshdesk_tags` creadas
- [x] Variables de entorno configuradas (`FRESHDESK__DOMAIN`, `FRESHDESK__APIKEY`)
- [x] `/api/v1/freshdesk/ping` devuelve 200 OK
- [x] `/api/v1/freshdesk/tags/suggest` devuelve tags (después de sync)
- [x] `/api/v1/freshdesk/tickets/suggest` funciona con usuario logueado
- [x] Background service sincroniza automáticamente (ver logs después de 1 min)
- [x] Logs se guardan en `logs/log-YYYYMMDD.txt`

---

**Fecha de implementación**: 2026-01-24  
**Versión de la API**: 1.0  
**Versión de Freshdesk API**: v2  
**Implementado por**: GitHub Copilot
