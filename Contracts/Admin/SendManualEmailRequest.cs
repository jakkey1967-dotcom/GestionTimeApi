using System.ComponentModel.DataAnnotations;

namespace GestionTime.Api.Contracts.Admin;

/// <summary>Solicitud de envío manual del correo de novedades Desktop.</summary>
public sealed class SendManualEmailRequest
{
    /// <summary>Email del destinatario.</summary>
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    /// <summary>Nombre completo del destinatario.</summary>
    [Required, MinLength(2)]
    public string FullName { get; set; } = "";
}
