# 🖥️ ESTADO ACTUAL - Gestión de Usuarios en GestionTime Desktop

**Fecha de análisis:** 2025-01-28  
**Proyecto analizado:** `C:\GestionTime\GestionTimeDesktop`

---

## ✅ **LO QUE YA ESTÁ IMPLEMENTADO**

### 1️⃣ **AdminUsersService** - ✅ PARCIALMENTE COMPLETO

**Ubicación:** `Services/Admin/AdminUsersService.cs`

**Métodos existentes:**
```csharp
✅ UpdateUserRoleAsync(Guid userId, string newRole) 
   → Llama a: PUT /api/v1/admin/users/{userId}/roles
   → ⚠️ Endpoint viejo, debe usar: /api/v1/users/{userId}/roles

✅ GetAvailableRoles() 
   → Retorna: ["ADMIN", "EDITOR", "USER"]

✅ IsValidRole(string role)
   → Valida si un rol es válido
```

**Métodos FALTANTES que hay que agregar:**
```csharp
❌ GetUsersAsync(int page, int pageSize) - Listar usuarios paginados
❌ UpdateUserEnabledAsync(Guid userId, bool enabled) - Habilitar/deshabilitar
❌ GetRolesFromBackendAsync() - Obtener roles desde /api/v1/roles
```

---

### 2️⃣ **DTOs Existentes** - ✅ COMPLETO

#### `UserListItemDto` (en `Models/Dtos/UsersListResponse.cs`)
```csharp
✅ Id (Guid)
✅ Email (string)
✅ FullName (string)
✅ Enabled (bool)
✅ Roles (string[])
✅ LastSeenAt (DateTime?)
✅ IsOnline (computed)
✅ Role (computed - primer rol)
✅ RolePriority (computed - ADMIN=1, EDITOR=2, USER=3)
```

#### `UpdateUserRoleRequest` (en `Models/Dtos/UpdateUserRoleRequest.cs`)
```csharp
✅ Role (string) - UN solo rol
```

**DTOs FALTANTES que hay que crear:**
```csharp
❌ UpdateUserEnabledRequest { Enabled: bool }
❌ UsersPagedResult { Total, Page, PageSize, TotalPages, Users[] }
❌ RolesResponse { Roles[] }
❌ RoleItemDto { Id, Name }
```

---

### 3️⃣ **Vistas Existentes** - ✅ COMPLETO (Para Presencia)

#### `UsersOnlineWindow.xaml` - Vista de Presencia Online/Offline
```csharp
✅ Muestra usuarios agrupados por rol (ADMIN, EDITOR, USER)
✅ Polling automático cada 15 segundos
✅ Muestra estado online/offline
✅ Botón de refresh manual con animación
⚠️ FALTA: Botón KICK para expulsar usuarios
```

#### `UsersOnlineViewModel.cs`
```csharp
✅ Maneja agrupación de usuarios por rol
✅ Polling automático con timer
✅ Actualización de estado online/offline
✅ ObservableCollection<UserRoleGroup>
```

---

## ❌ **LO QUE NO EXISTE Y HAY QUE CREAR**

### 🆕 **Ventana de Administración de Usuarios (NUEVA)**

**Archivos que NO existen:**
```
Views/
  └── Admin/
      └── UsersManagementWindow.xaml         ❌ NO EXISTE
      └── UsersManagementWindow.xaml.cs      ❌ NO EXISTE

ViewModels/
  └── Admin/
      └── UsersManagementViewModel.cs        ❌ NO EXISTE
```

**Funcionalidad requerida:**
- ❌ Listar TODOS los usuarios (no solo online/offline)
- ❌ DataGrid con usuarios paginados
- ❌ Panel lateral para editar usuario seleccionado
- ❌ Checkboxes para asignar roles
- ❌ Botón "Guardar Roles"
- ❌ Botón "Habilitar/Deshabilitar Usuario"
- ❌ Paginación (Anterior/Siguiente)
- ❌ Status bar con mensajes de éxito/error

---

## 🔧 **CORRECCIONES NECESARIAS**

### 1️⃣ Actualizar Endpoint en `AdminUsersService.cs`

**Problema:** Usa endpoint viejo  
**Solución:**

```csharp
// ❌ ANTES (INCORRECTO)
$"/api/v1/admin/users/{userId}/roles"

// ✅ DESPUÉS (CORRECTO)
$"/api/v1/users/{userId}/roles"
```

---

### 2️⃣ Agregar Métodos Faltantes en `AdminUsersService.cs`

```csharp
/// <summary>Obtiene la lista paginada de usuarios.</summary>
public async Task<UsersPagedResult?> GetUsersAsync(int page = 1, int pageSize = 50, CancellationToken ct = default)
{
    try
    {
        _log?.LogInformation("📋 Obteniendo usuarios (página {page})...", page);

        var response = await App.Api.GetAsync<UsersPagedResult>(
            $"/api/v1/users?page={page}&pageSize={pageSize}",
            ct
        );

        return response;
    }
    catch (Exception ex)
    {
        _log?.LogError(ex, "❌ Error al obtener usuarios");
        return null;
    }
}

/// <summary>Habilita o deshabilita un usuario.</summary>
public async Task<bool> UpdateUserEnabledAsync(Guid userId, bool enabled, CancellationToken ct = default)
{
    try
    {
        var request = new UpdateUserEnabledRequest { Enabled = enabled };
        
        var response = await App.Api.PutAsync<UpdateUserEnabledRequest, object>(
            $"/api/v1/users/{userId}/enabled",
            request,
            ct
        );

        if (response != null)
        {
            PresenceService.Instance.ClearCache();
            return true;
        }

        return false;
    }
    catch (Exception ex)
    {
        _log?.LogError(ex, "❌ Error al actualizar estado del usuario");
        return false;
    }
}
```

---

### 3️⃣ Crear DTOs Faltantes en `Models/Dtos/`

```csharp
// UpdateUserEnabledRequest.cs
public sealed class UpdateUserEnabledRequest
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }
}

// UsersPagedResult.cs
public sealed class UsersPagedResult
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }

    [JsonPropertyName("users")]
    public List<UserListItemDto> Users { get; set; } = new();
}

// RolesResponse.cs
public sealed class RolesResponse
{
    [JsonPropertyName("roles")]
    public List<RoleItemDto> Roles { get; set; } = new();
}

public sealed class RoleItemDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
```

---

## 🚀 **PLAN DE IMPLEMENTACIÓN**

### **Paso 1:** Corregir `AdminUsersService.cs` ⚠️
1. Cambiar endpoint de `/api/v1/admin/users/{id}/roles` a `/api/v1/users/{id}/roles`
2. Agregar método `GetUsersAsync()`
3. Agregar método `UpdateUserEnabledAsync()`

### **Paso 2:** Crear DTOs Faltantes 📦
1. `UpdateUserEnabledRequest.cs`
2. `UsersPagedResult.cs`
3. `RolesResponse.cs` y `RoleItemDto.cs`

### **Paso 3:** Crear ViewModel 🎨
1. Crear carpeta `ViewModels/Admin/`
2. Crear `UsersManagementViewModel.cs`
   - Propiedades: `Users`, `SelectedUser`, `AvailableRoles`, `IsLoading`, `StatusMessage`
   - Métodos: `LoadDataAsync()`, `SaveUserRolesAsync()`, `ToggleUserEnabledAsync()`

### **Paso 4:** Crear Vista XAML 🖼️
1. Crear carpeta `Views/Admin/`
2. Crear `UsersManagementWindow.xaml`
   - Grid con 3 columnas: Lista usuarios | Separador | Panel edición
   - DataGrid para usuarios
   - Checkboxes para roles
   - Botones de acción

### **Paso 5:** Agregar al Menú Principal 🔗
1. En `MainWindow.xaml` agregar opción de menú "Administrar Usuarios" (solo ADMIN)
2. Abrir `UsersManagementWindow` al hacer click

---

## 📚 **ENDPOINTS DEL BACKEND (YA DISPONIBLES)**

| Método | Endpoint | Estado Backend |
|--------|----------|----------------|
| GET | `/api/v1/users?page={page}&pageSize={pageSize}` | ✅ FUNCIONA |
| GET | `/api/v1/users/{id}` | ✅ FUNCIONA |
| GET | `/api/v1/roles` | ✅ FUNCIONA |
| PUT | `/api/v1/users/{id}/roles` | ✅ FUNCIONA |
| PUT | `/api/v1/users/{id}/enabled` | ✅ FUNCIONA |

---

## ✅ **CHECKLIST DE IMPLEMENTACIÓN**

### AdminUsersService.cs
- [ ] Corregir endpoint `/api/v1/admin/users/{id}/roles` → `/api/v1/users/{id}/roles`
- [ ] Agregar `GetUsersAsync(int page, int pageSize)`
- [ ] Agregar `UpdateUserEnabledAsync(Guid userId, bool enabled)`

### DTOs
- [ ] Crear `UpdateUserEnabledRequest.cs`
- [ ] Crear `UsersPagedResult.cs`
- [ ] Crear `RolesResponse.cs`
- [ ] Crear `RoleItemDto.cs`

### ViewModel
- [ ] Crear carpeta `ViewModels/Admin/`
- [ ] Crear `UsersManagementViewModel.cs`
- [ ] Implementar `LoadDataAsync()`
- [ ] Implementar `SaveUserRolesAsync()`
- [ ] Implementar `ToggleUserEnabledAsync()`
- [ ] Implementar paginación

### Vista XAML
- [ ] Crear carpeta `Views/Admin/`
- [ ] Crear `UsersManagementWindow.xaml`
- [ ] Implementar DataGrid de usuarios
- [ ] Implementar panel de edición lateral
- [ ] Implementar checkboxes de roles
- [ ] Implementar botones de acción
- [ ] Implementar paginación

### Integración
- [ ] Agregar opción de menú en `MainWindow.xaml`
- [ ] Validar que solo ADMIN pueda acceder
- [ ] Probar flujo completo

---

## 🔗 **REFERENCIAS**

- **Script de prueba backend:** `scripts/test-users-management.ps1`
- **Documentación API:** `docs/USERS_MANAGEMENT_API.md`
- **Vista de presencia existente:** `Views/UsersOnlineWindow.xaml`
- **ViewModel de presencia existente:** `ViewModels/UsersOnlineViewModel.cs`

---

**Próximos pasos:** Implementar ventana nueva de administración de usuarios siguiendo este análisis.
