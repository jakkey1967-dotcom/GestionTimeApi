# 🏷️ Cómo se Actualizan los Tags en GestionTime

## 📋 Resumen Ejecutivo

Los tags en GestionTime provienen de **dos fuentes independientes:**

1. **Tags locales** de Partes de Trabajo (tabla `pss_dvnx.partes_de_trabajo`, columna `tags`)
2. **Tags de Freshdesk** (tabla `pss_dvnx.freshdesk_tags` - **DEPRECADA, ya no se usa**)

---

## 🔄 Sistema Actual de Tags

### ✅ Fuente Principal: Partes de Trabajo Locales

Los tags se obtienen **directamente de los partes de trabajo** existentes, sin necesidad de sincronización con Freshdesk.

**Endpoint:**
```
GET /api/v1/freshdesk/tags/suggest?term=&limit=20
```

**Lógica actual:**
```csharp
// Extrae tags de la columna 'tags' (text[]) de partes_de_trabajo
var allTags = await _db.PartesDeTrabajo
    .Where(p => p.Tags != null && p.Tags.Length > 0)
    .SelectMany(p => p.Tags!)
    .ToListAsync();

// Los ordena por frecuencia de uso
var tagFrequency = allTags
    .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
    .Select(g => new { Tag = g.Key, Count = g.Count() })
    .OrderByDescending(t => t.Count)
    .ToList();
```

**Ventajas:**
- ⚡ **Ultra rápido:** No hace llamadas a API externa
- 🎯 **Relevante:** Muestra tags que realmente se usan en tu empresa
- 📊 **Frecuencia:** Ordena por más usados primero
- 🔒 **Sin rate limiting:** Consultas ilimitadas

---

## ❌ Sistema Legacy: Freshdesk Tags (YA NO SE USA)

### Tabla `pss_dvnx.freshdesk_tags`

Esta tabla existe pero **ya no se utiliza** para el endpoint de sugerencias.

**Estado:** DEPRECADA ⚠️

**¿Por qué se deprecó?**
1. **Lento:** Requiere sincronización con Freshdesk API
2. **Innecesario:** Los tags locales son suficientes
3. **Rate limiting:** Limitado a 50 req/min por Freshdesk

---

## 🔧 Sincronización Automática (Background Service)

### ⚠️ IMPORTANTE: Solo sincroniza **Ticket Headers**, NO tags

El `FreshdeskSyncBackgroundService` sincroniza:
- ✅ Tickets (headers) → tabla `freshdesk_ticket_headers_cache`
- ✅ Companies → tabla `freshdesk_companies_cache`
- ❌ **NO sincroniza tags** (ya no es necesario)

**Configuración:**
```json
"Freshdesk": {
  "SyncEnabled": true,
  "SyncIntervalHours": 24
}
```

**Flujo:**
```
1. Cada 24 horas (configurable)
2. Llama a Freshdesk API
3. Sincroniza tickets actualizados en los últimos 30 días
4. Actualiza cache local para búsquedas rápidas
```

---

## 📊 Cómo Funcionan los Tags en Desktop

### 1. Usuario Crea/Edita Parte
```
Desktop → POST /api/v1/partes-de-trabajo
Body: {
  "ticket": 12345,
  "accion": "...",
  "tags": ["urgente", "instalacion", "hardware"]
}
```

### 2. Tags se Guardan en BD
```sql
INSERT INTO pss_dvnx.partes_de_trabajo (
  ...,
  tags  -- Columna text[] (array de texto)
) VALUES (
  ...,
  ARRAY['urgente', 'instalacion', 'hardware']
);
```

### 3. Desktop Solicita Sugerencias
```
Desktop → GET /api/v1/freshdesk/tags/suggest?term=inst&limit=10

Response: [
  "instalacion",    // Frecuencia: 145
  "instancia",      // Frecuencia: 23
  "instrucciones"   // Frecuencia: 8
]
```

**Lógica:**
- Busca en **todos los partes** que contengan tags
- Filtra por término (si se proporciona)
- Ordena por **frecuencia de uso**
- Devuelve los más usados primero

---

## 🔄 ¿Cómo se Actualizan los Tags Entonces?

### ✅ Actualización Automática (Tiempo Real)

Los tags se actualizan **automáticamente** cada vez que:

1. ✅ Se crea un nuevo parte con tags
2. ✅ Se edita un parte y se agregan/modifican tags
3. ✅ Se elimina un parte (tags desaparecen si nadie más los usa)

**NO hay sincronización manual necesaria.** Los tags están siempre actualizados porque se leen directamente de la tabla `partes_de_trabajo`.

### Ejemplo de Flujo:

```
09:00 - Usuario crea parte con tag "urgente" → DB actualizada
09:01 - Desktop pide sugerencias → Ve "urgente" disponible ✅
10:00 - Otro usuario crea parte con "urgente" → Frecuencia aumenta
10:01 - Desktop pide sugerencias → "urgente" aparece primero (más usado)
```

---

## 🛠️ Endpoints Relacionados

### 1. Sugerir Tags (Desktop)
```http
GET /api/v1/freshdesk/tags/suggest?term=inst&limit=10
Authorization: Bearer {token}
```

**Response:**
```json
{
  "success": true,
  "count": 3,
  "tags": ["instalacion", "instancia", "instrucciones"]
}
```

### 2. Sincronizar Tags desde Freshdesk (LEGACY - No recomendado)
```http
POST /api/v1/freshdesk/tags/sync?mode=recent&days=30
Authorization: Bearer {token}
Roles: Admin
```

⚠️ **NOTA:** Este endpoint sincroniza a la tabla `freshdesk_tags` que ya **no se usa** para sugerencias.

---

## 📈 Ventajas del Sistema Actual

### 1. Performance
- ⚡ **~10ms** vs ~500ms de Freshdesk API
- 🚀 Consulta directa a PostgreSQL con índices

### 2. Relevancia
- 🎯 Solo tags que **realmente se usan** en tu empresa
- 📊 Ordenados por frecuencia de uso

### 3. Sin Límites
- 🔓 No hay rate limiting
- 🌐 Funciona sin internet (si Freshdesk está caído)

### 4. Sincronización Automática
- ⏱️ Actualización en tiempo real (sin sincronización manual)
- ✅ Siempre reflejan el estado actual

---

## 🔍 Verificar Tags Actuales

### SQL Query:
```sql
-- Ver todos los tags únicos con frecuencia
SELECT 
  UNNEST(tags) as tag,
  COUNT(*) as frequency
FROM pss_dvnx.partes_de_trabajo
WHERE tags IS NOT NULL
GROUP BY tag
ORDER BY frequency DESC
LIMIT 20;
```

### Script PowerShell:
```powershell
# Test del endpoint de sugerencias
.\scripts\test-tags-suggest.ps1
```

---

## ⚙️ Configuración Recomendada

### appsettings.json
```json
{
  "Freshdesk": {
    "Domain": "${FRESHDESK_DOMAIN}",
    "ApiKey": "${FRESHDESK_API_KEY}",
    "SyncEnabled": true,           // Para tickets/companies
    "SyncIntervalHours": 24
  }
}
```

**NOTA:** `SyncEnabled` controla la sincronización de **tickets y companies**, NO de tags (que ya no se sincronizan desde Freshdesk).

---

## 🆕 Migración de Tags Legacy

Si tienes tags antiguos en `freshdesk_tags` que quieres migrar a partes:

```sql
-- Script de migración (ejecutar con precaución)
-- Este script NO existe aún, pero podría crearse si es necesario
```

**Recomendación:** Los tags se irán poblando naturalmente a medida que se creen nuevos partes. No es necesario migrar tags antiguos.

---

## 📚 Documentación Relacionada

- `docs/PARTE_TAGS_IMPLEMENTATION.md` - Implementación de tags en partes
- `docs/FRESHDESK_INTEGRATION.md` - Integración general con Freshdesk
- `scripts/test-parte-con-tags.ps1` - Test de creación de partes con tags

---

## ❓ FAQ

### ¿Los tags se sincronizan con Freshdesk?
**NO.** Los tags se leen de la tabla local `partes_de_trabajo`, no de Freshdesk.

### ¿Cada cuánto se actualizan los tags?
**Tiempo real.** Se actualizan cada vez que se crea/edita un parte.

### ¿Puedo sincronizar tags desde Freshdesk?
**Sí, pero no es necesario.** El endpoint `POST /api/v1/freshdesk/tags/sync` existe pero ya no se usa para sugerencias.

### ¿Qué hace el Background Service?
Sincroniza **tickets y companies** cada 24 horas, **NO tags**.

### ¿Cómo agrego un nuevo tag?
Simplemente úsalo al crear un parte. Si no existe, se agregará automáticamente.

### ¿Los tags son case-sensitive?
**NO.** Se agrupan case-insensitive: "Urgente" = "urgente" = "URGENTE"

---

## 🎯 Conclusión

**Los tags en GestionTime son ultra simples:**
1. Usuario crea parte con tags → Se guardan en BD
2. Desktop pide sugerencias → Se leen de BD (ordenados por frecuencia)
3. **NO hay sincronización manual necesaria**

**Sistema anterior (Freshdesk tags):**
- ❌ Deprecado
- ❌ Ya no se usa para sugerencias
- ✅ Reemplazado por sistema local más rápido y relevante

---

**¿Necesitas más información?**
- Consulta `Services/FreshdeskService.cs` (método `SuggestTagsAsync`)
- Revisa `docs/PARTE_TAGS_IMPLEMENTATION.md`
- Ejecuta `.\scripts\test-tags-suggest.ps1`
