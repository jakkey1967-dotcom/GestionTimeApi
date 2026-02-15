# 🔧 Fix: Filtro de agentId Mal Aplicado en Backend

**Fecha:** 2026-02-14  
**Versión:** v1.9.5-alpha  
**Estado:** ✅ Corregido  
**Archivo:** `Services/InformesService.cs`

---

## 🐛 Problema Identificado

Cuando un usuario EDITOR/ADMIN **NO** especificaba el parámetro `agentId` en las llamadas a `/api/v2/informes/resumen` o `/api/v2/informes/partes`, el backend devolvía **datos de TODOS los usuarios** en lugar de usar el `currentUserId` del JWT por defecto.

### Código Incorrecto (ANTES)

```csharp
// ❌ PROBLEMA: Si agentIds está vacío, NO aplica filtro
// Líneas ~158-162 (GetResumenAsync) y ~73-77 (GetPartesAsync)

// Filtro por agente (con control de rol)
var agentIds = ResolveAgentIds(query.AgentId, query.AgentIds, currentUserId, userRole);
if (agentIds.Any())  // ← Si está vacío (EDITOR/ADMIN sin agentId), NO filtra
{
    baseQuery = baseQuery.Where(p => agentIds.Contains(p.IdUsuario));
}
```

### ¿Cuál era el problema?

**Comportamiento del método `ResolveAgentIds`:**

| Rol | agentId enviado | agentIds resultado | Comentario |
|-----|-----------------|-------------------|------------|
| USER | (ignorado) | `[currentUserId]` | ✅ Siempre usa su propio ID |
| EDITOR/ADMIN | `null` | `[]` (vacío) | ❌ **Lista vacía** |
| EDITOR/ADMIN | `GUID específico` | `[GUID]` | ✅ Usa el ID enviado |

**Problema:** Cuando `agentIds` está vacío y se hace `if (agentIds.Any())`, la condición es `false` y **NO se aplica el WHERE**, devolviendo **TODOS los partes de TODOS los usuarios**.

**Comentario engañoso en el código:**
```csharp
// Si no se especifica ninguno, agentIds queda vacío (= todos)
```

Este comentario sugería que era intencional devolver "todos", pero causaba:
- **Duplicados:** Si el frontend hacía múltiples llamadas (una por usuario), recibía datos duplicados.
- **Pérdida de seguridad:** Un EDITOR podría ver datos de TODOS sin especificarlo explícitamente.
- **Inconsistencia:** USER siempre filtra por su ID, pero EDITOR/ADMIN no.

---

## ✅ Solución Implementada

### Código Corregido (AHORA)

```csharp
// ✅ CORREGIDO: Si es EDITOR/ADMIN y no especifica agentId, usar currentUserId por defecto

// Filtro por agente (con control de rol)
var agentIds = ResolveAgentIds(query.AgentId, query.AgentIds, currentUserId, userRole);

// Si es EDITOR/ADMIN y no especificó agentId, usar currentUserId por defecto
if (!agentIds.Any() && (userRole == "EDITOR" || userRole == "ADMIN"))
{
    agentIds.Add(currentUserId);
}

// SIEMPRE aplicar filtro de agente (ahora nunca estará vacío)
baseQuery = baseQuery.Where(p => agentIds.Contains(p.IdUsuario));
```

### Cambios Realizados

1. **Validación adicional:** Si `agentIds` está vacío y el rol es EDITOR/ADMIN, añadir `currentUserId`.
2. **Filtro siempre aplicado:** Eliminar el `if (agentIds.Any())` y aplicar el WHERE siempre.
3. **Comentario actualizado:** Clarificar que ahora se usa `currentUserId` por defecto.

---

## 📊 Comparación de Comportamiento

### Escenario 1: USER busca sus datos

#### ANTES ✅
- `ResolveAgentIds` devuelve `[currentUserId]`
- `if (agentIds.Any())` → `true`
- Aplica filtro: `WHERE id_usuario = currentUserId`
- **Resultado:** Solo sus datos ✅

#### AHORA ✅
- `ResolveAgentIds` devuelve `[currentUserId]`
- No entra al `if (!agentIds.Any())` (porque tiene 1 elemento)
- Aplica filtro: `WHERE id_usuario = currentUserId`
- **Resultado:** Solo sus datos ✅ (sin cambios)

---

### Escenario 2: EDITOR busca SIN especificar agentId

#### ANTES ❌
- `ResolveAgentIds` devuelve `[]` (vacío)
- `if (agentIds.Any())` → `false`
- **NO aplica filtro**
- **Resultado:** Devuelve datos de **TODOS los usuarios** ❌

#### AHORA ✅
- `ResolveAgentIds` devuelve `[]` (vacío)
- Entra al `if (!agentIds.Any() && userRole == "EDITOR")` → `true`
- Añade `currentUserId` a la lista: `[currentUserId]`
- Aplica filtro: `WHERE id_usuario = currentUserId`
- **Resultado:** Solo sus propios datos ✅

---

### Escenario 3: EDITOR busca CON agentId específico

#### ANTES ✅
- `ResolveAgentIds` devuelve `[agentId específico]`
- `if (agentIds.Any())` → `true`
- Aplica filtro: `WHERE id_usuario = agentId`
- **Resultado:** Solo datos del agente especificado ✅

#### AHORA ✅
- `ResolveAgentIds` devuelve `[agentId específico]`
- No entra al `if (!agentIds.Any())` (porque tiene 1 elemento)
- Aplica filtro: `WHERE id_usuario = agentId`
- **Resultado:** Solo datos del agente especificado ✅ (sin cambios)

---

### Escenario 4: ADMIN busca CON agentIds múltiples (separados por comas)

#### ANTES ✅
- `ResolveAgentIds` devuelve `[id1, id2, id3]`
- `if (agentIds.Any())` → `true`
- Aplica filtro: `WHERE id_usuario IN (id1, id2, id3)`
- **Resultado:** Datos de los agentes especificados ✅

#### AHORA ✅
- `ResolveAgentIds` devuelve `[id1, id2, id3]`
- No entra al `if (!agentIds.Any())` (porque tiene 3 elementos)
- Aplica filtro: `WHERE id_usuario IN (id1, id2, id3)`
- **Resultado:** Datos de los agentes especificados ✅ (sin cambios)

---

## 🎯 Beneficios de la Corrección

### ✅ Beneficio 1: Seguridad mejorada
**Antes:** EDITOR/ADMIN sin `agentId` podía ver **todos** los usuarios sin restricción.  
**Ahora:** Por defecto ve solo sus propios datos (comportamiento consistente con USER).

### ✅ Beneficio 2: Elimina duplicados
**Antes:** Si el frontend hacía llamadas múltiples (una por usuario), recibía datos duplicados porque cada llamada devolvía "todos".  
**Ahora:** Cada llamada devuelve solo los datos del agente solicitado (o del usuario actual si no se especifica).

### ✅ Beneficio 3: Comportamiento consistente
**Antes:** USER siempre filtraba, EDITOR/ADMIN no.  
**Ahora:** **Todos** los roles filtran por defecto usando `currentUserId`.

### ✅ Beneficio 4: Compatibilidad con frontend
El frontend (GestionTimeDesktop) envía `agentId` cuando busca datos de un agente específico:
- **USER:** Frontend envía `CurrentUserId` (backend lo ignora y usa el del JWT).
- **EDITOR/ADMIN:** Frontend envía `SelectedAgentId` o `null` (backend ahora usa `currentUserId` si es `null`).

---

## 📝 Logs Esperados (AHORA)

### Caso 1: USER busca sus datos

```
[InformesService] Informes/resumen: user=<GUID>, role=USER, scope=week, filters={ agentId=null, ... }, parts=12, duration=45ms
```

**Comportamiento:** Usa `currentUserId` del JWT (ignora `agentId` enviado).

---

### Caso 2: EDITOR busca SIN agentId

```
[InformesService] Informes/resumen: user=<GUID>, role=EDITOR, scope=day, filters={ agentId=null, ... }, parts=5, duration=32ms
```

**Comportamiento:** Como `agentId=null`, usa `currentUserId` del JWT por defecto.

---

### Caso 3: EDITOR busca CON agentId específico

```
[InformesService] Informes/resumen: user=<GUID>, role=EDITOR, scope=week, filters={ agentId=<otro-GUID>, ... }, parts=18, duration=58ms
```

**Comportamiento:** Usa el `agentId` especificado.

---

### Caso 4: ADMIN busca CON agentIds múltiples

```
[InformesService] Informes/partes: user=<GUID>, role=ADMIN, filters={ agentIds="<id1>,<id2>,<id3>", ... }, total=124, duration=102ms
```

**Comportamiento:** Usa los IDs especificados en `agentIds` (separados por comas).

---

## 🔄 Endpoints Afectados

### 1. `GET /api/v2/informes/resumen`
- **Método:** `GetResumenAsync` en `InformesService.cs`
- **Líneas modificadas:** ~158-168
- **Impacto:** Ahora usa `currentUserId` por defecto si EDITOR/ADMIN no especifica `agentId`.

### 2. `GET /api/v2/informes/partes`
- **Método:** `GetPartesAsync` en `InformesService.cs`
- **Líneas modificadas:** ~73-83
- **Impacto:** Ahora usa `currentUserId` por defecto si EDITOR/ADMIN no especifica `agentId`.

---

## ✅ Testing Recomendado

### Test 1: USER sin agentId
```powershell
# Login como USER (psantos@global-retail.com)
$loginResponse = Invoke-RestMethod -Uri "https://gestiontimeapi.onrender.com/api/v1/auth/login-desktop" -Method POST -Body (@{email="psantos@global-retail.com"; password="12345678"} | ConvertTo-Json) -ContentType "application/json"
$token = $loginResponse.accessToken

# Llamar sin agentId
$response = Invoke-RestMethod -Uri "https://gestiontimeapi.onrender.com/api/v2/informes/resumen?scope=day&date=2026-02-14" -Headers @{Authorization="Bearer $token"}
$response.partsCount  # Debería devolver solo partes de psantos
```

### Test 2: EDITOR sin agentId (ANTES devolvía todos, AHORA solo propios)
```powershell
# Login como EDITOR
$loginResponse = Invoke-RestMethod -Uri "https://gestiontimeapi.onrender.com/api/v1/auth/login-desktop" -Method POST -Body (@{email="editor@empresa.com"; password="password"} | ConvertTo-Json) -ContentType "application/json"
$token = $loginResponse.accessToken

# Llamar sin agentId
$response = Invoke-RestMethod -Uri "https://gestiontimeapi.onrender.com/api/v2/informes/resumen?scope=week&weekIso=2026-W07" -Headers @{Authorization="Bearer $token"}
$response.partsCount  # ANTES: todos los usuarios | AHORA: solo del editor
```

### Test 3: EDITOR con agentId específico
```powershell
# Llamar con agentId de otro usuario
$agentId = "b1c2d3e4-f5a6-7890-abcd-ef1234567890"  # GUID de otro agente
$response = Invoke-RestMethod -Uri "https://gestiontimeapi.onrender.com/api/v2/informes/resumen?scope=day&date=2026-02-14&agentId=$agentId" -Headers @{Authorization="Bearer $token"}
$response.partsCount  # Debería devolver solo partes del agente especificado
```

---

## 🚀 Deployment

### 1. Compilar y reiniciar backend
```bash
cd GestionTimeApi
dotnet build
dotnet run  # O reiniciar en Render.com
```

### 2. Verificar logs en Render
```
https://dashboard.render.com/web/srv-xxx/logs
```

Buscar líneas:
```
[InformesService] Informes/resumen: user=..., role=EDITOR, filters={ agentId=null, ... }
```

### 3. Testing desde frontend (GestionTimeDesktop)
- **F5** para ejecutar en Debug
- Ir a **Informes**
- Como EDITOR: Buscar sin seleccionar agente
- Verificar en **Output window** logs `[InformesService]`
- Debería ver: `agentId: <tu-GUID>` (no `null`)

---

## 📚 Archivos Modificados

### Backend (GestionTimeApi)
- ✅ `Services/InformesService.cs` (2 ubicaciones: GetResumenAsync y GetPartesAsync)
- ✅ `scripts/Fix-AgentIdFilter.ps1` (script de aplicación del fix)
- ✅ `docs/FIX_FILTRO_AGENTID_BACKEND.md` (esta documentación)

### Frontend (GestionTimeDesktop)
- ✅ Ya estaba correcto (envía `SelectedAgentId` cuando es EDITOR/ADMIN)
- ✅ `Docs/FIX_FILTRO_BYDAY_MAL_APLICADO.md` (documentación del fix del frontend)

---

## ✅ Conclusión

### Problema Original
- Filtro de `agentIds` en backend permitía que EDITOR/ADMIN vieran **todos** los usuarios cuando no especificaban `agentId`.
- Causaba duplicados en el frontend cuando hacía múltiples llamadas.
- Comportamiento inconsistente: USER siempre filtraba, EDITOR/ADMIN no.

### Solución Implementada
- ✅ Si EDITOR/ADMIN no especifica `agentId`, usar `currentUserId` del JWT por defecto.
- ✅ Aplicar filtro **siempre** (eliminar condicional `if (agentIds.Any())`).
- ✅ Comentarios actualizados para clarificar comportamiento.
- ✅ Cambio aplicado en **2 métodos:** `GetResumenAsync` y `GetPartesAsync`.

### Testing
- ⏳ Pendiente: Testing en runtime con backend reiniciado.
- ⏳ Pendiente: Verificar logs en Render.com.
- ⏳ Pendiente: Testing desde frontend (GestionTimeDesktop).

---

**Versión:** v1.9.5-alpha  
**Fecha:** 2026-02-14  
**Estado:** ✅ Fix aplicado en código, pendiente reinicio del backend  
**Script:** `scripts/Fix-AgentIdFilter.ps1`

**FIN DEL DOCUMENTO**
