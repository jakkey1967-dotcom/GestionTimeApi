namespace GestionTime.Domain.Versioning;

/// <summary>Fila de la vista v_desktop_client_last_version (última versión Desktop por usuario).</summary>
public sealed class VDesktopClientLastVersion
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string AppVersionRaw { get; set; } = "";
    public string Platform { get; set; } = "Desktop";
    public int VerMajor { get; set; }
    public int VerMinor { get; set; }
    public int VerPatch { get; set; }
    public string? VerPrerelease { get; set; }
    public string? OsVersion { get; set; }
    public string? MachineName { get; set; }
    public DateTimeOffset LoggedAt { get; set; }
}
