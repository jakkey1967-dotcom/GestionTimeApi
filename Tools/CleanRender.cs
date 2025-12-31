using Npgsql;

namespace GestionTime.Api.Tools;

/// <summary>
/// Herramienta para limpiar completamente la base de datos de Render
/// ADVERTENCIA: Esto eliminará TODAS las tablas y datos
/// Uso: dotnet run -- clean-render
/// </summary>
public class CleanRender
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine();
        Console.WriteLine("????????????????????????????????????????????????????????????");
        Console.WriteLine("?       ??  LIMPIEZA COMPLETA DE BASE DE DATOS ??         ?");
        Console.WriteLine("????????????????????????????????????????????????????????????");
        Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("??  ADVERTENCIA: Esta operación eliminará TODAS las tablas y datos");
        Console.WriteLine("??  de la base de datos de Render.");
        Console.ResetColor();
        Console.WriteLine();

        Console.Write("¿Estás ABSOLUTAMENTE seguro de continuar? (escribe 'SI ESTOY SEGURO'): ");
        var confirmacion = Console.ReadLine()?.Trim();

        if (confirmacion != "SI ESTOY SEGURO")
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n??  Operación cancelada");
            Console.ResetColor();
            return;
        }

        var connectionString = "Host=dpg-d57tobm3jp1c73b6i4ug-a.frankfurt-postgres.render.com;Port=5432;Database=pss_dvnx;Username=gestiontime;Password=BvCDRFguh9SljJJUZOzGpdvpxgf18qnI;SSL Mode=Require;Trust Server Certificate=true";

        try
        {
            await using var conn = new NpgsqlConnection(connectionString);
            
            Console.WriteLine("\n?? Conectando a Render...");
            await conn.OpenAsync();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("? Conectado");
            Console.ResetColor();
            Console.WriteLine();

            // 1. Obtener todos los schemas personalizados
            Console.WriteLine("?? Buscando schemas personalizados...");
            var schemasQuery = @"
                SELECT schema_name 
                FROM information_schema.schemata 
                WHERE schema_name NOT IN ('pg_catalog', 'information_schema', 'pg_toast')
                ORDER BY schema_name";

            var schemas = new List<string>();
            await using (var cmd = new NpgsqlCommand(schemasQuery, conn))
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    schemas.Add(reader.GetString(0));
                }
            }

            if (schemas.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("??  No hay schemas personalizados para limpiar");
                Console.ResetColor();
                return;
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"?? Encontrados {schemas.Count} schema(s): {string.Join(", ", schemas)}");
            Console.ResetColor();
            Console.WriteLine();

            // 2. Eliminar cada schema y recrearlo
            foreach (var schema in schemas)
            {
                Console.Write($"???  Limpiando schema '{schema}'... ");

                try
                {
                    // Eliminar schema con CASCADE (borra todas las tablas)
                    var dropSchemaCmd = new NpgsqlCommand($"DROP SCHEMA IF EXISTS \"{schema}\" CASCADE", conn);
                    await dropSchemaCmd.ExecuteNonQueryAsync();

                    // Recrear schema vacío
                    var createSchemaCmd = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", conn);
                    await createSchemaCmd.ExecuteNonQueryAsync();

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("? Limpio");
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"? Error: {ex.Message}");
                    Console.ResetColor();
                }
            }

            Console.WriteLine();
            Console.WriteLine("????????????????????????????????????????????????????????????");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("?               ? LIMPIEZA COMPLETADA ?                   ?");
            Console.ResetColor();
            Console.WriteLine("????????????????????????????????????????????????????????????");
            Console.WriteLine();

            Console.WriteLine("?? La base de datos está ahora vacía y lista para usar");
            Console.WriteLine("?? Puedes ejecutar tu aplicación para aplicar las migraciones");
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"? ERROR: {ex.Message}");
            Console.ResetColor();
            Console.WriteLine(ex.StackTrace);
        }
    }
}
