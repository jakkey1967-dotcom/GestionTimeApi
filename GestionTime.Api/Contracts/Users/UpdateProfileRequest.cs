namespace GestionTime.Api.Contracts.Users;

public sealed record UpdateProfileRequest(
    string? first_name,
    string? last_name,
    string? phone,
    string? mobile,
    string? address,
    string? city,
    string? postal_code,
    string? department,
    string? position,
    string? employee_type, // tecnico, tecnico_remoto, atencion_cliente, administrativo, manager
    DateTime? hire_date,
    string? avatar_url,
    string? notes
);
