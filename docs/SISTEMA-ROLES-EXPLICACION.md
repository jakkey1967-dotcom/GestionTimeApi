# 🔍 SISTEMA DE ROLES - GestionTime API

## 📊 Estructura de Base de Datos

### Tablas Involucradas:

1. **`pss_dvnx.users`** - Tabla principal de usuarios
   - `id` (UUID) - ID del usuario
   - `email` (VARCHAR) - Email único
   - `full_name` (VARCHAR) - Nombre completo
   - `password_hash` (TEXT) - Contraseña hasheada con BCrypt
   - `enabled` (BOOLEAN) - Usuario activo/inactivo

2. **`pss_dvnx.roles`** - Catálogo de roles
   - `id` (INT) - ID del rol
   - `name` (VARCHAR) - Nombre del rol (ADMIN, EDITOR, USER)

3. **`pss_dvnx.user_roles`** - Relación muchos a muchos (un usuario puede tener múltiples roles)
   - `user_id` (UUID FK) - ID del usuario
   - `role_id` (INT FK) - ID del rol

---

## 🎭 Roles Disponibles

| ID | Nombre | Descripción | Permisos |
|----|--------|-------------|----------|
| 1  | ADMIN  | Administrador del sistema | Acceso completo, puede ver `/api/v1/admin/users` |
| 2  | EDITOR | Supervisor/Editor | Puede editar contenido y ver usuarios |
| 3  | USER   | Usuario estándar | Solo lectura |

---

## 🔧 Endpoint para Cambiar Roles

### **PUT `/api/v1/admin/users/{id:guid}/roles`**

**Controlador:** `AdminUsersController.cs`

**Request Body:**
```json
{
  "roles": ["ADMIN"]
}
```

**Comportamiento:**
1. Elimina TODOS los roles actuales del usuario
2. Asigna los nuevos roles especificados
3. Si `roles` está vacío o no se envía, asigna rol `USER` por defecto

**Código relevante:**
```csharp
[HttpPut("{id:guid}/roles")]
public async Task<IActionResult> SetRoles([FromRoute] Guid id, [FromBody] SetRolesRequest req)
{
    // 1. Buscar usuario
    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
    
    // 2. Validar roles
    var roleNames = (req.Roles ?? Array.Empty<string>())
        .Select(x => x.Trim().ToUpper())
        .Distinct()
        .ToArray();
    
    if (roleNames.Length == 0)
        roleNames = new[] { "USER" };
    
    // 3. Eliminar roles actuales
    var current = await db.UserRoles.Where(ur => ur.UserId == id).ToListAsync();
    db.UserRoles.RemoveRange(current);
    
    // 4. Asignar nuevos roles
    foreach (var r in roles)
        db.UserRoles.Add(new UserRole { UserId = id, RoleId = r.Id });
    
    await db.SaveChangesAsync();
}
```

---

## ✅ SOLUCIÓN PARA psantos@global-retail.com

### **Opción 1: SQL Directo (RECOMENDADO - Más Rápido)**

Ejecutar el archivo que creé:
```
C:\GestionTime\GestionTimeApi\docs\SQL-ChangePSantosToAdmin.sql
```

O copiar y ejecutar en PostgreSQL:

```sql
SET search_path TO pss_dvnx;

DO $$
DECLARE
    v_user_id UUID;
    v_admin_role_id INT;
BEGIN
    -- Buscar usuario
    SELECT id INTO v_user_id FROM pss_dvnx.users WHERE email = 'psantos@global-retail.com';
    
    -- Buscar rol ADMIN
    SELECT id INTO v_admin_role_id FROM pss_dvnx.roles WHERE name = 'ADMIN';
    
    -- Eliminar roles actuales
    DELETE FROM pss_dvnx.user_roles WHERE user_id = v_user_id;
    
    -- Asignar ADMIN
    INSERT INTO pss_dvnx.user_roles (user_id, role_id) VALUES (v_user_id, v_admin_role_id);
    
    RAISE NOTICE '✅ Rol ADMIN asignado a psantos@global-retail.com';
END $$;
```

---

### **Opción 2: API REST (requiere ya ser ADMIN)**

```powershell
# 1. Login como admin
$login = Invoke-RestMethod -Uri "https://gestiontimeapi.onrender.com/api/v1/auth/login-desktop" `
    -Method Post `
    -Body '{"email":"admin@admin.com","password":"Admin@2025"}' `
    -ContentType "application/json"

$token = $login.AccessToken

# 2. Obtener ID de psantos
$users = Invoke-RestMethod -Uri "https://gestiontimeapi.onrender.com/api/v1/admin/users" `
    -Headers @{Authorization="Bearer $token"} `
    -Method Get

$psantos = $users | Where-Object { $_.email -eq "psantos@global-retail.com" }
$userId = $psantos.id

# 3. Cambiar rol a ADMIN
Invoke-RestMethod -Uri "https://gestiontimeapi.onrender.com/api/v1/admin/users/$userId/roles" `
    -Headers @{Authorization="Bearer $token"} `
    -Method Put `
    -Body '{"roles":["ADMIN"]}' `
    -ContentType "application/json"
```

---

## ⚠️ IMPORTANTE DESPUÉS DEL CAMBIO

1. **psantos@global-retail.com DEBE cerrar sesión** en GestionTime Desktop
2. **Volver a hacer login** para obtener un nuevo token JWT
3. El nuevo token incluirá el claim con rol `ADMIN`
4. Ahora podrá ver la ventana de usuarios online y acceder a `/api/v1/admin/users`

---

## 🔍 Verificación

### SQL:
```sql
SELECT u.email, STRING_AGG(r.name, ', ') AS roles
FROM pss_dvnx.users u
JOIN pss_dvnx.user_roles ur ON u.id = ur.user_id
JOIN pss_dvnx.roles r ON ur.role_id = r.id
WHERE u.email = 'psantos@global-retail.com'
GROUP BY u.email;
```

**Resultado esperado:**
```
email: psantos@global-retail.com
roles: ADMIN
```

### Logs de la aplicación Desktop:
```
[INFO] Usuario: psantos@global-retail.com, Rol: ADMIN
[INFO] 📂 UsersOnlineWindow creada
[INFO] 🌐 Cargando usuarios desde API GET /api/v1/admin/users...
[INFO] ✅ Usuarios cargados: X usuarios
```

---

## 📝 Notas Técnicas

- **Relación:** Un usuario puede tener múltiples roles (muchos a muchos)
- **Hash de contraseña:** BCrypt.Net
- **Schema:** `pss_dvnx` (configurable en `appsettings.json`)
- **Validación:** El endpoint valida que los roles existan antes de asignarlos
- **Transacciones:** Usa transacciones para garantizar consistencia

---

**Creado:** 2025-01-21  
**Proyecto:** GestionTime API v1.5.0-beta
