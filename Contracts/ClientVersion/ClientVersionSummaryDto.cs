namespace GestionTime.Api.Contracts.ClientVersion;

public sealed class ClientVersionSummaryDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string AppVersionRaw { get; set; } = "";
    public string Platform { get; set; } = "";
    public string? OsVersion { get; set; }
    public string? MachineName { get; set; }
    public DateTimeOffset LoggedAt { get; set; }
}
