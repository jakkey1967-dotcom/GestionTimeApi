using BCrypt.Net;

namespace GestionTime.Api.Tools;

/// <summary>
/// Herramienta para verificar/generar hashes BCrypt
/// Uso: dotnet run -- verify-password [password] [hash]
/// </summary>
public class VerifyPassword
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine();
        Console.WriteLine("????????????????????????????????????????????????????????????");
        Console.WriteLine("?         ?? VERIFICAR PASSWORD BCRYPT ??                 ?");
        Console.WriteLine("????????????????????????????????????????????????????????????");
        Console.WriteLine();

        if (args.Length == 0)
        {
            // Modo: Generar hash para "rootadmin"
            var password = "rootadmin";
            var hash = BCrypt.Net.BCrypt.HashPassword(password);
            
            Console.WriteLine("?? GENERANDO HASH NUEVO:");
            Console.WriteLine($"   Password: {password}");
            Console.WriteLine($"   Hash:     {hash}");
            Console.WriteLine();
            
            // Verificar que funciona
            var works = BCrypt.Net.BCrypt.Verify(password, hash);
            Console.WriteLine($"   ? Verificación: {(works ? "CORRECTO" : "ERROR")}");
            Console.WriteLine();
        }
        else if (args.Length >= 2)
        {
            // Modo: Verificar password contra hash
            var password = args[0];
            var hash = args[1];
            
            Console.WriteLine("?? VERIFICANDO PASSWORD:");
            Console.WriteLine($"   Password: {password}");
            Console.WriteLine($"   Hash:     {hash}");
            Console.WriteLine();
            
            try
            {
                var result = BCrypt.Net.BCrypt.Verify(password, hash);
                
                if (result)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("   ? PASSWORD CORRECTO");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("   ? PASSWORD INCORRECTO");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"   ? ERROR: {ex.Message}");
                Console.ResetColor();
            }
            
            Console.WriteLine();
        }
        else
        {
            Console.WriteLine("??  Uso:");
            Console.WriteLine("   dotnet run -- verify-password                    (genera hash para 'rootadmin')");
            Console.WriteLine("   dotnet run -- verify-password [password] [hash]  (verifica password)");
            Console.WriteLine();
        }
        
        await Task.CompletedTask;
    }
}
