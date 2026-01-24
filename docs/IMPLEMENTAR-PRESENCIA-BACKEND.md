# ═══════════════════════════════════════════════════════════════════════════════
# INSTRUCCIONES: Agregar Soporte de Presencia (Online/Offline) al Backend
# ═══════════════════════════════════════════════════════════════════════════════

## 📋 Pasos para Implementar

### **PASO 1: Ejecutar la migración SQL**

Ejecutar en PostgreSQL:
```bash
cd C:\GestionTime\GestionTimeApi
psql -h HOST -U USER -d DATABASE -f docs/SQL-Migration-AddLastSeenAt.sql
```

O copiar y pegar el contenido del archivo en pgAdmin/DBeaver.

---

### **PASO 2: Actualizar la entidad User.cs**

**Archivo:** `GestionTime.Domain\Auth\User.cs`

Agregar esta propiedad después de la línea 11:

```csharp
public DateTime? LastSeenAt { get; set; }
```

El código completo debería verse así:

```csharp
namespace GestionTime.Domain.Auth;

public sealed class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string FullName { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public bool EmailConfirmed { get; set; } = false;
    
    // ✅ NUEVO: Control de presencia (online/offline)
    public DateTime? LastSeenAt { get; set; }
    
    // Control de expiración de contraseñas
    public DateTime? PasswordChangedAt { get; set; }
    public bool MustChangePassword { get; set; } = false;
    public int PasswordExpirationDays { get; set; } = 90;

    public List<UserRole> UserRoles { get; set; } = new();
    public List<RefreshToken> RefreshTokens { get; set; } = new();
    
    public UserProfile? Profile { get; set; }
    
    // Propiedades calculadas
    public bool IsPasswordExpired => PasswordChangedAt.HasValue && 
        PasswordChangedAt.Value.AddDays(PasswordExpirationDays) < DateTime.UtcNow;
    
    public bool ShouldChangePassword => MustChangePassword || IsPasswordExpired;
    
    public int DaysUntilPasswordExpires => PasswordChangedAt.HasValue 
        ? Math.Max(0, PasswordExpirationDays - (DateTime.UtcNow - PasswordChangedAt.Value).Days)
        : 0;
    
    // ✅ NUEVO: Indica si el usuario está online (last_seen_at < 2 minutos)
    public bool IsOnline => LastSeenAt.HasValue && 
        LastSeenAt.Value >= DateTime.UtcNow.AddMinutes(-2);
}
```

---

### **PASO 3: Actualizar AdminUsersController.cs**

**Archivo:** `Controllers\AdminUsersController.cs`

**A) Actualizar el endpoint GET /api/v1/admin/users** para incluir `LastSeenAt`:

Buscar la línea ~62 donde está:
```csharp
.Select(u => new { u.Id, u.Email, u.FullName, u.Enabled })
```

Y reemplazarla por:
```csharp
.Select(u => new { u.Id, u.Email, u.FullName, u.Enabled, u.LastSeenAt })
```

Luego buscar la línea ~77 donde está:
```csharp
var result = users.Select(u => new
{
    u.Id,
    u.Email,
    u.FullName,
    u.Enabled,
    roles = rolesByUser.TryGetValue(u.Id, out var rr) ? rr : Array.Empty<string>()
});
```

Y reemplazarla por:
```csharp
var result = users.Select(u => new
{
    u.Id,
    u.Email,
    u.FullName,
    u.Enabled,
    u.LastSeenAt,
    roles = rolesByUser.TryGetValue(u.Id, out var rr) ? rr : Array.Empty<string>()
});
```

**B) Agregar endpoint GET /api/v1/admin/ping** al final del archivo:

```csharp
[HttpGet("ping")]
[AllowAnonymous] // Permitir sin autenticación o cambiar a [Authorize] si prefieres
public async Task<IActionResult> Ping()
{
    // Obtener el email del usuario autenticado desde el claim
    var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
    
    if (string.IsNullOrEmpty(userEmail))
    {
        return Ok(new { message = "Ping recibido (sin usuario autenticado)" });
    }

    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
    
    if (user == null)
    {
        return NotFound(new { message = "Usuario no encontrado" });
    }

    user.LastSeenAt = DateTime.UtcNow;
    await db.SaveChangesAsync();

    _logger.LogDebug("Ping recibido de {Email}, last_seen_at actualizado", userEmail);

    return Ok(new { message = "Ping registrado", lastSeenAt = user.LastSeenAt });
}
```

No olvides agregar el using al principio del archivo:
```csharp
using System.Security.Claims;
```

---

### **PASO 4: Actualizar el DbContext (opcional, si usas Fluent API)**

Si el proyecto usa configuración de Fluent API, agregar en `GestionTime.Infrastructure.Persistence.EntityConfigurations`:

```csharp
builder.Property(u => u.LastSeenAt)
    .HasColumnType("timestamp with time zone");

builder.HasIndex(u => u.LastSeenAt)
    .HasDatabaseName("idx_users_last_seen_at");
```

---

### **PASO 5: Probar los cambios**

1. **Reiniciar el backend** (dotnet run)
2. **Verificar que compile** sin errores
3. **Probar el endpoint de ping:**
```bash
curl -X GET "https://gestiontimeapi.onrender.com/api/v1/admin/ping" \
  -H "Authorization: Bearer TOKEN_AQUI"
```

4. **Verificar que el GET /api/v1/admin/users ahora devuelve `lastSeenAt`:**
```bash
curl -X GET "https://gestiontimeapi.onrender.com/api/v1/admin/users" \
  -H "Authorization: Bearer TOKEN_AQUI"
```

---

### **PASO 6: Actualizar el Frontend (Desktop)**

Ya está listo en el frontend, solo necesitas actualizar el DTO para recibir `LastSeenAt`:

**Archivo:** `Models/Dtos/UsersListResponse.cs` (línea ~15)

Agregar:
```csharp
[JsonPropertyName("lastSeenAt")]
public DateTime? LastSeenAt { get; set; }
```

Y cambiar la propiedad `IsOnline` (línea ~58):
```csharp
/// <summary>Indica si el usuario está online (last_seen_at menor a 2 minutos).</summary>
[JsonIgnore]
public bool IsOnline
{
    get
    {
        if (!Enabled || LastSeenAt == null)
            return false;

        var threshold = DateTime.UtcNow.AddMinutes(-2);
        return LastSeenAt.Value >= threshold;
    }
}
```

---

## ✅ Verificación Final

Después de todos los cambios:

1. **Backend devuelve:**
```json
{
  "id": "guid-aqui",
  "email": "user@example.com",
  "fullName": "Usuario Prueba",
  "enabled": true,
  "lastSeenAt": "2025-01-21T18:30:00Z",
  "roles": ["USER"]
}
```

2. **Frontend muestra:**
- ✅ Usuarios con círculo verde si están online (< 2 min)
- ✅ Usuarios con círculo gris si están offline (> 2 min)
- ✅ Se actualiza cada 15 segundos automáticamente

---

## 📝 Notas

- El umbral de "online" es **2 minutos** (configurable)
- El frontend hace polling cada **15 segundos** con caché
- El ping se envía cada **30 segundos** desde el frontend (implementar)
- Los usuarios sin `last_seen_at` (NULL) se muestran como offline

---

**Fecha:** 2025-01-21  
**Proyecto:** GestionTime API + Desktop v1.5.0-beta
