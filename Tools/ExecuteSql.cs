using Npgsql;

namespace GestionTime.Api.Tools;

/// <summary>
/// Herramienta para ejecutar scripts SQL directamente en Render
/// Uso: dotnet run -- execute-sql [archivo.sql]
/// </summary>
public class ExecuteSql
{
    private static readonly string RenderConnectionString = 
        "Host=dpg-d57tobm3jp1c73b6i4ug-a.frankfurt-postgres.render.com;Port=5432;Database=pss_dvnx;Username=gestiontime;Password=BvCDRFguh9SljJJUZOzGpdvpxgf18qnI;SSL Mode=Require;Trust Server Certificate=true";

    public static async Task Main(string[] args)
    {
        Console.WriteLine();
        Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║           📝 EJECUTAR SQL EN RENDER 📝                  ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        if (args.Length == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠️  Uso: dotnet run -- execute-sql [archivo.sql]");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Ejemplo:");
            Console.WriteLine("  dotnet run -- execute-sql Tools/SQL/create_admin_user.sql");
            Console.WriteLine();
            return;
        }

        var sqlFile = args[0];

        if (!File.Exists(sqlFile))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ El archivo no existe: {sqlFile}");
            Console.ResetColor();
            return;
        }

        try
        {
            var sql = await File.ReadAllTextAsync(sqlFile);
            
            Console.WriteLine($"📄 Archivo: {sqlFile}");
            Console.WriteLine($"📏 Tamaño: {sql.Length} caracteres");
            Console.WriteLine();
            
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠️  ¿Estás seguro de ejecutar este script? (s/N): ");
            Console.ResetColor();
            Console.Write("> ");
            var confirm = Console.ReadLine()?.Trim().ToLowerInvariant();
            
            if (confirm != "s" && confirm != "si" && confirm != "y" && confirm != "yes")
            {
                Console.WriteLine("Operación cancelada");
                return;
            }

            Console.WriteLine();
            Console.WriteLine("🔌 Conectando a Render...");

            await using var conn = new NpgsqlConnection(RenderConnectionString);
            await conn.OpenAsync();
            
            Console.WriteLine("✅ Conectado");
            Console.WriteLine();
            Console.WriteLine("⚙️  Ejecutando script...");
            Console.WriteLine();

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.CommandTimeout = 60; // 60 segundos timeout

            // Capturar mensajes de NOTICE de PostgreSQL
            conn.Notice += (sender, e) =>
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"  ℹ️  {e.Notice.MessageText}");
                Console.ResetColor();
            };

            // Ejecutar script
            var affectedRows = await cmd.ExecuteNonQueryAsync();
            
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✅ Script ejecutado exitosamente");
            Console.ResetColor();
            Console.WriteLine($"   Filas afectadas: {affectedRows}");
            Console.WriteLine();

            // Si el script contiene SELECT, mostrar resultados
            if (sql.ToUpperInvariant().Contains("SELECT"))
            {
                Console.WriteLine("📊 Resultados:");
                Console.WriteLine();

                await using var selectCmd = new NpgsqlCommand(sql, conn);
                await using var reader = await selectCmd.ExecuteReaderAsync();

                // Mostrar nombres de columnas
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    Console.Write($"{reader.GetName(i),-20} | ");
                }
                Console.WriteLine();
                Console.WriteLine(new string('-', reader.FieldCount * 23));

                // Mostrar filas
                while (await reader.ReadAsync())
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var value = reader.IsDBNull(i) ? "NULL" : reader.GetValue(i).ToString();
                        Console.Write($"{value,-20} | ");
                    }
                    Console.WriteLine();
                }
                Console.WriteLine();
            }

            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine();
            
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ ERROR: {ex.Message}");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Detalles:");
            Console.WriteLine(ex.ToString());
        }
    }
}
