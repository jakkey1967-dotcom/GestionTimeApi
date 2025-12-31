using Npgsql;
using System.Text;
using System.Text.Json;

namespace GestionTime.Api.Tools;

/// <summary>
/// Herramienta para exportar schemas de PostgreSQL a CSV
/// Uso: 
///   dotnet run -- export-schema                    (modo interactivo)
///   dotnet run -- export-schema [schema_name]      (exportar schema específico)
///   dotnet run -- export-schema config save        (guardar DATABASE_URL)
///   dotnet run -- export-schema config show        (mostrar URL guardada)
///   dotnet run -- export-schema config clear       (limpiar URL guardada)
/// </summary>
public class ExportSchema
{
    private static readonly string ConfigFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".gestiontime_db_config.json"
    );

    public static async Task Main(string[] args)
    {
        // Manejar comandos de configuración
        if (args.Length >= 2 && args[0] == "config")
        {
            HandleConfigCommand(args[1]);
            return;
        }

        Console.WriteLine();
        Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║       📦 EXPORTADOR DE SCHEMAS POSTGRESQL 📦            ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // Obtener connection string (prioritario: variable de entorno, luego archivo guardado)
        var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            connectionString = LoadSavedConnectionString();
        }

        if (string.IsNullOrEmpty(connectionString))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠️  No se encontró DATABASE_URL configurado");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Configúralo ahora:");
            Console.WriteLine();
            
            SaveConnectionString();
            
            // Intentar cargar el connection string guardado
            connectionString = LoadSavedConnectionString();
            
            if (string.IsNullOrEmpty(connectionString))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n❌ No se pudo configurar DATABASE_URL");
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine("Alternativamente puedes:");
                Console.WriteLine("  1. Configurar variable de entorno:");
                Console.WriteLine("     $env:DATABASE_URL = \"postgresql://user:pass@host:5432/db\"");
                Console.WriteLine();
                Console.WriteLine("  2. Ejecutar: dotnet run -- export-schema config save");
                Console.WriteLine();
                return;
            }
        }

        // Mostrar origen del connection string
        var source = Environment.GetEnvironmentVariable("DATABASE_URL") != null 
            ? "variable de entorno" 
            : "archivo guardado";
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"\n📍 Usando connection string desde: {source}");
        Console.ResetColor();

        // Convertir formato Render si es necesario
        if (connectionString.StartsWith("postgres://") || connectionString.StartsWith("postgresql://"))
        {
            Console.WriteLine("🔄 Convirtiendo formato Render...");
            connectionString = ConvertRenderUrl(connectionString);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ Convertido");
            Console.ResetColor();
        }

        try
        {
            // Determinar schema a exportar
            string? schemaToExport = args.Length > 0 ? args[0] : null;

            if (string.IsNullOrEmpty(schemaToExport))
            {
                schemaToExport = await SelectSchemaInteractive(connectionString);
                if (string.IsNullOrEmpty(schemaToExport))
                {
                    Console.WriteLine("Operación cancelada");
                    return;
                }
            }

            await ExportSchemaToCSV(connectionString, schemaToExport);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ ERROR: {ex.Message}");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine(ex.StackTrace);
        }
    }

    private static void HandleConfigCommand(string command)
    {
        Console.WriteLine();
        Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║           ⚙️  CONFIGURACIÓN DATABASE_URL ⚙️              ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        switch (command.ToLower())
        {
            case "save":
                SaveConnectionString();
                break;

            case "show":
                ShowConnectionString();
                break;

            case "clear":
                ClearConnectionString();
                break;

            default:
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("⚠️  Comando desconocido");
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine("Comandos disponibles:");
                Console.WriteLine("  save  - Guardar DATABASE_URL permanentemente");
                Console.WriteLine("  show  - Mostrar DATABASE_URL guardada");
                Console.WriteLine("  clear - Limpiar DATABASE_URL guardada");
                break;
        }
    }

    private static void SaveConnectionString()
    {
        Console.WriteLine("Opciones para guardar DATABASE_URL:");
        Console.WriteLine();
        Console.WriteLine("  1. Pegar URL completa (Render, Railway, etc.)");
        Console.WriteLine("  2. Ingresar datos de conexión manualmente");
        Console.WriteLine();
        Console.Write("Selecciona una opción (1/2): ");
        
        var opcion = Console.ReadLine()?.Trim();
        
        string? connectionUrl = null;
        
        if (opcion == "1")
        {
            Console.WriteLine();
            Console.WriteLine("Pega tu DATABASE_URL:");
            Console.Write("> ");
            
            connectionUrl = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(connectionUrl))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("⚠️  Operación cancelada");
                Console.ResetColor();
                return;
            }

            // Validar que sea una URL válida
            if (!connectionUrl.StartsWith("postgres://") && !connectionUrl.StartsWith("postgresql://"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ ERROR: El formato debe ser postgresql://...");
                Console.ResetColor();
                return;
            }
        }
        else if (opcion == "2")
        {
            Console.WriteLine();
            Console.WriteLine("Ingresa los datos de conexión:");
            Console.WriteLine();
            
            Console.Write("Host (ej: localhost, 192.168.1.100): ");
            var host = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(host))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ Host es requerido");
                Console.ResetColor();
                return;
            }
            
            Console.Write("Puerto [5432]: ");
            var portInput = Console.ReadLine()?.Trim();
            var port = string.IsNullOrWhiteSpace(portInput) ? "5432" : portInput;
            
            Console.Write("Nombre de la base de datos: ");
            var database = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(database))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ Nombre de base de datos es requerido");
                Console.ResetColor();
                return;
            }
            
            Console.Write("Usuario: ");
            var username = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(username))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ Usuario es requerido");
                Console.ResetColor();
                return;
            }
            
            Console.Write("Password: ");
            var password = ReadPassword();
            if (string.IsNullOrWhiteSpace(password))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n❌ Password es requerido");
                Console.ResetColor();
                return;
            }
            
            // Construir URL en formato PostgreSQL
            connectionUrl = $"postgresql://{username}:{password}@{host}:{port}/{database}";
            
            Console.WriteLine();
            Console.WriteLine("✅ Connection string construido correctamente");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠️  Opción inválida");
            Console.ResetColor();
            return;
        }

        try
        {
            var config = new { DatabaseUrl = connectionUrl };
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFilePath, json);

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ DATABASE_URL guardado correctamente");
            Console.ResetColor();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"📁 Ubicación: {ConfigFilePath}");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("💡 Ahora puedes ejecutar:");
            Console.WriteLine("   dotnet run -- export-schema");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ ERROR al guardar: {ex.Message}");
            Console.ResetColor();
        }
    }

    private static void ShowConnectionString()
    {
        var url = LoadSavedConnectionString();
        
        if (string.IsNullOrEmpty(url))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠️  No hay DATABASE_URL guardada");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Para guardar una, ejecuta:");
            Console.WriteLine("  dotnet run -- export-schema config save");
            return;
        }

        Console.WriteLine("DATABASE_URL guardada:");
        Console.WriteLine();
        
        // Ocultar password por seguridad
        var maskedUrl = MaskPassword(url);
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"  {maskedUrl}");
        Console.ResetColor();
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"📁 Ubicación: {ConfigFilePath}");
        Console.ResetColor();
    }

    private static void ClearConnectionString()
    {
        if (!File.Exists(ConfigFilePath))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠️  No hay DATABASE_URL guardada");
            Console.ResetColor();
            return;
        }

        Console.Write("¿Estás seguro de eliminar el DATABASE_URL guardado? (s/N): ");
        var confirmacion = Console.ReadLine()?.Trim().ToLower();

        if (confirmacion == "s" || confirmacion == "si" || confirmacion == "yes" || confirmacion == "y")
        {
            File.Delete(ConfigFilePath);
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ DATABASE_URL eliminado");
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠️  Operación cancelada");
            Console.ResetColor();
        }
    }

    private static string? LoadSavedConnectionString()
    {
        if (!File.Exists(ConfigFilePath))
            return null;

        try
        {
            var json = File.ReadAllText(ConfigFilePath);
            var config = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            return config?.GetValueOrDefault("DatabaseUrl");
        }
        catch
        {
            return null;
        }
    }

    private static string MaskPassword(string connectionUrl)
    {
        try
        {
            var uri = new Uri(connectionUrl);
            var userInfo = uri.UserInfo.Split(':');

            if (userInfo.Length == 2)
            {
                var maskedPassword = new string('*', Math.Min(userInfo[1].Length, 8));
                return connectionUrl.Replace($":{userInfo[1]}@", $":{maskedPassword}@");
            }
        }
        catch { }
        
        return connectionUrl;
    }

    private static string ConvertRenderUrl(string databaseUrl)
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':');

        // Para localhost o conexiones locales, no usar SSL
        var requireSsl = !uri.Host.Contains("localhost") && !uri.Host.StartsWith("127.0.0.1") && !uri.Host.StartsWith("192.168.");
        var sslMode = requireSsl ? "Require" : "Disable";

        return $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode={sslMode};Trust Server Certificate=true";
    }

    private static async Task<string?> SelectSchemaInteractive(string connectionString)
    {
        Console.WriteLine();
        Console.WriteLine("🔍 Conectando a la base de datos...");

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        var query = @"
            SELECT schema_name 
            FROM information_schema.schemata 
            WHERE schema_name NOT IN ('pg_catalog', 'information_schema', 'pg_toast', 'pg_temp_1', 'pg_toast_temp_1')
            ORDER BY schema_name";

        await using var cmd = new NpgsqlCommand(query, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        var schemas = new List<string>();
        while (await reader.ReadAsync())
        {
            schemas.Add(reader.GetString(0));
        }

        if (schemas.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠️  No se encontraron schemas");
            Console.ResetColor();
            return null;
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("📁 Schemas disponibles:");
        Console.ResetColor();
        Console.WriteLine();

        for (int i = 0; i < schemas.Count; i++)
        {
            Console.WriteLine($"  {i + 1}. {schemas[i]}");
        }

        Console.WriteLine();
        Console.Write("Seleccione el número del schema (Enter para cancelar): ");
        var input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
            return null;

        if (int.TryParse(input, out int selection) && selection > 0 && selection <= schemas.Count)
        {
            return schemas[selection - 1];
        }

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("❌ Selección inválida");
        Console.ResetColor();
        return null;
    }

    private static async Task ExportSchemaToCSV(string connectionString, string schemaName)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var outputDir = Path.Combine(Directory.GetCurrentDirectory(), $"{schemaName}_export_{timestamp}");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine();
        Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"║         📦 EXPORTANDO SCHEMA: {schemaName.PadRight(20)} ║");
        Console.ResetColor();
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        // Obtener lista de tablas
        Console.WriteLine($"🔍 Buscando tablas en schema '{schemaName}'...");

        var tablesQuery = $@"
            SELECT table_name 
            FROM information_schema.tables 
            WHERE table_schema = '{schemaName}' 
              AND table_type = 'BASE TABLE'
            ORDER BY table_name";

        await using var tablesCmd = new NpgsqlCommand(tablesQuery, conn);
        await using var tablesReader = await tablesCmd.ExecuteReaderAsync();

        var tables = new List<string>();
        while (await tablesReader.ReadAsync())
        {
            tables.Add(tablesReader.GetString(0));
        }
        await tablesReader.CloseAsync();

        if (tables.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"⚠️  No se encontraron tablas en schema '{schemaName}'");
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✅ Encontradas {tables.Count} tabla(s):");
        Console.ResetColor();
        Console.WriteLine();

        // Mostrar tablas con conteos
        var tableCounts = new Dictionary<string, long>();
        foreach (var table in tables)
        {
            var countQuery = $"SELECT COUNT(*) FROM \"{schemaName}\".\"{table}\"";
            await using var countCmd = new NpgsqlCommand(countQuery, conn);
            var count = (long)(await countCmd.ExecuteScalarAsync() ?? 0);
            tableCounts[table] = count;
            Console.WriteLine($"   • {table} ({count} registros)");
        }

        Console.WriteLine();

        // Exportar cada tabla
        var summary = new StringBuilder();
        summary.AppendLine($"EXPORTACIÓN SCHEMA: {schemaName}");
        summary.AppendLine("=============================");
        summary.AppendLine($"Fecha: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        summary.AppendLine($"Total de tablas: {tables.Count}");
        summary.AppendLine();
        summary.AppendLine("TABLAS EXPORTADAS:");
        summary.AppendLine();

        int contador = 0;
        foreach (var table in tables)
        {
            contador++;
            var csvPath = Path.Combine(outputDir, $"{table}.csv");

            Console.Write($"[{contador}/{tables.Count}] Exportando '{table}'...");

            try
            {
                var rowCount = await ExportTableToCSV(conn, schemaName, table, csvPath);

                var fileInfo = new FileInfo(csvPath);
                var sizeKB = Math.Round(fileInfo.Length / 1024.0, 2);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($" ✅ ({rowCount} registros, {sizeKB} KB)");
                Console.ResetColor();

                summary.AppendLine($"- {table} : {rowCount} registros [OK]");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($" ❌ ERROR");
                Console.ResetColor();
                Console.WriteLine($"   {ex.Message}");

                summary.AppendLine($"- {table} : ERROR - {ex.Message}");
            }
        }

        // Guardar resumen
        var readmePath = Path.Combine(outputDir, "README.txt");
        await File.WriteAllTextAsync(readmePath, summary.ToString());

        // Mostrar resumen final
        Console.WriteLine();
        Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("║               ✅ EXPORTACIÓN COMPLETADA ✅                ║");
        Console.ResetColor();
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"📁 Archivos generados en: {outputDir}");
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine("Contenido:");

        foreach (var file in Directory.GetFiles(outputDir))
        {
            var fileInfo = new FileInfo(file);
            var sizeKB = Math.Round(fileInfo.Length / 1024.0, 2);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"   • {Path.GetFileName(file)} ({sizeKB} KB)");
            Console.ResetColor();
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("💡 Archivos CSV listos para usar");
        Console.ResetColor();
        Console.WriteLine();
    }

    private static async Task<int> ExportTableToCSV(NpgsqlConnection conn, string schema, string table, string outputPath)
    {
        var query = $"SELECT * FROM \"{schema}\".\"{table}\"";
        await using var cmd = new NpgsqlCommand(query, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        using var writer = new StreamWriter(outputPath, false, Encoding.UTF8);

        // Escribir headers
        var headers = new List<string>();
        for (int i = 0; i < reader.FieldCount; i++)
        {
            headers.Add(reader.GetName(i));
        }
        await writer.WriteLineAsync(string.Join(",", headers));

        // Escribir datos
        int rowCount = 0;
        while (await reader.ReadAsync())
        {
            var values = new List<string>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var value = reader.IsDBNull(i) ? "" : reader.GetValue(i).ToString();
                
                // Escapar valores con comas, comillas o saltos de línea
                if (value != null && (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r')))
                {
                    value = $"\"{value.Replace("\"", "\"\"")}\"";
                }
                
                values.Add(value ?? "");
            }
            await writer.WriteLineAsync(string.Join(",", values));
            rowCount++;
        }

        return rowCount;
    }

    private static string ReadPassword()
    {
        var password = new StringBuilder();
        ConsoleKeyInfo key;
        
        do
        {
            key = Console.ReadKey(true);
            
            if (key.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                password.Remove(password.Length - 1, 1);
                Console.Write("\b \b");
            }
            else if (!char.IsControl(key.KeyChar))
            {
                password.Append(key.KeyChar);
                Console.Write("*");
            }
        } while (key.Key != ConsoleKey.Enter);
        
        return password.ToString();
    }
}
