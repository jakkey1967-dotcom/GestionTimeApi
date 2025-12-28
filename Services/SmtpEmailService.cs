using System.Net;
using System.Net.Mail;

namespace GestionTime.Api.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetToken)
    {
        var subject = "Recuperación de Contraseña - GestionTime";
        var htmlBody = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f9f9f9; }}
                    .header {{ background-color: #0B8C99; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}
                    .content {{ padding: 30px; background-color: white; border-radius: 0 0 8px 8px; }}
                    .token {{ 
                        background-color: #e9ecef; 
                        padding: 15px; 
                        border-left: 4px solid #0B8C99; 
                        margin: 20px 0;
                        font-size: 24px;
                        font-weight: bold;
                        text-align: center;
                        letter-spacing: 2px;
                    }}
                    .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>?? Recuperación de Contraseña</h1>
                    </div>
                    <div class='content'>
                        <p>Hola,</p>
                        <p>Recibimos una solicitud para restablecer tu contraseña en <strong>GestionTime</strong>.</p>
                        
                        <p>Tu código de verificación es:</p>
                        <div class='token'>{resetToken}</div>
                        
                        <p>Este código expira en <strong>1 hora</strong>.</p>
                        
                        <p><strong>?? Si no solicitaste este cambio, ignora este correo.</strong></p>
                        
                        <p>Saludos,<br/>El equipo de GestionTime</p>
                    </div>
                    <div class='footer'>
                        <p>Este es un correo automático, por favor no respondas a este mensaje.</p>
                        <p>&copy; 2024 GestionTime. Todos los derechos reservados.</p>
                    </div>
                </div>
            </body>
            </html>
        ";

        await SendEmailAsync(toEmail, subject, htmlBody);
    }

    public async Task SendRegistrationEmailAsync(string toEmail, string verificationToken)
    {
        var subject = "Verificación de Email - GestionTime";
        var htmlBody = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f9f9f9; }}
                    .header {{ background-color: #0B8C99; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}
                    .content {{ padding: 30px; background-color: white; border-radius: 0 0 8px 8px; }}
                    .token {{ 
                        background-color: #e9ecef; 
                        padding: 15px; 
                        border-left: 4px solid #0B8C99; 
                        margin: 20px 0;
                        font-size: 24px;
                        font-weight: bold;
                        text-align: center;
                        letter-spacing: 2px;
                    }}
                    .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>?? Verificación de Registro</h1>
                    </div>
                    <div class='content'>
                        <p>¡Bienvenido a <strong>GestionTime</strong>!</p>
                        <p>Para completar tu registro, necesitamos verificar tu dirección de correo electrónico.</p>
                        
                        <p>Tu código de verificación es:</p>
                        <div class='token'>{verificationToken}</div>
                        
                        <p>Este código expira en <strong>24 horas</strong>.</p>
                        
                        <p>Si no creaste esta cuenta, puedes ignorar este correo.</p>
                        
                        <p>Saludos,<br/>El equipo de GestionTime</p>
                    </div>
                    <div class='footer'>
                        <p>Este es un correo automático, por favor no respondas a este mensaje.</p>
                        <p>&copy; 2024 GestionTime. Todos los derechos reservados.</p>
                    </div>
                </div>
            </body>
            </html>
        ";

        await SendEmailAsync(toEmail, subject, htmlBody);
    }

    /// <summary>
    /// Envía email de activación con enlace directo
    /// </summary>
    public async Task SendActivationEmailAsync(GestionTime.Domain.Auth.User user, string activationToken)
    {
        try
        {
            var activationUrl = GenerateActivationUrl(activationToken);
            var logoUrl = GenerateLogoUrl();
            
            _logger.LogInformation("?? Enviando email de activación a {Email}", user.Email);
            _logger.LogInformation("   URL de activación: {Url}", activationUrl);

            // Cargar template HTML
            var templatePath = Path.Combine("Templates", "EmailTemplates", "ActivationEmail.html");
            var htmlTemplate = await File.ReadAllTextAsync(templatePath);

            // Reemplazar variables en el template
            var htmlBody = htmlTemplate
                .Replace("{{USER_NAME}}", user.FullName ?? user.Email?.Split('@')[0] ?? "Usuario")
                .Replace("{{ACTIVATION_LINK}}", activationUrl)
                .Replace("{{LOGO_URL}}", logoUrl);

            // Crear mensaje
            using var message = new MailMessage();
            message.From = new MailAddress(_config["Email:From"], _config["Email:FromName"]);
            message.To.Add(user.Email);
            message.Subject = "Activar tu cuenta - GestionTime";
            message.Body = htmlBody;
            message.IsBodyHtml = true;
            message.Priority = MailPriority.High;

            // Configurar SMTP
            using var smtp = new SmtpClient(_config["Email:SmtpHost"], int.Parse(_config["Email:SmtpPort"]))
            {
                Credentials = new NetworkCredential(_config["Email:SmtpUser"], _config["Email:SmtpPassword"]),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            await smtp.SendMailAsync(message);
            
            _logger.LogInformation("? Email de activación enviado exitosamente a {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Error enviando email de activación a {Email}", user.Email);
            throw;
        }
    }

    /// <summary>
    /// Genera URL de activación completa
    /// </summary>
    private string GenerateActivationUrl(string token)
    {
        // TODO: Obtener URL base de configuración en producción
        var baseUrl = "https://localhost:2501";
        return $"{baseUrl}/api/v1/auth/activate/{token}";
    }

    /// <summary>
    /// Genera URL del logo completa
    /// </summary>
    private string GenerateLogoUrl()
    {
        var baseUrl = "https://localhost:2501";
        return $"{baseUrl}/images/LogoOscuro.png";
    }

    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        try
        {
            var smtpHost = _config["Email:SmtpHost"];
            var smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
            var smtpUser = _config["Email:SmtpUser"];
            var smtpPass = _config["Email:SmtpPassword"];
            var fromEmail = _config["Email:From"] ?? "noreply@gestiontime.com";
            var fromName = _config["Email:FromName"] ?? "GestionTime";

            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPass))
            {
                _logger.LogError("? Configuración de email incompleta. Verifica appsettings.json");
                throw new InvalidOperationException("Configuración de email incompleta");
            }

            _logger.LogInformation("?? Enviando email a {Email} - Asunto: {Subject}", toEmail, subject);

            using var smtpClient = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(smtpUser, smtpPass)
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage);

            _logger.LogInformation("? Email enviado exitosamente a {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Error enviando email a {Email}", toEmail);
            throw;
        }
    }
}
