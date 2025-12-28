# Sistema de Perfiles de Usuario - GestionTime

## ?? Resumen

Sistema de perfiles de usuario con información personal, laboral y de contacto. Relación 1:1 con la tabla `users`.
postgresql://psoftwareitem_user:PObwCkQIJIcyzB8EHKSIJcjMhWDnU3XM@dpg-d57jbqeuk2gs73d3anog-a/psoftwareitem
---

## ??? Estructura de Datos

### Tabla `user_profiles`

| Campo | Tipo | Nullable | Descripción |
|-------|------|----------|-------------|
| `id` | UUID (PK, FK) | NO | Mismo ID que `users` |
| `first_name` | VARCHAR(100) | SÍ | Nombre |
| `last_name` | VARCHAR(100) | SÍ | Apellidos |
| `phone` | VARCHAR(20) | SÍ | Teléfono fijo |
| `mobile` | VARCHAR(20) | SÍ | Teléfono móvil |
| `address` | VARCHAR(200) | SÍ | Dirección completa |
| `city` | VARCHAR(100) | SÍ | Ciudad |
| `postal_code` | VARCHAR(10) | SÍ | Código postal |
| `department` | VARCHAR(100) | SÍ | Departamento |
| `position` | VARCHAR(100) | SÍ | Cargo/Posición |
| `employee_type` | VARCHAR(50) | SÍ | Tipo de empleado |
| `hire_date` | DATE | SÍ | Fecha de contratación |
| `avatar_url` | VARCHAR(500) | SÍ | URL del avatar |
| `notes` | TEXT | SÍ | Notas adicionales |
| `created_at` | TIMESTAMP | NO | Fecha creación |
| `updated_at` | TIMESTAMP | NO | Fecha actualización |

### Tipos de Empleado (`employee_type`)

| Valor | Descripción |
|-------|-------------|
| `tecnico` | Técnico presencial |
| `tecnico_remoto` | Técnico remoto |
| `atencion_cliente` | Atención al cliente |
| `administrativo` | Personal administrativo |
| `manager` | Manager/Responsable |

---

## ?? API Endpoints

### Obtener mi perfil
```http
GET /api/v1/profiles/me
Authorization: Bearer {token}
```

**Respuesta:**
```json
{
  "id": "uuid",
  "first_name": "Francisco",
  "last_name": "Santos",
  "full_name": "Francisco Santos",
  "phone": "+34 900 000 000",
  "mobile": "+34 600 000 000",
  "address": "Calle Example 123",
  "city": "Madrid",
  "postal_code": "28001",
  "department": "Tecnologia",
  "position": "Administrador de Sistemas",
  "employee_type": "administrativo",
  "hire_date": "2020-01-15",
  "avatar_url": "https://example.com/avatar.jpg",
  "notes": "Notas adicionales",
  "created_at": "2024-01-01T00:00:00Z",
  "updated_at": "2024-01-17T10:00:00Z"
}
```

### Actualizar mi perfil
```http
PUT /api/v1/profiles/me
Authorization: Bearer {token}
Content-Type: application/json

{
  "first_name": "Francisco",
  "last_name": "Santos",
  "phone": "+34 900 000 000",
  "mobile": "+34 600 000 000",
  "address": "Calle Example 123",
  "city": "Madrid",
  "postal_code": "28001",
  "department": "Tecnologia",
  "position": "Administrador de Sistemas",
  "employee_type": "administrativo",
  "hire_date": "2020-01-15",
  "avatar_url": "https://example.com/avatar.jpg",
  "notes": "Notas adicionales"
}
```

**Respuesta:**
```json
{
  "message": "Perfil actualizado correctamente"
}
```

### Obtener perfil de otro usuario (ADMIN)
```http
GET /api/v1/profiles/{userId}
Authorization: Bearer {token}
```

### Actualizar perfil de otro usuario (ADMIN)
```http
PUT /api/v1/profiles/{userId}
Authorization: Bearer {token}
Content-Type: application/json

{
  "first_name": "Juan",
  "last_name": "Pérez",
  "employee_type": "tecnico",
  ...
}
```

### Listar todos los perfiles (ADMIN)
```http
GET /api/v1/profiles
GET /api/v1/profiles?employee_type=tecnico
GET /api/v1/profiles?department=Tecnologia
Authorization: Bearer {token}
```

**Respuesta:**
```json
[
  {
    "id": "uuid",
    "email": "user@example.com",
    "first_name": "Juan",
    "last_name": "Pérez",
    "full_name": "Juan Pérez",
    "phone": "+34 900 000 001",
    "mobile": "+34 600 000 001",
    "department": "Tecnologia",
    "position": "Técnico",
    "employee_type": "tecnico",
    "hire_date": "2021-03-10",
    "enabled": true
  }
]
```

---

## ?? Migración de Base de Datos

Ejecuta el script SQL en pgAdmin:

```
C:\GestionTime\src\GestionTime.Infrastructure\Migrations\SQL\002_CreateUserProfiles.sql
```

**Pasos:**
1. Abre pgAdmin
2. Conecta a `gestiontime`
3. Query Tool ? Abrir archivo SQL
4. Ejecutar el script completo

---

## ?? Uso en el Código

### Crear perfil al registrar usuario

```csharp
// Después de crear el usuario
var profile = new UserProfile
{
    Id = newUser.Id,
    FirstName = "Juan",
    LastName = "Pérez",
    EmployeeType = "tecnico",
    Department = "Tecnologia",
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow
};
db.UserProfiles.Add(profile);
await db.SaveChangesAsync();
```

### Consultar usuario con perfil

```csharp
var user = await db.Users
    .Include(u => u.Profile)
    .FirstOrDefaultAsync(u => u.Id == userId);

if (user?.Profile is not null)
{
    var fullName = user.Profile.FullName;
    var isTech = user.Profile.IsTechnician;
}
```

---

## ?? Casos de Uso

### 1. Crear usuario nuevo con perfil
Al crear un usuario, automáticamente se crea su perfil con datos básicos.

### 2. Usuario edita su perfil
Desde la aplicación desktop, el usuario puede actualizar sus datos personales y de contacto.

### 3. Admin gestiona perfiles
El administrador puede ver y editar los perfiles de todos los usuarios.

### 4. Filtrar técnicos
Listar solo usuarios con `employee_type = 'tecnico'` o `'tecnico_remoto'`.

### 5. Organigrama
Consultar usuarios por departamento para generar organigramas.

---

## ?? Integración con el Frontend

### Desktop (WPF)

Crear un `ProfilePage.xaml` con:

**Sección Personal:**
- Nombre
- Apellidos
- Teléfono
- Móvil

**Sección Dirección:**
- Dirección
- Ciudad
- Código Postal

**Sección Laboral:**
- Departamento
- Cargo
- Tipo de empleado (ComboBox)
- Fecha de contratación

**Sección Otros:**
- Avatar (FileDialog para subir)
- Notas

---

## ?? Validaciones

### Backend
- ? El ID del perfil debe coincidir con un usuario existente
- ? Los campos de texto tienen longitud máxima
- ? `employee_type` debe ser uno de los valores válidos (validar en frontend)
- ? `hire_date` no puede ser futura

### Frontend (recomendado)
- Validar formato de teléfono
- Validar código postal
- Validar URL del avatar
- Validar longitud de textos

---

## ?? Seguridad

- ? Un usuario solo puede ver/editar su propio perfil
- ? Solo ADMINs pueden ver/editar perfiles de otros usuarios
- ? La eliminación de un usuario elimina automáticamente su perfil (CASCADE)

---

## ?? Ejemplo Completo

```csharp
// Crear usuario con perfil
var user = new User
{
    Email = "jtecnico@example.com",
    FullName = "Juan Técnico",
    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
    Enabled = true
};
db.Users.Add(user);

var profile = new UserProfile
{
    Id = user.Id,
    FirstName = "Juan",
    LastName = "Técnico",
    Mobile = "+34 600 111 222",
    Department = "Soporte Técnico",
    Position = "Técnico de Campo",
    EmployeeType = "tecnico",
    HireDate = new DateTime(2023, 6, 1),
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow
};
db.UserProfiles.Add(profile);

await db.SaveChangesAsync();
```

---

## ?? Próximos Pasos

1. ? Ejecutar migración SQL `002_CreateUserProfiles.sql`
2. ? Reiniciar la API
3. ? Probar endpoints en Swagger
4. ? Implementar UI en desktop para editar perfil
5. ? Añadir validaciones en frontend
6. ? Implementar subida de avatar (futuro)
