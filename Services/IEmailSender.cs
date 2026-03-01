namespace GestionTime.Api.Services;

/// <summary>Envío genérico de emails (usado por campañas y otros servicios).</summary>
public interface IEmailSender
{
    /// <summary>Envía un email HTML sin plantilla predefinida.</summary>
    Task SendRawEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default);
}
