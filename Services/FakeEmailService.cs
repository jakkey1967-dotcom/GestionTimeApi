namespace GestionTime.Api.Services;

public class FakeEmailService : IEmailService
{
    private readonly ILogger<FakeEmailService> _logger;

    public FakeEmailService(ILogger<FakeEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendPasswordResetEmailAsync(string toEmail, string resetToken)
    {
        _logger.LogWarning("[FAKE EMAIL] Reset password para {Email} - Código: {Token}", toEmail, resetToken);
        Console.WriteLine("========================");
        Console.WriteLine("FAKE EMAIL - RESET PASSWORD");
        Console.WriteLine($"Para: {toEmail}");
        Console.WriteLine("Asunto: Recuperación de Contraseña");
        Console.WriteLine($"Código: {resetToken}");
        Console.WriteLine("========================");
        return Task.CompletedTask;
    }

    public Task SendRegistrationEmailAsync(string toEmail, string verificationToken)
    {
        _logger.LogWarning("[FAKE EMAIL] Verificación de registro para {Email} - Código: {Token}", toEmail, verificationToken);
        Console.WriteLine("========================");
        Console.WriteLine("FAKE EMAIL - REGISTRO");
        Console.WriteLine($"Para: {toEmail}");
        Console.WriteLine("Asunto: Verificación de Email");
        Console.WriteLine($"Código: {verificationToken}");
        Console.WriteLine("========================");
        return Task.CompletedTask;
    }

    public Task SendActivationEmailAsync(GestionTime.Domain.Auth.User user, string activationToken)
    {
        var activationUrl = $"https://localhost:2501/api/v1/auth/activate/{activationToken}";

        _logger.LogInformation("?? FAKE EMAIL - Activación para {Email}:", user.Email);
        _logger.LogInformation("   ?? Usuario: {FullName}", user.FullName ?? "Sin nombre");
        _logger.LogInformation("   ?? URL de activación: {Url}", activationUrl);
        _logger.LogInformation("   ?? Para activar: Abre el enlace de arriba en tu navegador");

        return Task.CompletedTask;
    }
}
