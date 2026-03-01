namespace GestionTime.Api.Services;

/// <summary>Envío genérico de emails (usado por campañas y otros servicios).</summary>
public interface IEmailSender
{
    /// <summary>Envía un email HTML sin plantilla predefinida.</summary>
    Task SendRawEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default);

    /// <summary>Envía un email HTML con imágenes embebidas por CID.</summary>
    Task SendRawEmailWithImagesAsync(string toEmail, string subject, string htmlBody,
        IReadOnlyList<EmailLinkedImage>? linkedImages = null, CancellationToken ct = default);
}

/// <summary>Imagen embebida por Content-ID en un email.</summary>
public sealed record EmailLinkedImage(string ContentId, string FilePath, string MimeType = "image/png");
