namespace GestionTime.Api.Contracts.Users;

/// <summary>Información de un usuario con sus roles.</summary>
public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public bool Enabled { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool MustChangePassword { get; set; }
    public List<string> Roles { get; set; } = new();
}

/// <summary>Información de un rol del sistema.</summary>
public class RoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

/// <summary>Request para actualizar roles de un usuario.</summary>
public class UpdateUserRolesRequest
{
    public List<string> Roles { get; set; } = new();
}

/// <summary>Request para habilitar/deshabilitar un usuario.</summary>
public class UpdateUserEnabledRequest
{
    public bool Enabled { get; set; }
}
