namespace GestionTime.Api.Contracts.Admin;

/// <summary>Filtros para GET /api/v2/admin/desktop-client-health.</summary>
public sealed class DesktopClientHealthQuery
{
    public Guid? AgentId { get; set; }
    public string? Q { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
