namespace GestionTime.Domain.Auth;

public sealed class UserProfile
{
    // Relación 1:1 con User (mismo ID)
    public Guid Id { get; set; }

    // Datos personales
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }

    // Dirección
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }

    // Información laboral
    public string? Department { get; set; }
    public string? Position { get; set; }
    
    // Tipo de empleado: tecnico, tecnico_remoto, atencion_cliente, administrativo, manager
    public string? EmployeeType { get; set; }
    
    public DateTime? HireDate { get; set; }

    // Otros
    public string? AvatarUrl { get; set; }
    public string? Notes { get; set; }

    // Auditoría
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navegación
    public User User { get; set; } = null!;

    // Propiedades calculadas
    public string FullName => string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName)
        ? "Sin nombre"
        : $"{FirstName} {LastName}".Trim();

    public bool IsTechnician => EmployeeType?.ToLowerInvariant() is "tecnico" or "tecnico_remoto";
}
