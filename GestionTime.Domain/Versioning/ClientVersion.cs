using GestionTime.Domain.Auth;

namespace GestionTime.Domain.Versioning;

public sealed class ClientVersion
{
    public long Id { get; set; }
    public Guid UserId { get; set; }
    public string Platform { get; set; } = "Desktop";
    public string AppVersionRaw { get; set; } = "";
    public int VerMajor { get; set; }
    public int VerMinor { get; set; }
    public int VerPatch { get; set; }
    public string? VerPrerelease { get; set; }
    public string? OsVersion { get; set; }
    public string? MachineName { get; set; }
    public DateTimeOffset LoggedAt { get; set; } = DateTimeOffset.UtcNow;

    public User? User { get; set; }
}
