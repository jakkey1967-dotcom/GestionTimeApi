namespace GestionTime.Api.Contracts.ClientVersion;

public sealed class RegisterVersionResponse
{
    public bool Ok { get; set; }
    public bool UpdateRequired { get; set; }
    public bool UpdateAvailable { get; set; }
    public string MinRequiredVersion { get; set; } = "";
    public string? LatestVersion { get; set; }
    public string? UpdateUrl { get; set; }
    public string? Message { get; set; }
}
