# ✅ CRUD DE CLIENTES IMPLEMENTADO

## 🎉 Resumen de Implementación

Se ha implementado exitosamente un **CRUD completo** para la entidad `Cliente` en la ruta `/api/v1/clientes`, **sin modificar** el endpoint existente `/api/v1/catalog/clientes`.

## 📋 Archivos Creados/Modificados

### ✅ Archivos Nuevos (13)
1. `Controllers/ClientesController.cs` - Controller con CRUD completo
2. `Contracts/Catalog/ClienteDto.cs` - DTO para respuestas
3. `Contracts/Catalog/ClienteCreateDto.cs` - DTO para POST
4. `Contracts/Catalog/ClienteUpdateDto.cs` - DTO para PUT
5. `Contracts/Catalog/ClienteUpdateNotaDto.cs` - DTO para PATCH
6. `Contracts/Catalog/ClientePagedResult.cs` - Resultado paginado
7. `GestionTime.Infrastructure/Migrations/20260125_AddNotaToCliente.cs` - Migración
8. `scripts/test-clientes-crud.ps1` - Script de pruebas
9. `scripts/add-nota-column.sql` - Script SQL para migración
10. `docs/CLIENTES_API.md` - Documentación completa
11. `docs/CLIENTES_CRUD_README.md` - README del feature

### 🔄 Archivos Modificados (2)
1. `GestionTime.Domain/Work/Cliente.cs` - Agregada propiedad `Nota`
2. `GestionTime.Infrastructure/Persistence/GestionTimeDbContext.cs` - Mapeo de columna `nota`

### ❌ NO MODIFICADOS (mantienen compatibilidad)
- `Controllers/CatalogController.cs` - Sin cambios
- Endpoint `/api/v1/catalog/clientes` - Funciona igual que antes

## 🚀 Endpoints Disponibles

### Nuevo CRUD: `/api/v1/clientes`
```
GET    /api/v1/clientes              - Lista con filtros y paginación
GET    /api/v1/clientes/{id}         - Obtener por ID
POST   /api/v1/clientes              - Crear nuevo
PUT    /api/v1/clientes/{id}         - Actualizar completo
PATCH  /api/v1/clientes/{id}/nota    - Actualizar solo nota
DELETE /api/v1/clientes/{id}         - Eliminar
```

### Existente (sin cambios): `/api/v1/catalog/clientes`
```
GET    /api/v1/catalog/clientes      - Lista simple para dropdowns
```

## 🔧 Pasos Siguientes (EN ORDEN)

### 1️⃣ Aplicar Migración de Base de Datos

**Opción A: Entity Framework**
```bash
cd C:\GestionTime\GestionTimeApi
dotnet ef database update --project GestionTime.Infrastructure
```

**Opción B: SQL Manual**
```bash
psql -U postgres -d gestiontime -f scripts/add-nota-column.sql
```

**Opción C: PgAdmin**
- Abrir PgAdmin
- Conectar a la base de datos
- Ejecutar el contenido de `scripts/add-nota-column.sql`

### 2️⃣ Ejecutar la API

```bash
cd C:\GestionTime\GestionTimeApi
dotnet run --project GestionTime.Api
```

O desde Visual Studio: F5

### 3️⃣ Ejecutar Tests

```powershell
cd C:\GestionTime\GestionTimeApi
.\scripts\test-clientes-crud.ps1
```

Este script verifica:
- ✅ Que `/api/v1/catalog/clientes` NO cambió
- ✅ Todos los endpoints del CRUD
- ✅ Filtros y paginación
- ✅ Validaciones
- ✅ Creación, actualización y eliminación

### 4️⃣ Verificar Swagger

Abrir en el navegador:
```
https://localhost:7096/swagger
```

Buscar el tag **"Clientes"** y probar los endpoints.

## 📖 Documentación

### Documentación Completa
Ver: `docs/CLIENTES_API.md`

Incluye:
- Especificación detallada de cada endpoint
- Ejemplos de request/response
- Códigos de error y manejo
- Casos de uso
- Comparación con endpoint de catalog

### README del Feature
Ver: `docs/CLIENTES_CRUD_README.md`

## 🧪 Pruebas Manuales Rápidas

### Verificar que catalog NO cambió
```bash
curl -X GET "https://localhost:7096/api/v1/catalog/clientes?limit=5" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -k
```

Debe retornar: `[{id: 1, nombre: "..."}, ...]`

### Probar nuevo endpoint
```bash
curl -X GET "https://localhost:7096/api/v1/clientes?page=1&size=5" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -k
```

Debe retornar objeto con: `items`, `totalCount`, `page`, `pageSize`, etc.

## 🔍 Filtros Disponibles

El nuevo endpoint soporta múltiples filtros:

```
GET /api/v1/clientes?q=madrid                    # Búsqueda de texto
GET /api/v1/clientes?provincia=Madrid            # Filtro exacto
GET /api/v1/clientes?hasNota=true                # Con/sin nota
GET /api/v1/clientes?id_puntoop=123              # Por id_puntoop
GET /api/v1/clientes?local_num=456               # Por local_num
GET /api/v1/clientes?page=2&size=50              # Paginación
GET /api/v1/clientes?q=madrid&hasNota=true       # Filtros combinados
```

## ✅ Checklist de Verificación

- [ ] Compilación exitosa (`dotnet build GestionTime.sln`)
- [ ] Migración aplicada (columna `nota` existe)
- [ ] API ejecutándose
- [ ] Tests ejecutados sin errores
- [ ] Swagger muestra endpoints bajo tag "Clientes"
- [ ] Endpoint `/api/v1/catalog/clientes` sigue funcionando igual
- [ ] Endpoints del nuevo CRUD funcionan correctamente
- [ ] Filtros funcionan
- [ ] Paginación funciona
- [ ] PATCH de nota funciona
- [ ] DELETE con validación de FK funciona

## 🎯 Casos de Uso Principales

### 1. Listar clientes con búsqueda
```http
GET /api/v1/clientes?q=empresa&page=1&size=20
```

### 2. Obtener detalles de un cliente
```http
GET /api/v1/clientes/123
```

### 3. Crear nuevo cliente
```http
POST /api/v1/clientes
Content-Type: application/json

{
  "nombre": "Nuevo Cliente SA",
  "nombreComercial": "Nuevo SA",
  "provincia": "Madrid",
  "nota": "Cliente potencial"
}
```

### 4. Actualizar solo la nota
```http
PATCH /api/v1/clientes/123/nota
Content-Type: application/json

{
  "nota": "Cliente confirmado"
}
```

### 5. Actualizar cliente completo
```http
PUT /api/v1/clientes/123
Content-Type: application/json

{
  "nombre": "Cliente Actualizado",
  "nombreComercial": "Actualizado SA",
  "provincia": "Barcelona",
  "nota": "Datos actualizados"
}
```

### 6. Eliminar cliente
```http
DELETE /api/v1/clientes/123
```

## 📊 Comparación de Respuestas

### Endpoint Antiguo (catalog)
```json
[
  { "id": 1, "nombre": "Cliente A" },
  { "id": 2, "nombre": "Cliente B" }
]
```

### Endpoint Nuevo (clientes)
```json
{
  "items": [
    {
      "id": 1,
      "nombre": "Cliente A",
      "idPuntoop": 123,
      "localNum": 456,
      "nombreComercial": "Comercial A",
      "provincia": "Madrid",
      "dataUpdate": "2026-01-25T10:00:00Z",
      "dataHtml": null,
      "nota": "Cliente importante"
    }
  ],
  "totalCount": 150,
  "page": 1,
  "pageSize": 20,
  "totalPages": 8,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

## 🐛 Troubleshooting

### Error: "La columna 'nota' no existe"
Solución: Aplicar la migración (ver paso 1️⃣)

### Error: "No puedo eliminar el cliente"
Causa: El cliente está referenciado por otros registros (FK)
Solución: Eliminar primero las referencias o entender que es el comportamiento esperado

### Script de pruebas falla en login
Solución: Verificar credenciales en el script (`admin@gestiontime.com` / `Admin123!`)

### Swagger no muestra los endpoints
Solución: Recompilar y reiniciar la API

## 📝 Logs

La aplicación genera logs estructurados:

```
[INFO] CreateCliente: Cliente creado con id=123, nombre=Nuevo Cliente SA
[INFO] UpdateClienteNota: Nota de cliente id=123 actualizada correctamente
[DEBUG] GetClientes: q=madrid, page=1, size=20
[ERROR] DeleteCliente: Error al eliminar cliente id=5. Posible violación de clave foránea
```

## 🔐 Seguridad

- ✅ Todos los endpoints requieren autenticación Bearer Token
- ✅ Validación de entrada con Data Annotations
- ✅ Sanitización automática (trim)
- ✅ Manejo seguro de excepciones
- ✅ Sin información sensible en logs

## 📈 Estado del Proyecto

| Item | Estado |
|------|--------|
| Compilación | ✅ Exitosa |
| Tests Unitarios | ⏳ Pendiente |
| Migración BD | ⏳ Pendiente (ejecutar) |
| Tests Manuales | ⏳ Pendiente (ejecutar script) |
| Documentación | ✅ Completa |
| Commit & Push | ✅ Realizado |

## 🎓 Recursos

- **Documentación API**: `docs/CLIENTES_API.md`
- **README Feature**: `docs/CLIENTES_CRUD_README.md`
- **Script de Pruebas**: `scripts/test-clientes-crud.ps1`
- **Migración SQL**: `scripts/add-nota-column.sql`
- **Swagger UI**: https://localhost:7096/swagger

## ✨ Características Destacadas

✅ CRUD completo (GET, POST, PUT, PATCH, DELETE)  
✅ Paginación con metadata completa  
✅ Filtros avanzados (búsqueda de texto, filtros exactos, hasNota)  
✅ Validaciones robustas  
✅ Logs estructurados  
✅ Documentación completa  
✅ Script de pruebas automatizado  
✅ Manejo de errores con mensajes claros  
✅ Compatible con endpoint existente (NO breaking changes)  

---

**🎉 ¡Implementación completada exitosamente!**

**Próximo paso**: Ejecutar la migración y probar los endpoints con el script de pruebas.
