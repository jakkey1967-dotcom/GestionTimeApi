using System.ComponentModel.DataAnnotations;

namespace GestionTime.Api.Contracts.Admin;

/// <summary>Solicitud de envío manual del correo de novedades Desktop a uno o varios destinatarios.</summary>
public sealed class SendManualEmailRequest
{
    /// <summary>Lista de destinatarios (mínimo uno).</summary>
    [Required, MinLength(1)]
    public List<ManualEmailRecipient> Recipients { get; set; } = new();
}

/// <summary>Destinatario individual para envío manual.</summary>
public sealed class ManualEmailRecipient
{
    /// <summary>Email del destinatario.</summary>
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    /// <summary>Nombre completo del destinatario.</summary>
    [Required, MinLength(2)]
    public string FullName { get; set; } = "";
}
