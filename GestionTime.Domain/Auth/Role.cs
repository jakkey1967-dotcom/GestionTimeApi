namespace GestionTime.Domain.Auth;

public sealed class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = ""; // ADMIN, MANAGER, USER

    public List<UserRole> UserRoles { get; set; } = new();
}
