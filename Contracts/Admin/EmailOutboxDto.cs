namespace GestionTime.Api.Contracts.Admin;

/// <summary>Item del histórico de emails enviados a un usuario.</summary>
public sealed class EmailOutboxItemDto
{
    public long Id { get; set; }
    public string Kind { get; set; } = "";
    public string Platform { get; set; } = "";
    public string? TargetVersionRaw { get; set; }
    public string PeriodKey { get; set; } = "";
    public string? Subject { get; set; }
    public string? BodyPreview { get; set; }
    public string Status { get; set; } = "";
    public DateTimeOffset? SentAt { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>Filtros para histórico de emails.</summary>
public sealed class EmailOutboxQuery
{
    public Guid UserId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
