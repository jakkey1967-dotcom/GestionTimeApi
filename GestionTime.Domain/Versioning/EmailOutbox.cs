using GestionTime.Domain.Auth;

namespace GestionTime.Domain.Versioning;

/// <summary>Registro de emails enviados a usuarios (outbox pattern con deduplicación semanal).</summary>
public sealed class EmailOutbox
{
    public long Id { get; set; }
    public Guid UserId { get; set; }
    public string Kind { get; set; } = "";
    public string Platform { get; set; } = "Desktop";
    public string? TargetVersionRaw { get; set; }
    public string PeriodKey { get; set; } = "";
    public string DedupeKey { get; set; } = "";
    public string? Subject { get; set; }
    public string? BodyPreview { get; set; }
    public string Status { get; set; } = "PENDING";
    public DateTimeOffset? SentAt { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public User? User { get; set; }
}
