namespace GestionTime.Api.Contracts.Admin;

/// <summary>Item de salud de un agente/usuario Desktop.</summary>
public sealed class DesktopClientHealthItemDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? CurrentVersion { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public string Status { get; set; } = "OK";
    public string? MachineName { get; set; }
    public string? OsVersion { get; set; }
    public string? UpdateUrl { get; set; }
}

/// <summary>Respuesta paginada de salud de clientes Desktop.</summary>
public sealed class DesktopClientHealthResponse
{
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? LatestVersion { get; set; }
    public string? MinVersion { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<DesktopClientHealthItemDto> Items { get; set; } = new();
}
