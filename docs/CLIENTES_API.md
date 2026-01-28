# API de Clientes - CRUD Completo

## Resumen

Nueva API REST para gestión completa de clientes en la ruta `/api/v1/clientes`.

**IMPORTANTE**: Este nuevo endpoint NO reemplaza ni modifica el endpoint existente `/api/v1/catalog/clientes`, que sigue funcionando exactamente igual para mantener compatibilidad con código existente.

## Tabla de Base de Datos

- **Schema**: `pss_dvnx`
- **Tabla**: `cliente`
- **Columnas**:
  - `id` (int, identity/serial) - PK, autogenerado
  - `nombre` (varchar) - Nombre del cliente
  - `id_puntoop` (int, nullable)
  - `local_num` (int, nullable)
  - `nombre_comercial` (varchar, nullable)
  - `provincia` (varchar, nullable)
  - `data_update` (timestamp) - Fecha de actualización
  - `data_html` (text, nullable)
  - `nota` (text, nullable) - Campo para notas adicionales

## Endpoints

### 1. GET /api/v1/clientes - Listar clientes con filtros y paginación

**Descripción**: Obtiene una lista paginada de clientes con múltiples opciones de filtrado.

**Query Parameters**:
- `q` (string, opcional) - Búsqueda de texto en nombre, nombre_comercial y provincia (case-insensitive)
- `id_puntoop` (int, opcional) - Filtro exacto por id_puntoop
- `local_num` (int, opcional) - Filtro exacto por local_num
- `provincia` (string, opcional) - Filtro exacto por provincia
- `hasNota` (bool, opcional) - Filtrar por presencia de nota
  - `true`: Solo clientes con nota
  - `false`: Solo clientes sin nota
- `page` (int, opcional, default: 1) - Número de página
- `size` (int, opcional, default: 50, max: 100) - Tamaño de página

**Respuesta** (200 OK):
```json
{
  "items": [
    {
      "id": 1,
      "nombre": "Cliente Ejemplo",
      "idPuntoop": 123,
      "localNum": 456,
      "nombreComercial": "Comercial SA",
      "provincia": "Madrid",
      "dataUpdate": "2026-01-25T10:30:00Z",
      "dataHtml": null,
      "nota": "Cliente importante"
    }
  ],
  "totalCount": 150,
  "page": 1,
  "pageSize": 50,
  "totalPages": 3,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

**Ejemplos**:
```bash
# Listar primeros 20 clientes
GET /api/v1/clientes?page=1&size=20

# Buscar clientes en Madrid
GET /api/v1/clientes?q=madrid

# Clientes con nota
GET /api/v1/clientes?hasNota=true

# Filtro combinado
GET /api/v1/clientes?provincia=Barcelona&hasNota=false&page=1&size=50
```

---

### 2. GET /api/v1/clientes/{id} - Obtener cliente por ID

**Descripción**: Obtiene un cliente específico por su ID.

**Path Parameters**:
- `id` (int) - ID del cliente

**Respuesta** (200 OK):
```json
{
  "id": 1,
  "nombre": "Cliente Ejemplo",
  "idPuntoop": 123,
  "localNum": 456,
  "nombreComercial": "Comercial SA",
  "provincia": "Madrid",
  "dataUpdate": "2026-01-25T10:30:00Z",
  "dataHtml": null,
  "nota": "Cliente importante"
}
```

**Respuesta de Error** (404 Not Found):
```json
{
  "message": "Cliente con id 999 no encontrado"
}
```

---

### 3. POST /api/v1/clientes - Crear nuevo cliente

**Descripción**: Crea un nuevo cliente. El ID se genera automáticamente.

**Request Body**:
```json
{
  "nombre": "Nuevo Cliente SA",
  "idPuntoop": 123,
  "localNum": 456,
  "nombreComercial": "Nuevo Cliente",
  "provincia": "Madrid",
  "dataUpdate": "2026-01-25T10:30:00Z",
  "dataHtml": "<html>...</html>",
  "nota": "Cliente nuevo"
}
```

**Campos**:
- `nombre` (**requerido**, max 200 chars) - Nombre del cliente
- `idPuntoop` (opcional)
- `localNum` (opcional)
- `nombreComercial` (opcional, max 200 chars)
- `provincia` (opcional, max 100 chars)
- `dataUpdate` (opcional) - Si no se envía, se usa fecha actual
- `dataHtml` (opcional)
- `nota` (opcional)

**Respuesta** (201 Created):
```json
{
  "id": 123,
  "nombre": "Nuevo Cliente SA",
  "idPuntoop": 123,
  "localNum": 456,
  "nombreComercial": "Nuevo Cliente",
  "provincia": "Madrid",
  "dataUpdate": "2026-01-25T10:30:00Z",
  "dataHtml": "<html>...</html>",
  "nota": "Cliente nuevo"
}
```

**Headers**:
- `Location: /api/v1/clientes/123`

**Validaciones**:
- Nombre requerido y no vacío
- Nombre máximo 200 caracteres
- NombreComercial máximo 200 caracteres
- Provincia máximo 100 caracteres
- Espacios en blanco se recortan automáticamente (trim)
- Nota vacía se normaliza a `null`

---

### 4. PUT /api/v1/clientes/{id} - Actualizar cliente completo

**Descripción**: Reemplaza completamente los datos de un cliente existente.

**Path Parameters**:
- `id` (int) - ID del cliente a actualizar

**Request Body**:
```json
{
  "nombre": "Cliente Actualizado",
  "idPuntoop": 999,
  "localNum": 888,
  "nombreComercial": "Actualizado SA",
  "provincia": "Barcelona",
  "dataUpdate": "2026-01-25T11:00:00Z",
  "dataHtml": null,
  "nota": "Datos actualizados"
}
```

**Respuesta** (200 OK):
```json
{
  "id": 1,
  "nombre": "Cliente Actualizado",
  "idPuntoop": 999,
  "localNum": 888,
  "nombreComercial": "Actualizado SA",
  "provincia": "Barcelona",
  "dataUpdate": "2026-01-25T11:00:00Z",
  "dataHtml": null,
  "nota": "Datos actualizados"
}
```

**Respuesta de Error** (404 Not Found):
```json
{
  "message": "Cliente con id 999 no encontrado"
}
```

---

### 5. PATCH /api/v1/clientes/{id}/nota - Actualizar solo la nota

**Descripción**: Actualiza únicamente el campo `nota` de un cliente sin modificar otros campos.

**Path Parameters**:
- `id` (int) - ID del cliente

**Request Body**:
```json
{
  "nota": "Nueva nota actualizada"
}
```

**Respuesta** (200 OK):
```json
{
  "id": 1,
  "nombre": "Cliente Ejemplo",
  "idPuntoop": 123,
  "localNum": 456,
  "nombreComercial": "Comercial SA",
  "provincia": "Madrid",
  "dataUpdate": "2026-01-25T10:30:00Z",
  "dataHtml": null,
  "nota": "Nueva nota actualizada"
}
```

**Notas**:
- Para borrar la nota, enviar `{"nota": null}` o `{"nota": ""}`
- La nota se recorta automáticamente (trim)

---

### 6. DELETE /api/v1/clientes/{id} - Eliminar cliente

**Descripción**: Elimina un cliente del sistema.

**Path Parameters**:
- `id` (int) - ID del cliente a eliminar

**Respuesta** (204 No Content):
Sin cuerpo de respuesta.

**Respuesta de Error** (404 Not Found):
```json
{
  "message": "Cliente con id 999 no encontrado"
}
```

**Respuesta de Error** (409 Conflict):
```json
{
  "status": 409,
  "title": "No se puede eliminar el cliente",
  "detail": "El cliente está referenciado por otros registros en el sistema y no puede ser eliminado. Primero debe eliminar las referencias asociadas.",
  "instance": "/api/v1/clientes/1"
}
```

---

## Autenticación

Todos los endpoints requieren autenticación mediante Bearer Token:

```http
Authorization: Bearer <token>
```

## Swagger

Los endpoints están documentados en Swagger UI bajo el tag **"Clientes"**.

URL: `https://localhost:7096/swagger`

## Testing

Se incluye un script de prueba completo en:
```
scripts/test-clientes-crud.ps1
```

Para ejecutar:
```powershell
.\scripts\test-clientes-crud.ps1
```

Este script prueba:
1. ✅ Verificación de que `/api/v1/catalog/clientes` NO cambió
2. ✅ GET lista con paginación
3. ✅ GET por ID
4. ✅ POST crear cliente
5. ✅ PATCH actualizar nota
6. ✅ PUT actualizar completo
7. ✅ DELETE eliminar cliente
8. ✅ Filtros: `q`, `hasNota`, `provincia`, etc.

## Comparación con /api/v1/catalog/clientes

| Característica | `/api/v1/catalog/clientes` | `/api/v1/clientes` |
|---------------|---------------------------|-------------------|
| **Método** | GET únicamente | CRUD completo (GET, POST, PUT, PATCH, DELETE) |
| **Respuesta** | `[{id, nombre}]` simple | DTOs completos con todos los campos |
| **Paginación** | `limit` y `offset` | `page` y `size` con metadata |
| **Filtros** | Solo `q` (búsqueda) | `q`, `id_puntoop`, `local_num`, `provincia`, `hasNota` |
| **Campo Nota** | No incluido | Soportado completamente |
| **Propósito** | Catálogo simple para dropdowns | Gestión completa de clientes |

**IMPORTANTE**: Ambos endpoints conviven sin problemas. El endpoint de catalog se mantiene para compatibilidad con código existente.

## Logs

La API genera logs estructurados:

- **INFO**: Operaciones de creación, actualización y eliminación
- **DEBUG**: Filtros aplicados en consultas
- **WARNING**: Recursos no encontrados (404)
- **ERROR**: Errores de base de datos (ej. violación de FK en DELETE)

Ejemplo de log:
```
[INFO] CreateCliente: Cliente creado con id=123, nombre=Nuevo Cliente SA
[INFO] UpdateClienteNota: Nota de cliente id=123 actualizada correctamente
[ERROR] DeleteCliente: Error al eliminar cliente id=5. Posible violación de clave foránea
```

## Validaciones

### Validaciones de Request
- **Nombre**: Requerido, trim automático, máximo 200 caracteres
- **NombreComercial**: Opcional, trim automático, máximo 200 caracteres
- **Provincia**: Opcional, trim automático, máximo 100 caracteres
- **Nota**: Opcional, trim automático, cadena vacía se normaliza a `null`

### Validaciones de Negocio
- No se puede eliminar un cliente si está referenciado por otros registros (ej. partes de trabajo)
- Los IDs deben ser enteros positivos
- La paginación tiene límites: page >= 1, size entre 1 y 100

## Cambios en la Base de Datos

Si la columna `nota` no existe en la tabla `pss_dvnx.cliente`, debe agregarse:

```sql
ALTER TABLE pss_dvnx.cliente 
ADD COLUMN IF NOT EXISTS nota TEXT;
```

## Estructura de Archivos

```
Controllers/
  ├── ClientesController.cs          # Nuevo controller (CRUD completo)
  └── CatalogController.cs           # Existente (NO modificado)

Contracts/Catalog/
  ├── ClienteDto.cs                  # DTO completo para GET
  ├── ClienteCreateDto.cs            # DTO para POST
  ├── ClienteUpdateDto.cs            # DTO para PUT
  ├── ClienteUpdateNotaDto.cs        # DTO para PATCH nota
  └── ClientePagedResult.cs          # Resultado paginado

GestionTime.Domain/Work/
  └── Cliente.cs                     # Entidad actualizada con propiedad Nota

GestionTime.Infrastructure/Persistence/
  └── GestionTimeDbContext.cs        # Mapeo actualizado con columna 'nota'

scripts/
  └── test-clientes-crud.ps1         # Script de pruebas
```

## Próximos Pasos

1. ✅ Ejecutar migraciones si la columna `nota` no existe
2. ✅ Probar todos los endpoints con `test-clientes-crud.ps1`
3. ✅ Verificar en Swagger que los endpoints aparecen correctamente
4. ✅ Confirmar que `/api/v1/catalog/clientes` sigue funcionando sin cambios
5. ✅ Verificar logs de la aplicación

## Notas Técnicas

- **Entity Framework**: Se usa `AsNoTracking()` en consultas de solo lectura para mejor rendimiento
- **PostgreSQL**: El ID se genera automáticamente con SERIAL
- **Schema**: Todos los queries usan el schema `pss_dvnx` configurado en el DbContext
- **Normalización**: Los strings se recortan automáticamente (trim) y las cadenas vacías se convierten a `null`
- **Manejo de Errores**: Respuestas estándar HTTP con mensajes claros en español
