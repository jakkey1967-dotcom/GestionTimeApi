namespace GestionTime.Domain.Work;

/// <summary>Nota asociada a un cliente: global (owner_user_id=null) o personal (por usuario).</summary>
public sealed class ClienteNota
{
    public Guid Id { get; set; }
    public int ClienteId { get; set; }

    /// <summary>NULL = nota global; NOT NULL = nota personal del usuario.</summary>
    public Guid? OwnerUserId { get; set; }

    public string Nota { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Navigation
    public Cliente? Cliente { get; set; }
}
