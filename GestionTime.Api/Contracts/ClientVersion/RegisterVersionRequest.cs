using System.ComponentModel.DataAnnotations;

namespace GestionTime.Api.Contracts.ClientVersion;

public sealed class RegisterVersionRequest
{
    [Required]
    [MaxLength(50)]
    public string AppVersion { get; set; } = "";

    [MaxLength(20)]
    public string Platform { get; set; } = "Desktop";

    [MaxLength(100)]
    public string? OsVersion { get; set; }

    [MaxLength(100)]
    public string? MachineName { get; set; }
}
