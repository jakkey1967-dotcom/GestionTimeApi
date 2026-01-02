# 📚 GestionTime API - Documentación de Endpoints

**Versión:** 1.0.0  
**Base URL:** `https://gestiontimeapi.onrender.com`  
**Última actualización:** 01 Enero 2025

---

## 📋 Índice

- [Autenticación](#autenticación)
- [Gestión de Usuarios (Admin)](#gestión-de-usuarios-admin)
- [Perfiles de Usuario](#perfiles-de-usuario)
- [Health Check](#health-check)
- [Catálogo](#catálogo)
- [Partes de Trabajo](#partes-de-trabajo)

---

## 🔐 Autenticación

Base: `/api/v1/auth`

### 1. Login Web (con cookies)

**POST** `/api/v1/auth/login`

Autenticación de usuarios con almacenamiento de tokens en cookies HttpOnly.

**Body:**
```json
{
  "email": "usuario@ejemplo.com",
  "password": "123456"
}
```

**Respuesta Exitosa (200):**
```json
{
  "message": "ok",
  "userName": "Juan Pérez",
  "userEmail": "usuario@ejemplo.com",
  "userRole": "USER",
  "mustChangePassword": false,
  "daysUntilPasswordExpires": 90
}
```

**Respuesta con Cambio de Contraseña Requerido (200):**
```json
{
  "message": "password_change_required",
  "mustChangePassword": true,
  "passwordExpired": false,
  "daysUntilExpiration": 5,
  "userName": "Juan Pérez"
}
```

**Errores:**
- `401 Unauthorized` - Credenciales inválidas
- `401 Unauthorized` - Usuario deshabilitado

---

### 2. Login Desktop (sin cookies)

**POST** `/api/v1/auth/login-desktop`

Autenticación para aplicaciones desktop, devuelve tokens en JSON.

**Body:**
```json
{
  "email": "usuario@ejemplo.com",
  "password": "123456"
}
```

**Respuesta Exitosa (200):**
```json
{
  "message": "ok",
  "userName": "Juan Pérez",
  "userEmail": "usuario@ejemplo.com",
  "userRole": "USER",
  "mustChangePassword": false,
  "daysUntilPasswordExpires": 90,
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "a1b2c3d4e5f6...",
  "expiresAt": "2025-01-15T10:30:00Z"
}
```

---

### 3. Refresh Token

**POST** `/api/v1/auth/refresh`

Renueva el access token usando el refresh token almacenado en cookies.

**Headers:**
```
Cookie: refresh_token=<token>
```

**Respuesta Exitosa (200):**
```json
{
  "message": "ok"
}
```

**Nota:** Los nuevos tokens se envían automáticamente en cookies.

**Errores:**
- `401 Unauthorized` - Token inválido o expirado

---

### 4. Obtener Usuario Actual

**GET** `/api/v1/auth/me`

Obtiene información del usuario autenticado.

**Headers:**
```
Authorization: Bearer <access_token>
```

**Respuesta (200):**
```json
{
  "email": "usuario@ejemplo.com",
  "roles": ["USER", "MANAGER"]
}
```

---

### 5. Logout

**POST** `/api/v1/auth/logout`

Cierra la sesión del usuario y revoca el refresh token.

**Headers:**
```
Authorization: Bearer <access_token>
```

**Respuesta (200):**
```json
{
  "message": "bye"
}
```

---

### 6. Registro de Usuario

**POST** `/api/v1/auth/register`

Registra un nuevo usuario en el sistema. El usuario debe activar su cuenta por email.

**Body:**
```json
{
  "email": "nuevo@ejemplo.com",
  "fullName": "Juan Pérez",
  "password": "123456"
}
```

**Respuesta Exitosa (200):**
```json
{
  "success": true,
  "message": "Registro exitoso. Revisa tu email para activar tu cuenta.",
  "error": null
}
```

**Errores:**
- `400 Bad Request` - Email ya registrado
- `400 Bad Request` - Campos requeridos faltantes

---

### 7. Activar Cuenta por Email

**GET** `/api/v1/auth/activate/{token}`

Activa una cuenta de usuario usando el enlace recibido por email.

**Parámetros:**
- `token` (string) - Token de activación

**Respuesta:** Página HTML con el resultado de la activación

---

### 8. Verificar Email (Código)

**POST** `/api/v1/auth/verify-email`

Verifica el email usando un código de 6 dígitos.

**Body:**
```json
{
  "email": "usuario@ejemplo.com",
  "token": "123456"
}
```

**Respuesta (200):**
```json
{
  "success": true,
  "message": "Email verificado exitosamente. Ya puedes iniciar sesión."
}
```

---

### 9. Solicitar Recuperación de Contraseña

**POST** `/api/v1/auth/forgot-password`

Solicita un código de recuperación de contraseña enviado por email.

**Body:**
```json
{
  "email": "usuario@ejemplo.com"
}
```

**Respuesta (200):**
```json
{
  "success": true,
  "message": "Código de verificación enviado a tu correo.",
  "error": null
}
```

---

### 10. Resetear Contraseña

**POST** `/api/v1/auth/reset-password`

Resetea la contraseña usando el código recibido por email.

**Body:**
```json
{
  "email": "usuario@ejemplo.com",
  "token": "123456",
  "newPassword": "nuevaContraseña123"
}
```

**Respuesta Exitosa (200):**
```json
{
  "success": true,
  "message": "Contraseña actualizada correctamente.",
  "error": null
}
```

**Errores:**
- `400 Bad Request` - Token inválido o expirado
- `400 Bad Request` - Contraseña muy corta (mínimo 6 caracteres)

---

### 11. Cambiar Contraseña

**POST** `/api/v1/auth/change-password`

Cambia la contraseña del usuario (requiere contraseña actual).

**Body:**
```json
{
  "email": "usuario@ejemplo.com",
  "currentPassword": "contraseñaActual",
  "newPassword": "nuevaContraseña123"
}
```

**Respuesta Exitosa (200):**
```json
{
  "success": true,
  "message": "Contraseña actualizada correctamente",
  "error": null
}
```

**Errores:**
- `400 Bad Request` - Contraseña nueva igual a la actual
- `401 Unauthorized` - Contraseña actual incorrecta

---

### 12. Forzar Cambio de Contraseña (Admin)

**POST** `/api/v1/auth/force-password-change`

Requiere que un usuario cambie su contraseña en el próximo login.

**Headers:**
```
Authorization: Bearer <admin_access_token>
```

**Body:**
```json
{
  "email": "usuario@ejemplo.com"
}
```

**Respuesta (200):**
```json
{
  "success": true,
  "message": "Usuario usuario@ejemplo.com debe cambiar su contraseña en el próximo login",
  "error": null
}
```

**Requiere:** Rol `ADMIN`

---

## 👥 Gestión de Usuarios (Admin)

Base: `/api/v1/admin/users`

**Nota:** Todos estos endpoints requieren rol `ADMIN`.

### 1. Listar Usuarios

**GET** `/api/v1/admin/users`

Lista todos los usuarios del sistema con paginación.

**Query Parameters:**
- `q` (string, opcional) - Buscar por email o nombre
- `limit` (int, opcional, default: 50, max: 200) - Límite de resultados
- `offset` (int, opcional, default: 0) - Desplazamiento para paginación

**Headers:**
```
Authorization: Bearer <admin_access_token>
```

**Ejemplo:**
```
GET /api/v1/admin/users?q=juan&limit=10&offset=0
```

**Respuesta (200):**
```json
[
  {
    "id": "uuid",
    "email": "juan@ejemplo.com",
    "fullName": "Juan Pérez",
    "enabled": true,
    "roles": ["USER", "MANAGER"]
  },
  {
    "id": "uuid",
    "email": "maria@ejemplo.com",
    "fullName": "María García",
    "enabled": true,
    "roles": ["USER"]
  }
]
```

---

### 2. Crear Usuario

**POST** `/api/v1/admin/users`

Crea un nuevo usuario (con email pre-confirmado).

**Headers:**
```
Authorization: Bearer <admin_access_token>
```

**Body:**
```json
{
  "email": "nuevo@ejemplo.com",
  "fullName": "Usuario Nuevo",
  "password": "temporal123",
  "roles": ["USER"]
}
```

**Respuesta (200):**
```json
{
  "id": "uuid",
  "email": "nuevo@ejemplo.com",
  "fullName": "Usuario Nuevo"
}
```

**Errores:**
- `400 Bad Request` - Campos obligatorios faltantes
- `409 Conflict` - Email ya existe

---

### 3. Actualizar Roles de Usuario

**PUT** `/api/v1/admin/users/{id}/roles`

Actualiza los roles de un usuario.

**Headers:**
```
Authorization: Bearer <admin_access_token>
```

**Parámetros:**
- `id` (guid) - ID del usuario

**Body:**
```json
{
  "roles": ["USER", "MANAGER"]
}
```

**Roles disponibles:**
- `USER` - Usuario estándar
- `MANAGER` - Manager/Responsable
- `ADMIN` - Administrador

**Respuesta (200):**
```json
{
  "message": "Roles actualizados."
}
```

---

### 4. Activar/Desactivar Usuario

**PUT** `/api/v1/admin/users/{id}/enabled`

Habilita o deshabilita un usuario del sistema.

**Headers:**
```
Authorization: Bearer <admin_access_token>
```

**Parámetros:**
- `id` (guid) - ID del usuario

**Body:**
```json
{
  "enabled": false
}
```

**Respuesta (200):**
```json
{
  "message": "Estado actualizado.",
  "enabled": false
}
```

---

### 5. Resetear Contraseña de Usuario

**POST** `/api/v1/admin/users/{id}/reset-password`

Resetea la contraseña de un usuario a una nueva contraseña especificada.

**Headers:**
```
Authorization: Bearer <admin_access_token>
```

**Parámetros:**
- `id` (guid) - ID del usuario

**Body:**
```json
{
  "newPassword": "nuevaTemporal123"
}
```

**Respuesta (200):**
```json
{
  "message": "Password actualizado."
}
```

---

## 👤 Perfiles de Usuario

Base: `/api/v1/profiles`

### 1. Obtener Mi Perfil

**GET** `/api/v1/profiles/me`

Obtiene el perfil del usuario autenticado.

**Headers:**
```
Authorization: Bearer <access_token>
```

**Respuesta (200):**
```json
{
  "id": "uuid",
  "first_name": "Juan",
  "last_name": "Pérez",
  "full_name": "Juan Pérez",
  "phone": "+34 900 000 000",
  "mobile": "+34 600 000 000",
  "address": "Calle Ejemplo 123",
  "city": "Madrid",
  "postal_code": "28001",
  "department": "Tecnología",
  "position": "Desarrollador",
  "employee_type": "tecnico",
  "hire_date": "2020-01-15",
  "avatar_url": "https://example.com/avatar.jpg",
  "notes": "Notas adicionales",
  "created_at": "2024-01-01T00:00:00Z",
  "updated_at": "2024-01-01T00:00:00Z"
}
```

---

### 2. Actualizar Mi Perfil

**PUT** `/api/v1/profiles/me`

Actualiza el perfil del usuario autenticado.

**Headers:**
```
Authorization: Bearer <access_token>
```

**Body:**
```json
{
  "first_name": "Juan",
  "last_name": "Pérez",
  "phone": "+34 900 000 000",
  "mobile": "+34 600 000 000",
  "address": "Calle Ejemplo 123",
  "city": "Madrid",
  "postal_code": "28001",
  "department": "Tecnología",
  "position": "Senior Developer",
  "employee_type": "tecnico",
  "hire_date": "2020-01-15",
  "avatar_url": "https://example.com/avatar.jpg",
  "notes": "Actualizado"
}
```

**Respuesta (200):**
```json
{
  "message": "Perfil actualizado correctamente"
}
```

---

### 3. Obtener Perfil de Usuario (Admin)

**GET** `/api/v1/profiles/{userId}`

Obtiene el perfil de cualquier usuario.

**Headers:**
```
Authorization: Bearer <admin_access_token>
```

**Parámetros:**
- `userId` (guid) - ID del usuario

**Respuesta:** Mismo formato que "Obtener Mi Perfil"

**Requiere:** Rol `ADMIN` o `MANAGER`

---

### 4. Actualizar Perfil de Usuario (Admin)

**PUT** `/api/v1/profiles/{userId}`

Actualiza el perfil de cualquier usuario.

**Headers:**
```
Authorization: Bearer <admin_access_token>
```

**Parámetros:**
- `userId` (guid) - ID del usuario

**Body:** Mismo formato que "Actualizar Mi Perfil"

**Requiere:** Rol `ADMIN` o `MANAGER`

---

### 5. Listar Todos los Perfiles (Admin)

**GET** `/api/v1/profiles`

Lista todos los perfiles con filtros opcionales.

**Headers:**
```
Authorization: Bearer <admin_access_token>
```

**Query Parameters:**
- `employee_type` (string, opcional) - Filtrar por tipo de empleado
- `department` (string, opcional) - Filtrar por departamento

**Ejemplo:**
```
GET /api/v1/profiles?employee_type=tecnico&department=IT
```

**Respuesta (200):**
```json
[
  {
    "id": "uuid",
    "email": "juan@ejemplo.com",
    "first_name": "Juan",
    "last_name": "Pérez",
    "full_name": "Juan Pérez",
    "department": "IT",
    "position": "Developer",
    "employee_type": "tecnico"
  }
]
```

**Requiere:** Rol `ADMIN` o `MANAGER`

---

## 🏥 Health Check

### Health Check Principal

**GET** `/health`

Verifica el estado de la API y sus dependencias.

**Respuesta (200):**
```json
{
  "status": "OK",
  "timestamp": "2025-01-01T12:00:00Z",
  "service": "GestionTime API",
  "version": "1.0.0",
  "client": "PSS Desarrollo",
  "clientId": "pss_dvnx",
  "schema": "pss_dvnx",
  "environment": "Production",
  "uptime": "0d 2h 15m 30s",
  "database": "connected",
  "configuration": {
    "jwtAccessMinutes": 15,
    "jwtRefreshDays": 14,
    "emailConfirmationRequired": false,
    "selfRegistrationAllowed": true,
    "passwordExpirationDays": 90,
    "maxUsers": 50,
    "maxStorageGB": 10,
    "corsOriginsCount": 3
  }
}
```

**Respuesta Error (503):**
```json
{
  "status": "UNHEALTHY",
  "timestamp": "2025-01-01T12:00:00Z",
  "service": "GestionTime API",
  "database": "error",
  "error": "Cannot connect to database"
}
```

---

## 📋 Catálogo

Base: `/api/v1/catalog`

**Nota:** Endpoints del módulo de catálogo (implementación pendiente)

---

## 📊 Partes de Trabajo

Base: `/api/v1/partes`

### 1. Listar Partes de Trabajo

**GET** `/api/v1/partes`

Obtiene los partes de trabajo del usuario autenticado con múltiples opciones de filtrado.

**Headers:**
```
Authorization: Bearer <access_token>
```

**Query Parameters (todos opcionales):**

| Parámetro | Tipo | Descripción |
|-----------|------|-------------|
| `fecha` | date | Filtrar por fecha específica (YYYY-MM-DD) |
| `fechaInicio` | date | Fecha inicio del rango (YYYY-MM-DD) |
| `fechaFin` | date | Fecha fin del rango (YYYY-MM-DD) |
| `created_from` | datetime | Filtrar por fecha de creación desde |
| `created_to` | datetime | Filtrar por fecha de creación hasta |
| `q` | string | Buscar en acción o ticket |
| `estado` | int | Filtrar por estado (1=Abierto, 2=Pausado, 3=Cerrado, 4=Anulado) |

**Ejemplos de uso:**
```
# Partes de una fecha específica
GET /api/v1/partes?fecha=2025-01-15

# Partes de un rango de fechas (más nuevo a más antiguo)
GET /api/v1/partes?fechaInicio=2025-01-01&fechaFin=2025-01-31

# Buscar por texto
GET /api/v1/partes?q=reparacion

# Filtrar por estado
GET /api/v1/partes?estado=1

# Combinación de filtros
GET /api/v1/partes?fechaInicio=2025-01-01&fechaFin=2025-01-31&estado=3&q=ticket
```

**Respuesta (200):**
```json
[
  {
    "id": 1,
    "fecha": "2025-01-15",
    "cliente": "Cliente XYZ",
    "id_cliente": 1,
    "tienda": "Tienda 01",
    "accion": "Reparación de equipos",
    "horainicio": "09:00",
    "horafin": "11:30",
    "duracion_min": 150,
    "ticket": "TKT-12345",
    "grupo": "Mantenimiento",
    "id_grupo": 1,
    "tipo": "Correctivo",
    "id_tipo": 2,
    "tecnico": "Juan Pérez",
    "estado": 3,
    "estado_nombre": "Cerrado",
    "created_at": "2025-01-15T09:00:00Z",
    "updated_at": "2025-01-15T11:30:00Z"
  },
  {
    "id": 2,
    "fecha": "2025-01-14",
    "cliente": "Cliente ABC",
    "id_cliente": 2,
    "tienda": "Tienda 02",
    "accion": "Instalación de software",
    "horainicio": "14:00",
    "horafin": "16:00",
    "duracion_min": 120,
    "ticket": null,
    "grupo": "Instalaciones",
    "id_grupo": 2,
    "tipo": "Preventivo",
    "id_tipo": 1,
    "tecnico": "Juan Pérez",
    "estado": 1,
    "estado_nombre": "Abierto",
    "created_at": "2025-01-14T14:00:00Z",
    "updated_at": "2025-01-14T14:00:00Z"
  }
]
```

**Nota:** Los resultados están ordenados de más nuevo a más antiguo (por fecha descendente y hora descendente).

---

### 2. Obtener Estados Disponibles

**GET** `/api/v1/partes/estados`

Obtiene la lista de estados posibles para los partes de trabajo.

**Headers:**
```
Authorization: Bearer <access_token>
```

**Respuesta (200):**
```json
[
  {
    "id": 1,
    "nombre": "Abierto"
  },
  {
    "id": 2,
    "nombre": "Pausado"
  },
  {
    "id": 3,
    "nombre": "Cerrado"
  },
  {
    "id": 4,
    "nombre": "Anulado"
  }
]
```

---

### 3. Crear Parte de Trabajo

**POST** `/api/v1/partes`

Crea un nuevo parte de trabajo.

**Headers:**
```
Authorization: Bearer <access_token>
```

**Body:**
```json
{
  "fecha_trabajo": "2025-01-15",
  "hora_inicio": "09:00",
  "hora_fin": "11:30",
  "id_cliente": 1,
  "tienda": "Tienda 01",
  "id_grupo": 1,
  "id_tipo": 2,
  "accion": "Reparación de equipos",
  "ticket": "TKT-12345"
}
```

**Respuesta (200):**
```json
{
  "id": 1
}
```

**Errores:**
- `400 Bad Request` - Hora inválida o hora_fin menor que hora_inicio

---

### 4. Actualizar Parte de Trabajo

**PUT** `/api/v1/partes/{id}`

Actualiza un parte de trabajo existente (solo si está en estado editable).

**Headers:**
```
Authorization: Bearer <access_token>
```

**Parámetros:**
- `id` (long) - ID del parte de trabajo

**Body:**
```json
{
  "fecha_trabajo": "2025-01-15",
  "hora_inicio": "09:00",
  "hora_fin": "12:00",
  "id_cliente": 1,
  "tienda": "Tienda 01",
  "id_grupo": 1,
  "id_tipo": 2,
  "accion": "Reparación de equipos - Actualizado",
  "ticket": "TKT-12345",
  "estado": 1
}
```

**Respuesta (200):**
```json
{
  "message": "ok"
}
```

**Errores:**
- `400 Bad Request` - Parte no puede ser editado (estado no editable)
- `404 Not Found` - Parte no encontrado o no pertenece al usuario

---

### 5. Eliminar Parte de Trabajo

**DELETE** `/api/v1/partes/{id}`

Elimina un parte de trabajo (solo si está en estado editable).

**Headers:**
```
Authorization: Bearer <access_token>
```

**Parámetros:**
- `id` (long) - ID del parte de trabajo

**Respuesta (200):**
```json
{
  "message": "ok"
}
```

**Errores:**
- `400 Bad Request` - Parte no puede ser eliminado (estado no editable)

---

### 6. Pausar Parte de Trabajo

**POST** `/api/v1/partes/{id}/pause`

Pausa un parte de trabajo en curso.

**Headers:**
```
Authorization: Bearer <access_token>
```

**Respuesta (200):**
```json
{
  "message": "ok",
  "estado": 2,
  "estado_nombre": "Pausado"
}
```

---

### 7. Reanudar Parte de Trabajo

**POST** `/api/v1/partes/{id}/resume`

Reanuda un parte de trabajo pausado.

**Headers:**
```
Authorization: Bearer <access_token>
```

**Respuesta (200):**
```json
{
  "message": "ok",
  "estado": 1,
  "estado_nombre": "Abierto"
}
```

**Errores:**
- `400 Bad Request` - Solo se puede reanudar un parte pausado

---

### 8. Cerrar Parte de Trabajo

**POST** `/api/v1/partes/{id}/close`

Cierra un parte de trabajo.

**Headers:**
```
Authorization: Bearer <access_token>
```

**Respuesta (200):**
```json
{
  "message": "ok",
  "estado": 3,
  "estado_nombre": "Cerrado"
}
```

---

### 9. Anular Parte de Trabajo

**POST** `/api/v1/partes/{id}/cancel`

Anula un parte de trabajo.

**Headers:**
```
Authorization: Bearer <access_token>
```

**Respuesta (200):**
```json
{
  "message": "ok",
  "estado": 4,
  "estado_nombre": "Anulado"
}
```

---

### 10. Cambiar Estado de Parte

**POST** `/api/v1/partes/{id}/estado`

Cambia el estado de un parte de trabajo de forma genérica.

**Headers:**
```
Authorization: Bearer <access_token>
```

**Body:**
```json
{
  "estado": 3
}
```

**Respuesta (200):**
```json
{
  "message": "ok",
  "estado": 3,
  "estado_nombre": "Cerrado"
}
```

---

## 📋 Catálogo

Base: `/api/v1/catalog`

**Nota:** Endpoints del módulo de catálogo (implementación pendiente)

---

## 🔒 Autenticación y Autorización

### Tipos de Autenticación

#### 1. Cookies HttpOnly (Web)
Los tokens se almacenan automáticamente en cookies HttpOnly:
- `access_token` - Token JWT de acceso (15 minutos)
- `refresh_token` - Token de renovación (14 días)

#### 2. Bearer Token (Desktop/Mobile)
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Roles Disponibles

| Rol | Descripción | Permisos |
|-----|-------------|----------|
| `USER` | Usuario estándar | Acceso básico |
| `MANAGER` | Manager/Responsable | Gestión de equipo |
| `ADMIN` | Administrador | Acceso completo |

---

## 📝 Códigos de Estado HTTP

| Código | Descripción |
|--------|-------------|
| `200 OK` | Operación exitosa |
| `400 Bad Request` | Datos inválidos o faltantes |
| `401 Unauthorized` | No autenticado o token inválido |
| `403 Forbidden` | Sin permisos suficientes |
| `404 Not Found` | Recurso no encontrado |
| `409 Conflict` | Conflicto (ej: email duplicado) |
| `500 Internal Server Error` | Error del servidor |
| `503 Service Unavailable` | Servicio no disponible |

---

## 🔄 Flujos Comunes

### Flujo de Login Web
```
1. POST /api/v1/auth/login → Recibe cookies automáticamente
2. Usa cookies en requests siguientes (automático)
3. POST /api/v1/auth/refresh → Renueva token automáticamente
4. POST /api/v1/auth/logout → Limpia cookies
```

### Flujo de Login Desktop
```
1. POST /api/v1/auth/login-desktop → Recibe tokens en JSON
2. Guarda tokens localmente
3. Incluye "Authorization: Bearer {token}" en requests
4. Renueva token cuando expire
```

### Flujo de Registro
```
1. POST /api/v1/auth/register
2. Usuario recibe email con enlace de activación
3. GET /api/v1/auth/activate/{token} → Activa cuenta
4. POST /api/v1/auth/login → Puede iniciar sesión
```

### Flujo de Recuperación de Contraseña
```
1. POST /api/v1/auth/forgot-password
2. Usuario recibe email con código de 6 dígitos
3. POST /api/v1/auth/reset-password con código
4. POST /api/v1/auth/login con nueva contraseña
```

---

## 📧 Correos Electrónicos

El sistema envía emails en los siguientes casos:

| Evento | Template | Contenido |
|--------|----------|-----------|
| Registro | `SendActivationEmailAsync` | Enlace de activación de cuenta |
| Recuperación | `SendPasswordResetEmailAsync` | Código de 6 dígitos |
| Verificación | `SendRegistrationEmailAsync` | Código de verificación |

**SMTP Configurado:** IONOS (`smtp.ionos.es:587`)

---

## 🔧 Variables de Entorno

Para configuración en producción (Render):

```env
DATABASE_URL=<auto-configurada>
DB_SCHEMA=pss_dvnx
JWT_SECRET_KEY=<tu-secret-key>
APP_BASE_URL=https://gestiontimeapi.onrender.com
ASPNETCORE_ENVIRONMENT=Production
SMTP_HOST=smtp.ionos.es
SMTP_PORT=587
SMTP_USER=envio_noreplica@tdkportal.com
SMTP_PASSWORD=<tu-password>
```

---

## 📚 Recursos Adicionales

- **Swagger UI:** https://gestiontimeapi.onrender.com/swagger
- **Health Check:** https://gestiontimeapi.onrender.com/health
- **Repositorio:** https://github.com/jakkey1967-dotcom/GestionTimeApi

---

**Última actualización:** 01 Enero 2025  
**Versión de la API:** 1.0.0  
**Estado:** ✅ Producción
