using GestionTime.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace GestionTime.Api.Tools;

public class ResetUserPassword
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Uso: reset-password --email usuario@ejemplo.com --password nuevaContraseña");
            return 1;
        }

        string? email = null;
        string? password = null;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--email" && i + 1 < args.Length)
            {
                email = args[i + 1];
            }
            else if (args[i] == "--password" && i + 1 < args.Length)
            {
                password = args[i + 1];
            }
        }

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Debe especificar --email y --password");
            Console.ResetColor();
            return 1;
        }

        await ExecuteAsync(email, password);
        return 0;
    }

    private static async Task ExecuteAsync(string email, string password)
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("  🔐 Reset de Contraseña de Usuario");
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine();

        // Validar contraseña
        if (password.Length < 6)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ La contraseña debe tener al menos 6 caracteres");
            Console.ResetColor();
            return;
        }

        // Cargar configuración
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = GetConnectionString(config);
        var schema = Environment.GetEnvironmentVariable("DB_SCHEMA") 
                     ?? config["Database:Schema"] 
                     ?? "pss_dvnx";

        Console.WriteLine($"📦 Base de datos: pss_dvnx | Schema: {schema}");
        Console.WriteLine($"👤 Usuario: {email}");
        Console.WriteLine();

        // Conectar a la BD
        var optionsBuilder = new DbContextOptionsBuilder<GestionTimeDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        await using var db = new GestionTimeDbContext(
            optionsBuilder.Options, 
            new DatabaseSchemaConfig { Schema = schema });

        // Buscar usuario
        Console.Write("🔍 Buscando usuario... ");
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Usuario no encontrado");
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✅ Encontrado");
        Console.ResetColor();

        // Generar hash BCrypt
        Console.Write("🔐 Generando hash BCrypt... ");
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✅ Generado");
        Console.ResetColor();

        // Actualizar usuario
        user.PasswordHash = passwordHash;
        user.MustChangePassword = false;
        user.PasswordChangedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("  ✅ CONTRASEÑA ACTUALIZADA EXITOSAMENTE");
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine($"📧 Email:    {email}");
        Console.WriteLine($"🔑 Password: {password}");
        Console.WriteLine($"📅 Fecha:    {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        Console.WriteLine();
        Console.WriteLine("💡 Ahora puedes loguearte con estas credenciales");
        Console.WriteLine();
    }

    private static string GetConnectionString(IConfiguration configuration)
    {
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

        if (!string.IsNullOrEmpty(databaseUrl) && databaseUrl.StartsWith("postgresql://"))
        {
            var uri = new Uri(databaseUrl);
            var userInfo = uri.UserInfo.Split(':');

            return $"Host={uri.Host};" +
                   $"Port={uri.Port};" +
                   $"Database={uri.AbsolutePath.TrimStart('/')};" +
                   $"Username={userInfo[0]};" +
                   $"Password={userInfo[1]};" +
                   $"SslMode=Require;";
        }

        return configuration.GetConnectionString("Default")
               ?? throw new InvalidOperationException("No se encontró connection string");
    }
}
