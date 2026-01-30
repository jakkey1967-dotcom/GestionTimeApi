# 🔄 Cambios en Backend API para GestionTime Desktop

> **Fecha:** Enero 2026  
> **Versión API:** v1  
> **Base URL:** `https://gestiontime-api.onrender.com/api/v1` (Producción)  
> **Base URL Local:** `http://localhost:2501/api/v1` (Desarrollo)

---

## 📋 Índice

- [Resumen Ejecutivo](#resumen-ejecutivo)
- [⚠️ Breaking Changes](#️-breaking-changes)
- [✨ Nuevos Endpoints](#-nuevos-endpoints)
  - [Clientes CRUD](#1-clientes-crud)
  - [Tipos CRUD](#2-tipos-crud)
  - [Grupos CRUD](#3-grupos-crud)
- [🔧 Mejoras Generales](#-mejoras-generales)
- [📝 Formato de Respuestas](#-formato-de-respuestas)
- [🧪 Testing](#-testing)

---

## Resumen Ejecutivo

Se han implementado **tres nuevos módulos CRUD completos** para mejorar la gestión de catálogos en GestionTime:

| Módulo | Endpoints | Estado | Paginación | Búsqueda |
|--------|-----------|--------|------------|----------|
| **Clientes** | 6 | ✅ Producción | ✅ Sí | ✅ Sí |
| **Tipos** | 5 | ✅ Producción | ✅ Sí | ✅ Sí |
| **Grupos** | 5 | ✅ Producción | ✅ Sí | ✅ Sí |

### Características Principales

✅ **Validación mejorada** con mensajes descriptivos en español  
✅ **Paginación configurable** en todos los listados  
✅ **Búsqueda full-text** en campos relevantes  
✅ **Manejo de errores consistente** (400, 404, 500)  
✅ **Case-insensitive JSON** (`nombre` = `Nombre` = `NOMBRE`)  
✅ **Logging detallado** para debugging  

---

## ⚠️ Breaking Changes

### 1. JSON Property Names (Case-Insensitive)

**ANTES (Estricto):**
```json
{
  "nombre": "value"  // ❌ Error si usabas minúsculas
}
```

**AHORA (Flexible):**
```json
{
  "Nombre": "value",   // ✅ PascalCase (recomendado)
  "nombre": "value",   // ✅ camelCase (también funciona)
  "NOMBRE": "value"    // ✅ Mayúsculas (también funciona)
}
```

### 2. Validación de Campos

Todos los endpoints ahora devuelven errores de validación **antes de llegar al servidor**:

**Respuesta de Error (400 Bad Request):**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Nombre": [
      "El nombre es requerido",
      "El nombre no puede exceder 120 caracteres"
    ]
  },
  "suggestion": "Envía un JSON con las propiedades en PascalCase...",
  "example": {
    "Nombre": "Instalación",
    "Descripcion": "Trabajos de instalación de equipos"
  }
}
```

### 3. Tipos de Datos

| Campo | Tipo Anterior | Tipo Actual | Notas |
|-------|---------------|-------------|-------|
| `Cliente.LocalNum` | `string` | `int?` | ⚠️ Ahora es numérico |
| `Cliente.IdPuntoop` | `string` | `int?` | ⚠️ Ahora es numérico |

**IMPORTANTE:** Si enviabas strings, ahora debes enviar números:

```json
// ❌ ANTES (Incorrecto ahora)
{
  "LocalNum": "TEST-001",
  "IdPuntoop": "9999"
}

// ✅ AHORA (Correcto)
{
  "LocalNum": 1,
  "IdPuntoop": 9999
}
```

---

## ✨ Nuevos Endpoints

## 1. Clientes CRUD

### 1.1 Listar Clientes (Con Paginación)

**GET** `/api/v1/clientes`

**Query Parameters:**
| Parámetro | Tipo | Requerido | Default | Descripción |
|-----------|------|-----------|---------|-------------|
| `page` | int | No | 1 | Número de página |
| `pageSize` | int | No | 50 | Elementos por página (max: 100) |
| `search` | string | No | - | Búsqueda en nombre, nombre comercial y provincia |

**Ejemplo Request:**
```http
GET /api/v1/clientes?page=1&pageSize=20&search=test
Authorization: Bearer {token}
```

**Ejemplo Response (200 OK):**
```json
{
  "items": [
    {
      "id": 1,
      "nombre": "AhorraCash/Bonacash",
      "idPuntoop": null,
      "localNum": null,
      "nombreComercial": null,
      "provincia": null,
      "dataUpdate": "2022-06-11T16:48:31.333Z",
      "dataHtml": null,
      "nota": null
    }
  ],
  "totalCount": 59,
  "page": 1,
  "pageSize": 50,
  "totalPages": 2,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

### 1.2 Obtener Cliente por ID

**GET** `/api/v1/clientes/{id}`

**Ejemplo Request:**
```http
GET /api/v1/clientes/1
Authorization: Bearer {token}
```

**Ejemplo Response (200 OK):**
```json
{
  "id": 1,
  "nombre": "AhorraCash/Bonacash",
  "idPuntoop": 1234,
  "localNum": 1,
  "nombreComercial": "Bonacash Store",
  "provincia": "Valencia",
  "dataUpdate": "2022-06-11T16:48:31.333Z",
  "dataHtml": "<div>...</div>",
  "nota": "Cliente VIP"
}
```

**Errores:**
- **404 Not Found:** Cliente no encontrado

### 1.3 Crear Cliente

**POST** `/api/v1/clientes`

**Request Body:**
```json
{
  "nombre": "Cliente Nuevo",           // ✅ Requerido (max 200 chars)
  "idPuntoop": 9999,                   // ⚠️ Opcional (int)
  "localNum": 1,                       // ⚠️ Opcional (int)
  "nombreComercial": "Comercial S.A.", // Opcional (max 200 chars)
  "provincia": "Madrid",               // Opcional (max 100 chars)
  "nota": "Cliente creado desde Desktop" // Opcional
}
```

**Ejemplo Response (201 Created):**
```json
{
  "id": 60,
  "nombre": "Cliente Nuevo",
  "idPuntoop": 9999,
  "localNum": 1,
  "nombreComercial": "Comercial S.A.",
  "provincia": "Madrid",
  "dataUpdate": "2026-01-29T21:30:00Z",
  "dataHtml": null,
  "nota": "Cliente creado desde Desktop"
}
```

**Headers:**
```
Location: /api/v1/clientes/60
```

**Validaciones:**
- `Nombre`: Requerido, máximo 200 caracteres
- `NombreComercial`: Máximo 200 caracteres
- `Provincia`: Máximo 100 caracteres
- `IdPuntoop`: Debe ser un número entero o null
- `LocalNum`: Debe ser un número entero o null

### 1.4 Actualizar Cliente (PUT)

**PUT** `/api/v1/clientes/{id}`

**Request Body (Completo):**
```json
{
  "nombre": "Cliente Actualizado",
  "idPuntoop": 9999,
  "localNum": 2,
  "nombreComercial": "Comercial Actualizado S.A.",
  "provincia": "Barcelona",
  "nota": "Cliente actualizado desde Desktop"
}
```

**Ejemplo Response (200 OK):**
```json
{
  "id": 60,
  "nombre": "Cliente Actualizado",
  "idPuntoop": 9999,
  "localNum": 2,
  "nombreComercial": "Comercial Actualizado S.A.",
  "provincia": "Barcelona",
  "dataUpdate": "2026-01-29T21:35:00Z",
  "dataHtml": null,
  "nota": "Cliente actualizado desde Desktop"
}
```

**Nota:** PUT requiere enviar **todos los campos** (es un reemplazo completo).

### 1.5 Actualizar Nota (PATCH)

**PATCH** `/api/v1/clientes/{id}/nota`

**Request Body:**
```json
{
  "nota": "Nueva nota actualizada parcialmente"
}
```

**Ejemplo Response (200 OK):**
```json
{
  "id": 60,
  "nombre": "Cliente Actualizado",
  "nota": "Nueva nota actualizada parcialmente",
  "...": "..."
}
```

**Ventaja:** Solo actualizas la nota sin necesidad de enviar todos los campos.

### 1.6 Eliminar Cliente

**DELETE** `/api/v1/clientes/{id}`

**Ejemplo Request:**
```http
DELETE /api/v1/clientes/60
Authorization: Bearer {token}
```

**Ejemplo Response (204 No Content):**
```
(Sin cuerpo)
```

**Errores:**
- **404 Not Found:** Cliente no encontrado

---

## 2. Tipos CRUD

### 2.1 Listar Tipos (Con Paginación)

**GET** `/api/v1/tipos`

**Query Parameters:**
| Parámetro | Tipo | Requerido | Default | Descripción |
|-----------|------|-----------|---------|-------------|
| `page` | int | No | 1 | Número de página |
| `pageSize` | int | No | 50 | Elementos por página (max: 100) |
| `search` | string | No | - | Búsqueda en nombre y descripción |

**Ejemplo Response (200 OK):**
```json
{
  "items": [
    {
      "id": 1,
      "nombre": "Instalación",
      "descripcion": "Trabajos de instalación de equipos"
    },
    {
      "id": 2,
      "nombre": "Mantenimiento",
      "descripcion": "Trabajos de mantenimiento preventivo"
    }
  ],
  "totalCount": 15,
  "page": 1,
  "pageSize": 50,
  "totalPages": 1,
  "hasNextPage": false,
  "hasPreviousPage": false
}
```

### 2.2 Obtener Tipo por ID

**GET** `/api/v1/tipos/{id}`

**Ejemplo Response (200 OK):**
```json
{
  "id": 1,
  "nombre": "Instalación",
  "descripcion": "Trabajos de instalación de equipos"
}
```

### 2.3 Crear Tipo

**POST** `/api/v1/tipos`

**Request Body:**
```json
{
  "nombre": "Consultoría",              // ✅ Requerido (max 120 chars)
  "descripcion": "Servicios de consultoría técnica" // Opcional (max 500 chars)
}
```

**Ejemplo Response (201 Created):**
```json
{
  "id": 16,
  "nombre": "Consultoría",
  "descripcion": "Servicios de consultoría técnica"
}
```

**Validaciones:**
- `Nombre`: Requerido, máximo 120 caracteres
- `Descripcion`: Opcional, máximo 500 caracteres

### 2.4 Actualizar Tipo

**PUT** `/api/v1/tipos/{id}`

**Request Body (Completo):**
```json
{
  "nombre": "Consultoría Avanzada",
  "descripcion": "Servicios de consultoría técnica especializada"
}
```

**Ejemplo Response (200 OK):**
```json
{
  "id": 16,
  "nombre": "Consultoría Avanzada",
  "descripcion": "Servicios de consultoría técnica especializada"
}
```

### 2.5 Eliminar Tipo

**DELETE** `/api/v1/tipos/{id}`

**Ejemplo Response (204 No Content):**
```
(Sin cuerpo)
```

---

## 3. Grupos CRUD

### 3.1 Listar Grupos (Con Paginación)

**GET** `/api/v1/grupos`

**Query Parameters:**
| Parámetro | Tipo | Requerido | Default | Descripción |
|-----------|------|-----------|---------|-------------|
| `page` | int | No | 1 | Número de página |
| `pageSize` | int | No | 50 | Elementos por página (max: 100) |
| `search` | string | No | - | Búsqueda en nombre y descripción |

**Ejemplo Response (200 OK):**
```json
{
  "items": [
    {
      "id": 1,
      "nombre": "Soporte Premium",
      "descripcion": "Clientes con soporte 24/7"
    },
    {
      "id": 2,
      "nombre": "Retail",
      "descripcion": "Clientes del sector retail"
    }
  ],
  "totalCount": 8,
  "page": 1,
  "pageSize": 50,
  "totalPages": 1,
  "hasNextPage": false,
  "hasPreviousPage": false
}
```

### 3.2 Obtener Grupo por ID

**GET** `/api/v1/grupos/{id}`

**Ejemplo Response (200 OK):**
```json
{
  "id": 1,
  "nombre": "Soporte Premium",
  "descripcion": "Clientes con soporte 24/7"
}
```

### 3.3 Crear Grupo

**POST** `/api/v1/grupos`

**Request Body:**
```json
{
  "nombre": "VIP",                      // ✅ Requerido (max 120 chars)
  "descripcion": "Clientes VIP prioritarios" // Opcional (max 500 chars)
}
```

**Ejemplo Response (201 Created):**
```json
{
  "id": 9,
  "nombre": "VIP",
  "descripcion": "Clientes VIP prioritarios"
}
```

**Validaciones:**
- `Nombre`: Requerido, máximo 120 caracteres
- `Descripcion`: Opcional, máximo 500 caracteres

### 3.4 Actualizar Grupo

**PUT** `/api/v1/grupos/{id}`

**Request Body (Completo):**
```json
{
  "nombre": "VIP Plus",
  "descripcion": "Clientes VIP con servicios adicionales"
}
```

**Ejemplo Response (200 OK):**
```json
{
  "id": 9,
  "nombre": "VIP Plus",
  "descripcion": "Clientes VIP con servicios adicionales"
}
```

### 3.5 Eliminar Grupo

**DELETE** `/api/v1/grupos/{id}`

**Ejemplo Response (204 No Content):**
```
(Sin cuerpo)
```

---

## 🔧 Mejoras Generales

### 1. Manejo de Errores Mejorado

Todos los endpoints ahora devuelven errores consistentes:

#### 400 Bad Request (Validación)
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Nombre": ["El nombre es requerido"],
    "Descripcion": ["La descripción no puede exceder 500 caracteres"]
  },
  "suggestion": "Revisa los campos marcados como errores...",
  "example": {
    "Nombre": "Instalación",
    "Descripcion": "Trabajos de instalación"
  }
}
```

#### 404 Not Found
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "Cliente con ID 999 no encontrado"
}
```

#### 500 Internal Server Error
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "Ha ocurrido un error interno. Contacta al administrador."
}
```

### 2. Case-Insensitive JSON

Puedes enviar propiedades en cualquier formato:

```json
// ✅ Todos son válidos
{ "Nombre": "Test" }
{ "nombre": "Test" }
{ "NOMBRE": "Test" }
{ "NoMbRe": "Test" }
```

**Recomendación:** Usa **PascalCase** (`Nombre`) para consistencia con .NET.

### 3. Logging Detallado

Todos los requests se loguean automáticamente en el servidor:

```log
[21:00:42 INF] 🔍 RequestBodyLoggingMiddleware - Processing POST /clientes
[21:00:42 INF] 🔍 ValidationLoggingFilter.OnActionExecuting - Clientes.Create - ModelState.IsValid=True
[21:00:42 INF] HTTP POST /api/v1/clientes respondió 201 en 45.2 ms
```

Si hay errores de validación:

```log
[21:00:42 WRN] ╔══════════════════════════════════════════════════════════════
[21:00:42 WRN] ║ VALIDACIÓN FALLIDA - ModelState Inválido
[21:00:42 WRN] ╠══════════════════════════════════════════════════════════════
[21:00:42 WRN] ║ Action: Clientes.Create
[21:00:42 WRN] ║ HTTP: POST /api/v1/clientes
[21:00:42 WRN] ╠══════════════════════════════════════════════════════════════
[21:00:42 WRN] ║ Campo: 'Nombre'
[21:00:42 WRN] ║   ❌ El nombre es requerido
[21:00:42 WRN] ╚══════════════════════════════════════════════════════════════
```

---

## 📝 Formato de Respuestas

### Respuestas Paginadas

Todas las listas usan este formato:

```json
{
  "items": [...],           // Array de elementos
  "totalCount": 59,         // Total de elementos (sin paginar)
  "page": 1,                // Página actual
  "pageSize": 50,           // Elementos por página
  "totalPages": 2,          // Total de páginas
  "hasNextPage": true,      // ¿Hay más páginas?
  "hasPreviousPage": false  // ¿Hay páginas anteriores?
}
```

### Códigos HTTP

| Código | Significado | Cuándo se usa |
|--------|-------------|---------------|
| 200 OK | Éxito | GET, PUT exitoso |
| 201 Created | Creado | POST exitoso |
| 204 No Content | Sin contenido | DELETE exitoso |
| 400 Bad Request | Request inválido | Validación fallida |
| 401 Unauthorized | No autenticado | Token inválido/expirado |
| 404 Not Found | No encontrado | Recurso no existe |
| 500 Internal Server Error | Error del servidor | Error inesperado |

---

## 🧪 Testing

### Scripts PowerShell Disponibles

```powershell
# Test completo de Clientes
.\scripts\test-clientes-with-logs.ps1

# Test completo de Tipos y Grupos
.\scripts\test-tipos-grupos-with-logs.ps1
```

### Ejemplo de Test con cURL

**Crear Cliente:**
```bash
curl -X POST https://gestiontime-api.onrender.com/api/v1/clientes \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "Nombre": "Test Cliente",
    "IdPuntoop": 1234,
    "LocalNum": 1,
    "Provincia": "Madrid"
  }'
```

**Listar Clientes:**
```bash
curl -X GET "https://gestiontime-api.onrender.com/api/v1/clientes?page=1&pageSize=20&search=test" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### Ejemplo de Test con C# (Desktop)

```csharp
using System.Net.Http;
using System.Net.Http.Json;

// Crear cliente
var nuevoCliente = new ClienteCreateDto
{
    Nombre = "Test Cliente",
    IdPuntoop = 1234,
    LocalNum = 1,
    NombreComercial = "Test S.A.",
    Provincia = "Madrid",
    Nota = "Cliente de prueba"
};

var response = await httpClient.PostAsJsonAsync(
    "api/v1/clientes", 
    nuevoCliente
);

if (response.IsSuccessStatusCode)
{
    var cliente = await response.Content.ReadFromJsonAsync<ClienteDto>();
    Console.WriteLine($"Cliente creado: ID={cliente.Id}");
}
else
{
    var error = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
    Console.WriteLine($"Error: {error.Title}");
    foreach (var err in error.Errors)
    {
        Console.WriteLine($"  - {err.Key}: {string.Join(", ", err.Value)}");
    }
}
```

---

## 📞 Contacto y Soporte

**Equipo Backend:**  
- Repositorio: https://github.com/jakkey1967-dotcom/GestionTimeApi
- Documentación completa: `/docs` en el repositorio

**Documentos relacionados:**
- `CLIENTES_API.md` - Documentación detallada de Clientes
- `TIPOS_GRUPOS_API.md` - Documentación detallada de Tipos y Grupos
- `BACKEND_CHANGES_2026-01-25.md` - Changelog completo

---

## ✅ Checklist de Integración Desktop

- [ ] Actualizar URLs de endpoints
- [ ] Implementar paginación en vistas de listado
- [ ] Agregar búsqueda en tiempo real
- [ ] Manejar errores de validación (400)
- [ ] Manejar errores 404 (recurso no encontrado)
- [ ] Actualizar modelos de datos (LocalNum e IdPuntoop ahora son `int?`)
- [ ] Probar case-insensitive JSON (opcional)
- [ ] Implementar refresh de listas después de CRUD
- [ ] Agregar indicadores de carga durante requests
- [ ] Testear con tokens expirados (401)

---

**Versión:** 1.0  
**Última actualización:** 29 Enero 2026  
**Estado:** ✅ En Producción
