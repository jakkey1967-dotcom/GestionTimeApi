# NUEVO: CRUD Completo para Clientes

## 🎯 Objetivo

Implementar un CRUD completo para la entidad `Cliente` en la ruta `/api/v1/clientes`, **sin modificar** el endpoint existente `/api/v1/catalog/clientes` para mantener la compatibilidad con código existente.

## ✅ Cambios Realizados

### 1. Modelo de Dominio
- ✅ **GestionTime.Domain/Work/Cliente.cs**: Agregada propiedad `Nota`

### 2. Infraestructura
- ✅ **GestionTime.Infrastructure/Persistence/GestionTimeDbContext.cs**: Actualizado mapeo con columna `nota`
- ✅ **GestionTime.Infrastructure/Migrations/20260125_AddNotaToCliente.cs**: Migración para agregar columna `nota`

### 3. DTOs (Data Transfer Objects)
- ✅ **Contracts/Catalog/ClienteDto.cs**: DTO completo para respuestas GET
- ✅ **Contracts/Catalog/ClienteCreateDto.cs**: DTO para crear clientes (POST)
- ✅ **Contracts/Catalog/ClienteUpdateDto.cs**: DTO para actualizar clientes (PUT)
- ✅ **Contracts/Catalog/ClienteUpdateNotaDto.cs**: DTO para actualizar solo nota (PATCH)
- ✅ **Contracts/Catalog/ClientePagedResult.cs**: Resultado paginado con metadata

### 4. Controller Nuevo
- ✅ **Controllers/ClientesController.cs**: CRUD completo con los siguientes endpoints:
  - `GET /api/v1/clientes` - Lista con filtros y paginación
  - `GET /api/v1/clientes/{id}` - Obtener por ID
  - `POST /api/v1/clientes` - Crear nuevo cliente
  - `PUT /api/v1/clientes/{id}` - Actualizar cliente completo
  - `PATCH /api/v1/clientes/{id}/nota` - Actualizar solo la nota
  - `DELETE /api/v1/clientes/{id}` - Eliminar cliente

### 5. Scripts y Documentación
- ✅ **scripts/test-clientes-crud.ps1**: Script de pruebas completo
- ✅ **scripts/add-nota-column.sql**: Script SQL para migración manual
- ✅ **docs/CLIENTES_API.md**: Documentación completa de la API

## 🔍 Características Principales

### Filtros Avanzados
- **Búsqueda de texto** (`?q=`): Busca en nombre, nombre_comercial y provincia
- **Filtros exactos**: `id_puntoop`, `local_num`, `provincia`
- **Filtro de nota** (`?hasNota=true|false`): Clientes con/sin nota

### Paginación
- Parámetros: `page` (default: 1) y `size` (default: 50, max: 100)
- Respuesta con metadata: `totalCount`, `totalPages`, `hasNextPage`, `hasPreviousPage`

### Validaciones
- Nombre requerido (max 200 caracteres)
- Trim automático en strings
- Normalización de valores vacíos a `null`
- Validación de restricciones de integridad referencial en DELETE

### Logs Estructurados
- **INFO**: Operaciones CRUD exitosas
- **DEBUG**: Filtros aplicados
- **WARNING**: Recursos no encontrados
- **ERROR**: Errores de base de datos

## 🚫 NO MODIFICADO

El siguiente endpoint se mantiene **exactamente igual** para compatibilidad:
- ❌ **NO TOCAR**: `GET /api/v1/catalog/clientes`
- ❌ **NO TOCAR**: `Controllers/CatalogController.cs`

## 📊 Comparación de Endpoints

| Feature | `/api/v1/catalog/clientes` | `/api/v1/clientes` |
|---------|---------------------------|-------------------|
| Métodos | GET only | GET, POST, PUT, PATCH, DELETE |
| Respuesta | `[{id, nombre}]` | DTOs completos |
| Paginación | `limit` + `offset` | `page` + `size` + metadata |
| Filtros | Solo `q` | `q`, `id_puntoop`, `local_num`, `provincia`, `hasNota` |
| Campo Nota | ❌ No | ✅ Sí |

## 🧪 Testing

Ejecutar el script de pruebas:
```powershell
.\scripts\test-clientes-crud.ps1
```

Este script verifica:
1. ✅ Que `/api/v1/catalog/clientes` NO cambió
2. ✅ Todos los endpoints del nuevo CRUD
3. ✅ Filtros y paginación
4. ✅ Validaciones
5. ✅ Manejo de errores

## 🗄️ Migración de Base de Datos

### Opción 1: Migración EF Core
```bash
dotnet ef database update --project GestionTime.Infrastructure
```

### Opción 2: SQL Manual
```bash
psql -U postgres -d gestiontime -f scripts/add-nota-column.sql
```

### Opción 3: Desde PgAdmin
Ejecutar el contenido de `scripts/add-nota-column.sql`

## 📖 Documentación

Ver documentación completa en: **docs/CLIENTES_API.md**

Incluye:
- Especificación detallada de cada endpoint
- Ejemplos de request/response
- Códigos de error
- Casos de uso
- Comparación con endpoint de catalog

## ✔️ Compilación

```bash
dotnet build GestionTime.sln
```

**Estado**: ✅ Build exitoso

## 🎯 Próximos Pasos

1. ✅ Ejecutar migraciones
2. ✅ Ejecutar script de pruebas
3. ✅ Verificar Swagger UI
4. ✅ Confirmar que endpoint de catalog no cambió
5. ✅ Revisar logs de la aplicación

## 📝 Notas Técnicas

- **Schema**: `pss_dvnx`
- **Tabla**: `cliente`
- **ORM**: Entity Framework Core 8.0
- **Base de Datos**: PostgreSQL 9.4+
- **Autenticación**: Bearer Token (todos los endpoints requieren auth)
- **Tag Swagger**: "Clientes"

## 🔐 Seguridad

- ✅ Todos los endpoints requieren autenticación (`[Authorize]`)
- ✅ Validación de entrada con Data Annotations
- ✅ Sanitización automática (trim)
- ✅ Manejo seguro de excepciones
- ✅ Logs sin información sensible

---

**Fecha**: 2026-01-25  
**Desarrollado por**: GitHub Copilot  
**Estado**: ✅ Completado y listo para usar
