namespace GestionTime.Domain.Auth;

/// <summary>
/// Sesión activa de un usuario (para tracking de presencia online)
/// </summary>
public class UserSession
{
    /// <summary>Session ID único (incluido en JWT claim "sid")</summary>
    public Guid Id { get; set; }

    /// <summary>Usuario dueño de la sesión</summary>
    public Guid UserId { get; set; }

    /// <summary>Fecha de creación de la sesión</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Última actividad registrada</summary>
    public DateTime LastSeenAt { get; set; }

    /// <summary>Fecha de revocación (null = sesión activa)</summary>
    public DateTime? RevokedAt { get; set; }

    // Información opcional del dispositivo
    public string? DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public string? Ip { get; set; }
    public string? UserAgent { get; set; }

    // Navegación
    public User User { get; set; } = null!;

    /// <summary>Indica si la sesión está activa (no revocada)</summary>
    public bool IsActive => RevokedAt == null;
}
