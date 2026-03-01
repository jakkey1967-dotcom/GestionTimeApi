using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace GestionTime.Api.Services;

public class SmtpEmailService : IEmailService, IEmailSender
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
                        <h1>🔐 Recuperación de Contraseña</h1>
                    </div>
                    <div class='content'>
                        <p>Hola,</p>
                        <p>Recibimos una solicitud para restablecer tu contraseña en <strong>GestionTime</strong>.</p>
                        
                        <p>Tu código de verificación es:</p>
                        <div class='token'>{resetToken}</div>
                        
                        <p>Este código expira en <strong>1 hora</strong>.</p>
                        
                        <p><strong>⚠️ Si no solicitaste este cambio, ignora este correo.</strong></p>
                        
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
                        <h1>📧 Verificación de Registro</h1>
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

    public async Task SendActivationEmailAsync(GestionTime.Domain.Auth.User user, string activationToken)
    {
        try
        {
            var activationUrl = GenerateActivationUrl(activationToken);
            var userName = user.FullName ?? user.Email?.Split('@')[0] ?? "Usuario";
            
            _logger.LogInformation("📧 Enviando email de activación a {Email}", user.Email);
            _logger.LogInformation("   URL de activación: {Url}", activationUrl);

            // ✅ Logo embebido en Base64 (funciona en todos los clientes de email)
            var logoBase64 = GetLogoBase64();

            var htmlBody = $@"
<!DOCTYPE html>
<html lang=""es"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f9f9f9; }}
        .header {{ background-color: #0B8C99; color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
        .logo {{ max-width: 200px; height: auto; margin-bottom: 15px; }}
        .content {{ padding: 30px; background-color: white; border-radius: 0 0 8px 8px; }}
        .button {{ 
            display: inline-block;
            padding: 15px 40px;
            background-color: #0B8C99;
            color: white !important;
            text-decoration: none;
            border-radius: 5px;
            font-weight: bold;
            margin: 20px 0;
        }}
        .link-text {{ 
            background-color: #f0f0f0;
            padding: 15px;
            border-radius: 5px;
            word-break: break-all;
            font-size: 12px;
            color: #0B8C99;
            margin: 15px 0;
        }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            {(string.IsNullOrEmpty(logoBase64) ? "" : $"<img src='data:image/png;base64,{logoBase64}' alt='GestionTime' class='logo' />")}
            <h1>¡Bienvenido a GestionTime!</h1>
        </div>
        <div class='content'>
            <h2>Hola {userName},</h2>
            <p>Gracias por registrarte en <strong>GestionTime</strong>.</p>
            <p>Para activar tu cuenta, haz clic en el botón de abajo:</p>
            
            <div style='text-align: center;'>
                <a href='{activationUrl}' class='button'>✓ Activar mi cuenta ahora</a>
            </div>
            
            <p><strong>¿El botón no funciona?</strong><br>Puedes copiar y pegar este enlace en tu navegador:</p>
            <div class='link-text'>{activationUrl}</div>
            
            <p style='color: #666; font-size: 14px;'>🔒 Este enlace expira en 24 horas por seguridad.</p>
            
            <p>Saludos,<br/>El equipo de GestionTime</p>
        </div>
        <div class='footer'>
            <p>Este es un correo automático, por favor no respondas a este mensaje.</p>
            <p>© 2025 GestionTime. Todos los derechos reservados.</p>
            <p style='margin-top: 10px; font-size: 11px;'>Si no solicitaste esta cuenta, puedes ignorar este email de forma segura.</p>
        </div>
    </div>
</body>
</html>";

            var subject = "Activar tu cuenta - GestionTime";
            await SendEmailAsync(user.Email ?? throw new ArgumentException("Email requerido"), subject, htmlBody);
            
            _logger.LogInformation("✅ Email de activación enviado exitosamente a {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error enviando email de activación a {Email}", user.Email);
            throw;
        }
    }

    /// <summary>
    /// Obtiene el logo en Base64 para embeber en emails
    /// </summary>
    private string GetLogoBase64()
    {
        try
        {
            // Lista de rutas posibles para el logo
            var possiblePaths = new[]
            {
                Path.Combine("wwwroot", "images", "LogoOscuro.png"),
                Path.Combine("wwwroot_pss_dvnx", "images", "LogoOscuro.png"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "images", "LogoOscuro.png"),
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "LogoOscuro.png")
            };

            foreach (var logoPath in possiblePaths)
            {
                if (File.Exists(logoPath))
                {
                    _logger.LogInformation("✅ Logo encontrado: {Path}", logoPath);
                    var imageBytes = File.ReadAllBytes(logoPath);
                    return Convert.ToBase64String(imageBytes);
                }
                else
                {
                    _logger.LogDebug("⏭️ Logo no encontrado en: {Path}", logoPath);
                }
            }
            
            _logger.LogWarning("⚠️ Logo no encontrado en ninguna ruta");
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Error cargando logo para email");
            return string.Empty;
        }
    }

    private string GenerateActivationUrl(string token)
    {
        // ✅ Prioridad: Variable de entorno, luego configuración
        var baseUrl = Environment.GetEnvironmentVariable("APP_BASE_URL") 
                      ?? _config["App:BaseUrl"] 
                      ?? "http://localhost:2501";
        
        baseUrl = baseUrl.TrimEnd('/');
        
        _logger.LogInformation("🌐 Generando URL de activación con BaseUrl: {BaseUrl}", baseUrl);
        
        return $"{baseUrl}/api/v1/auth/activate/{token}";
    }

    // GL-BEGIN: IEmailSender
    /// <summary>Envío genérico de email HTML (usado por campañas).</summary>
    public async Task SendRawEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        await SendEmailAsync(toEmail, subject, htmlBody);
    }

    /// <summary>Envío de email HTML con imágenes embebidas por CID (logo, etc.).</summary>
    public async Task SendRawEmailWithImagesAsync(string toEmail, string subject, string htmlBody,
        IReadOnlyList<EmailLinkedImage>? linkedImages = null, CancellationToken ct = default)
    {
        await SendEmailWithLinkedImagesAsync(toEmail, subject, htmlBody, linkedImages);
    }
    // GL-END: IEmailSender

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
                _logger.LogError("❌ Configuración de email incompleta");
                throw new InvalidOperationException("Configuración de email incompleta");
            }

            _logger.LogInformation("📧 Enviando email via MailKit a {Email}", toEmail);
            _logger.LogInformation("   SMTP: {Host}:{Port}, SSL/TLS: STARTTLS", smtpHost, smtpPort);

            // ✅ USAR MAILKIT (soporta STARTTLS correctamente)
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            
            // ✅ CONECTAR CON STARTTLS (puerto 587)
            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            
            // ✅ AUTENTICAR
            await client.AuthenticateAsync(smtpUser, smtpPass);
            
            // ✅ ENVIAR
            await client.SendAsync(message);
            
            // ✅ DESCONECTAR
            await client.DisconnectAsync(true);

            _logger.LogInformation("✅ Email enviado exitosamente a {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error enviando email a {Email}", toEmail);
            throw;
        }
    }

    // GL-BEGIN: SendEmailWithLinkedImagesAsync
    private async Task SendEmailWithLinkedImagesAsync(string toEmail, string subject, string htmlBody,
        IReadOnlyList<EmailLinkedImage>? linkedImages)
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
                throw new InvalidOperationException("Configuración de email incompleta");

            _logger.LogInformation("📧 Enviando email con CID images a {Email}", toEmail);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };

            if (linkedImages is { Count: > 0 })
            {
                foreach (var img in linkedImages)
                {
                    if (!File.Exists(img.FilePath))
                    {
                        _logger.LogWarning("CID image no encontrada: {Path}", img.FilePath);
                        continue;
                    }

                    var attachment = bodyBuilder.LinkedResources.Add(img.FilePath);
                    attachment.ContentId = img.ContentId;
                    attachment.ContentDisposition = new MimeKit.ContentDisposition(MimeKit.ContentDisposition.Inline);
                }
            }

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpUser, smtpPass);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("✅ Email con CID enviado a {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error enviando email con CID a {Email}", toEmail);
            throw;
        }
    }
    // GL-END: SendEmailWithLinkedImagesAsync
}
