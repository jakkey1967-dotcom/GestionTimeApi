# 👥 API de Gestión de Usuarios y Roles

## 📋 Descripción
API para que los administradores puedan gestionar usuarios y sus roles desde GestionTime Desktop.

---

## 🔐 Autenticación
Todos los endpoints requieren:
- **Header:** `Authorization: Bearer {token}`
- **Rol requerido:** `Admin` o `ADMIN`

---

## 📡 Endpoints

### 1. Listar Usuarios
**GET** `/api/v1/users`

**Query Parameters:**
- `page` (opcional, default: 1) - Número de página
- `pageSize` (opcional, default: 50, max: 100) - Registros por página

**Respuesta:**
```json
{
  "total": 25,
  "page": 1,
  "pageSize": 50,
  "totalPages": 1,
  "users": [
    {
      "id": "guid",
      "email": "user@example.com",
      "fullName": "Juan Pérez",
      "enabled": true,
      "emailConfirmed": true,
      "mustChangePassword": false,
      "roles": ["USER", "ADMIN"]
    }
  ]
}
```

---

### 2. Obtener Usuario por ID
**GET** `/api/v1/users/{id}`

**Respuesta:**
```json
{
  "id": "guid",
  "email": "user@example.com",
  "fullName": "Juan Pérez",
  "enabled": true,
  "emailConfirmed": true,
  "mustChangePassword": false,
  "roles": ["USER"]
}
```

---

### 3. Listar Roles Disponibles
**GET** `/api/v1/roles`

**Respuesta:**
```json
{
  "roles": [
    { "id": "guid", "name": "ADMIN" },
    { "id": "guid", "name": "USER" }
  ]
}
```

---

### 4. Actualizar Roles de Usuario
**PUT** `/api/v1/users/{id}/roles`

**Body:**
```json
{
  "roles": ["USER", "ADMIN"]
}
```

**Respuesta:**
```json
{
  "message": "Roles actualizados exitosamente",
  "userId": "guid",
  "email": "user@example.com",
  "roles": ["USER", "ADMIN"]
}
```

**Errores:**
- `400 Bad Request` - Si intentas modificar tus propios roles
- `400 Bad Request` - Si incluyes roles inválidos
- `404 Not Found` - Si el usuario no existe

---

### 5. Habilitar/Deshabilitar Usuario
**PUT** `/api/v1/users/{id}/enabled`

**Body:**
```json
{
  "enabled": false
}
```

**Respuesta:**
```json
{
  "message": "Usuario deshabilitado",
  "userId": "guid",
  "email": "user@example.com",
  "enabled": false
}
```

**Errores:**
- `400 Bad Request` - Si intentas deshabilitarte a ti mismo
- `404 Not Found` - Si el usuario no existe

---

## 🧪 Pruebas

### Ejecutar script de prueba:
```powershell
.\scripts\test-users-management.ps1
```

### Prueba manual con PowerShell:
```powershell
# 1. Login
$loginBody = @{
    email = "admin@example.com"
    password = "password"
} | ConvertTo-Json

$login = Invoke-RestMethod `
    -Uri "https://localhost:2502/api/v1/auth/login-desktop" `
    -Method POST `
    -ContentType "application/json" `
    -Body $loginBody

$headers = @{
    "Authorization" = "Bearer $($login.accessToken)"
    "Content-Type" = "application/json"
}

# 2. Listar usuarios
$users = Invoke-RestMethod `
    -Uri "https://localhost:2502/api/v1/users" `
    -Method GET `
    -Headers $headers

# 3. Ver roles disponibles
$roles = Invoke-RestMethod `
    -Uri "https://localhost:2502/api/v1/roles" `
    -Method GET `
    -Headers $headers

# 4. Actualizar roles
$updateBody = @{ roles = @("USER", "ADMIN") } | ConvertTo-Json
Invoke-RestMethod `
    -Uri "https://localhost:2502/api/v1/users/{userId}/roles" `
    -Method PUT `
    -Headers $headers `
    -Body $updateBody
```

---

## 🛡️ Protecciones de Seguridad

1. **No puedes modificar tus propios roles** - Evita que un admin se quite permisos accidentalmente
2. **No puedes deshabilitarte a ti mismo** - Evita quedar bloqueado del sistema
3. **Validación de roles** - Solo se pueden asignar roles que existan en el sistema
4. **Solo Admin** - Todos los endpoints requieren rol de administrador

---

## 📱 Implementación en Desktop (WPF)

### Ejemplo de flujo en GestionTime Desktop:

```csharp
// 1. Obtener lista de usuarios
var users = await _apiService.GetUsersAsync(page: 1, pageSize: 50);

// 2. Mostrar en DataGrid
UsersDataGrid.ItemsSource = users.Users;

// 3. Al seleccionar usuario, mostrar roles
var selectedUser = UsersDataGrid.SelectedItem as UserDto;
var roles = await _apiService.GetRolesAsync();
RolesComboBox.ItemsSource = roles.Roles;

// 4. Actualizar roles
await _apiService.UpdateUserRolesAsync(
    userId: selectedUser.Id,
    roles: new[] { "USER", "ADMIN" }
);

// 5. Habilitar/deshabilitar
await _apiService.UpdateUserEnabledAsync(
    userId: selectedUser.Id,
    enabled: false
);
```

---

## 📝 Logs

Los logs registran todas las operaciones:
```
[INFO] Admin admin@example.com consultando lista de usuarios
[INFO] Admin admin@example.com actualizando roles de usuario {userId} a: USER, ADMIN
[WARN] Admin admin@example.com intentó modificar sus propios roles
[INFO] ✅ Roles actualizados exitosamente para usuario user@example.com
```

---

## 🔄 Próximos pasos para Desktop

1. Crear ventana de gestión de usuarios (`UsersManagementWindow.xaml`)
2. Implementar servicio API (`UsersManagementApiService.cs`)
3. Agregar opción en menú Admin
4. Implementar búsqueda/filtrado de usuarios
5. Agregar confirmación antes de cambios críticos

---

**Fecha de creación:** 2025-01-27  
**Autor:** Sistema GestionTime  
**Versión:** 1.0
