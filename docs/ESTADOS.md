# Sistema de Estados - Partes de Trabajo

## ?? Definición de Estados

Los estados de los partes de trabajo se representan con valores numéricos en la columna `state` para mayor consistencia y facilitar futuras integraciones.

| Valor | Constante | Nombre | Descripción |
|-------|-----------|--------|-------------|
| `0` | `EstadoParte.Abierto` | Abierto / Open | Parte en progreso, editable |
| `1` | `EstadoParte.Pausado` | Pausado / Paused | Temporalmente detenido, editable |
| `2` | `EstadoParte.Cerrado` | Cerrado / Closed | Completado, no editable |
| `3` | `EstadoParte.Enviado` | Enviado / Sent | Cerrado y enviado al destino |
| `9` | `EstadoParte.Anulado` | Anulado / Cancelled | Cancelado/eliminado lógicamente |

### Columnas en Base de Datos

| Columna | Tipo | Descripción |
|---------|------|-------------|
| `state` | INTEGER | **Nueva** - Valor numérico del estado |
| `estado` | VARCHAR | **Legacy** - Se mantiene para compatibilidad |

---

## ?? Diagrama de Transiciones

```
                    ???????????????
                    ?    OPEN     ???????????????
                    ?     (0)     ?             ?
                    ???????????????             ?
                           ?                    ?
              ???????????????????????????       ?
              ?            ?            ?       ?
              ?            ?            ?       ?
       ???????????? ???????????? ????????????  ?
       ?  PAUSED  ? ?  CLOSED  ? ?CANCELLED ?  ?
       ?   (1)    ? ?   (2)    ? ?   (9)    ?  ?
       ???????????? ???????????? ????????????  ?
            ?            ?                      ?
            ?            ?                      ?
            ?     ????????????                  ?
            ?     ?   SENT   ?                  ?
            ?     ?   (3)    ?                  ?
            ?     ????????????                  ?
            ?                                   ?
            ?????????????????????????????????????
                       (resume)
```

---

## ?? Reglas de Negocio

### Edición
- Solo los partes en estado **Open (0)** o **Paused (1)** pueden ser editados.
- Los partes cerrados, enviados o anulados NO pueden modificarse.

### Transiciones permitidas

| Desde | Hacia | Endpoint |
|-------|-------|----------|
| Open | Paused | `POST /pause` |
| Open | Closed | `POST /close` |
| Open | Cancelled | `POST /cancel` |
| Paused | Open | `POST /resume` |
| Paused | Closed | `POST /close` |
| Paused | Cancelled | `POST /cancel` |
| Closed | Sent | (futuro: integración externa) |

### Transiciones NO permitidas

| Desde | Hacia | Razón |
|-------|-------|-------|
| Closed | Open | Ya completado |
| Sent | * | Ya procesado externamente |
| Cancelled | * | Eliminación lógica permanente |

---

## ?? API Endpoints

### Consultar estados disponibles
```http
GET /api/v1/partes/states
```

**Respuesta:**
```json
[
  { "id": 0, "name": "Abierto" },
  { "id": 1, "name": "Pausado" },
  { "id": 2, "name": "Cerrado" },
  { "id": 3, "name": "Enviado" },
  { "id": 9, "name": "Anulado" }
]
```

### Cambiar estado específico

| Endpoint | Descripción |
|----------|-------------|
| `POST /api/v1/partes/{id}/pause` | Pausa el parte |
| `POST /api/v1/partes/{id}/resume` | Reanuda un parte pausado |
| `POST /api/v1/partes/{id}/close` | Cierra el parte |
| `POST /api/v1/partes/{id}/cancel` | Anula el parte |

### Cambiar estado genérico
```http
POST /api/v1/partes/{id}/state
Content-Type: application/json

{ "state": 2 }
```

**Respuesta:**
```json
{
  "message": "ok",
  "state": 2,
  "state_name": "Cerrado"
}
```

### Filtrar por estado
```http
GET /api/v1/partes?state=0
```

---

## ?? Uso en Código

### Clase EstadoParte

```csharp
using GestionTime.Domain.Work;

// Constantes
int state = EstadoParte.Abierto;      // 0
int state = EstadoParte.Pausado;      // 1
int state = EstadoParte.Cerrado;      // 2
int state = EstadoParte.Enviado;      // 3
int state = EstadoParte.Anulado;      // 9

// Validación
bool esValido = EstadoParte.EsValido(state);

// Permisos
bool puedeEditar = EstadoParte.PuedeEditar(state);
bool puedeCerrar = EstadoParte.PuedeCerrar(state);
bool puedeEnviar = EstadoParte.PuedeEnviar(state);
bool puedeAnular = EstadoParte.PuedeAnular(state);

// Nombre para mostrar
string nombre = EstadoParte.ObtenerNombre(state); // "Abierto"
```

### Entidad ParteDeTrabajo

```csharp
var parte = new ParteDeTrabajo();

// Propiedad de estado
parte.State = EstadoParte.Abierto;

// Propiedades de conveniencia (solo lectura)
bool abierto = parte.EstaAbierto;
bool pausado = parte.EstaPausado;
bool cerrado = parte.EstaCerrado;
bool enviado = parte.EstaEnviado;
bool anulado = parte.EstaAnulado;
bool editable = parte.PuedeEditar;
string nombre = parte.StateNombre;
```

---

## ?? Migración de Base de Datos

Ejecuta el script de migración para añadir la columna `state`:

```
GestionTime.Infrastructure/Migrations/SQL/20250117_CambiarEstadoAInteger.sql
```

### Estrategia de Migración

1. **Fase 1**: Añadir columna `state` (INTEGER) - Sin eliminar `estado`
2. **Fase 2**: Migrar valores de `estado` a `state`
3. **Fase 3** (futuro): Eliminar columna `estado` cuando todo esté migrado

### Mapeo de valores

| estado (legacy) | state (nuevo) |
|-----------------|---------------|
| `"activo"` | `0` |
| `"abierto"` | `0` |
| `"pausado"` | `1` |
| `"cerrado"` | `2` |
| `"enviado"` | `3` |
| `"anulado"` | `9` |

---

## ?? Futuras Extensiones

El sistema de estados está diseñado para soportar:

1. **Integración con sistemas externos**: Estado `Sent (3)` para tracking de sincronización
2. **Auditoría de cambios**: Historial de transiciones de estado
3. **Workflows personalizados**: Nuevos estados intermedios si se requieren
4. **Notificaciones**: Triggers basados en cambios de estado
