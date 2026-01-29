# API CRUD - Tipos y Grupos

## 📋 Descripción

Endpoints para gestionar los catálogos de **Tipos** y **Grupos** que se utilizan en los partes de trabajo.

- **Tipos**: Categorías de trabajo (ej: Instalación, Reparación, Mantenimiento)
- **Grupos**: Agrupación de clientes o equipos (ej: Soporte Premium, Clientes VIP)

## 🔗 Endpoints

### Tipos

#### 1. Listar todos los tipos
```http
GET /api/v1/tipos
```

**Response 200 OK:**
```json
[
  {
    "id": 1,
    "nombre": "Instalación",
    "descripcion": "Trabajos de instalación de equipos"
  },
  {
    "id": 2,
    "nombre": "Reparación",
    "descripcion": null
  }
]
```

#### 2. Obtener un tipo por ID
```http
GET /api/v1/tipos/{id}
```

**Response 200 OK:**
```json
{
  "id": 1,
  "nombre": "Instalación",
  "descripcion": "Trabajos de instalación de equipos"
}
```

**Response 404 Not Found:**
```json
{
  "message": "Tipo no encontrado"
}
```

#### 3. Crear un nuevo tipo
```http
POST /api/v1/tipos
Content-Type: application/json

{
  "nombre": "Instalación",
  "descripcion": "Trabajos de instalación de equipos"
}
```

**Validaciones:**
- `nombre`: Requerido, max 120 caracteres, no puede estar duplicado
- `descripcion`: Opcional, max 500 caracteres

**Response 201 Created:**
```json
{
  "id": 3,
  "nombre": "Instalación",
  "descripcion": "Trabajos de instalación de equipos"
}
```

**Response 400 Bad Request:**
```json
{
  "message": "El nombre es requerido"
}
```

**Response 409 Conflict:**
```json
{
  "message": "Ya existe un tipo con ese nombre"
}
```

#### 4. Actualizar un tipo
```http
PUT /api/v1/tipos/{id}
Content-Type: application/json

{
  "nombre": "Instalación y Configuración",
  "descripcion": "Trabajos de instalación y configuración completa"
}
```

**Response 200 OK:**
```json
{
  "id": 1,
  "nombre": "Instalación y Configuración",
  "descripcion": "Trabajos de instalación y configuración completa"
}
```

**Response 404 Not Found / 409 Conflict:** (igual que en CREATE)

#### 5. Eliminar un tipo
```http
DELETE /api/v1/tipos/{id}
```

**Response 204 No Content:** (eliminado correctamente)

**Response 404 Not Found:**
```json
{
  "message": "Tipo no encontrado"
}
```

**Response 409 Conflict:**
```json
{
  "message": "No se puede borrar: hay partes de trabajo que usan este tipo"
}
```

### Grupos

Los endpoints de Grupos tienen la misma estructura que Tipos, pero en la ruta `/api/v1/grupos`:

```http
GET    /api/v1/grupos          # Listar todos
GET    /api/v1/grupos/{id}     # Obtener por ID
POST   /api/v1/grupos          # Crear nuevo
PUT    /api/v1/grupos/{id}     # Actualizar
DELETE /api/v1/grupos/{id}     # Eliminar
```

**Ejemplo de body para crear/actualizar grupo:**
```json
{
  "nombre": "Soporte Premium",
  "descripcion": "Clientes con soporte premium 24/7"
}
```

## 🔐 Autenticación

Todos los endpoints requieren autenticación JWT. Incluir el token en:
- Cookie `access_token` (automático si usas `-WebSession` en PowerShell)
- O header `Authorization: Bearer {token}`

## 🧪 Testing

Ejecutar el script de pruebas:

```powershell
.\scripts\test-tipos-grupos-crud.ps1
```

Este script prueba:
1. ✅ Listar tipos/grupos
2. ✅ Crear nuevo
3. ✅ Obtener por ID
4. ✅ Actualizar
5. ✅ Validación de duplicados (409 Conflict)
6. ✅ Eliminar (con validación de uso)

## 📊 Esquema de Base de Datos

### Tabla `pss_dvnx.tipo`
```sql
CREATE TABLE pss_dvnx.tipo (
    id_tipo SERIAL PRIMARY KEY,
    nombre VARCHAR(120) NOT NULL,
    descripcion VARCHAR(500)
);
```

### Tabla `pss_dvnx.grupo`
```sql
CREATE TABLE pss_dvnx.grupo (
    id_grupo SERIAL PRIMARY KEY,
    nombre VARCHAR(120) NOT NULL,
    descripcion VARCHAR(500)
);
```

### Relaciones
- `pss_dvnx.partesdetrabajo.id_tipo` → `pss_dvnx.tipo.id_tipo`
- `pss_dvnx.partesdetrabajo.id_grupo` → `pss_dvnx.grupo.id_grupo`

## ⚠️ Integridad Referencial

No se puede eliminar un tipo o grupo si está siendo usado por algún parte de trabajo. La API devolverá `409 Conflict` en ese caso.

Para verificar el uso:
```sql
-- Ver qué tipos están en uso
SELECT t.id_tipo, t.nombre, COUNT(p.id) as partes_count
FROM pss_dvnx.tipo t
LEFT JOIN pss_dvnx.partesdetrabajo p ON p.id_tipo = t.id_tipo
GROUP BY t.id_tipo, t.nombre;

-- Ver qué grupos están en uso
SELECT g.id_grupo, g.nombre, COUNT(p.id) as partes_count
FROM pss_dvnx.grupo g
LEFT JOIN pss_dvnx.partesdetrabajo p ON p.id_grupo = g.id_grupo
GROUP BY g.id_grupo, g.nombre;
```

## 📁 Archivos Implementados

```
Contracts/Catalog/
  ├── TipoDto.cs              # DTOs para Tipo
  └── GrupoDto.cs             # DTOs para Grupo

Services/
  ├── TipoService.cs          # Lógica de negocio Tipos
  └── GrupoService.cs         # Lógica de negocio Grupos

Controllers/
  ├── TiposController.cs      # API REST Tipos
  └── GruposController.cs     # API REST Grupos

GestionTime.Domain/Work/
  ├── Tipo.cs                 # Entidad (ya existía)
  └── Grupo.cs                # Entidad (ya existía)
```

## ✅ Sin Migraciones

Como se solicitó, **NO se han creado migraciones EF Core**. Las tablas ya existen en la BD.

Si necesitas crear las tablas manualmente:
```sql
-- Ya existen en pss_dvnx schema
-- No es necesario ejecutar nada
```

## 🚀 Próximos Pasos

1. Arrancar la API: `dotnet run`
2. Ejecutar tests: `.\scripts\test-tipos-grupos-crud.ps1`
3. Integrar en frontend para gestionar catálogos
4. Usar en dropdowns al crear/editar partes de trabajo
