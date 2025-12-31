using Npgsql;
using System.Text;

namespace GestionTime.Api.Tools;

/// <summary>
/// Herramienta para importar schemas de PostgreSQL desde CSV
/// Uso: 
///   dotnet run -- import-schema [ruta_carpeta_csv]
///   dotnet run -- import-schema                     (modo interactivo, busca carpetas *_export_*)
/// </summary>
public class ImportSchema
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine();
        Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║       📥 IMPORTADOR DE SCHEMAS POSTGRESQL 📥            ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // Determinar carpeta de origen
        string? exportFolder = null;
        
        if (args.Length > 0)
        {
            exportFolder = args[0];
        }
        else
        {
            // Buscar la carpeta de exportación más reciente automáticamente
            var currentDir = Directory.GetCurrentDirectory();
            var exportFolders = Directory.GetDirectories(currentDir, "*_export_*")
                .OrderByDescending(d => Directory.GetCreationTime(d))
                .ToList();

            if (exportFolders.Count > 0)
            {
                exportFolder = exportFolders[0];
                var folderName = Path.GetFileName(exportFolder);
                var csvCount = Directory.GetFiles(exportFolder, "*.csv").Length;
                
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"📁 Carpeta detectada: {folderName}");
                Console.WriteLine($"📦 Archivos CSV: {csvCount}");
                Console.ResetColor();
                Console.WriteLine();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("⚠️  No se encontraron carpetas de exportación");
                Console.ResetColor();
                return;
            }
        }

        if (!Directory.Exists(exportFolder))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ ERROR: La carpeta '{exportFolder}' no existe");
            Console.ResetColor();
            return;
        }

        // Obtener connection string de destino
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("⚠️  DESTINO: Base de datos LOCAL (gtdefault)");
        Console.ResetColor();
        Console.WriteLine();

        var connectionString = await GetDestinationConnectionString();
        if (string.IsNullOrEmpty(connectionString))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ No se pudo configurar la conexión");
            Console.ResetColor();
            return;
        }

        await ImportFromCSV(exportFolder, connectionString);
    }

    private static Task<string?> SelectExportFolderInteractive()
    {
        Console.WriteLine("🔍 Buscando carpetas de exportación...");
        Console.WriteLine();

        var currentDir = Directory.GetCurrentDirectory();
        var exportFolders = Directory.GetDirectories(currentDir, "*_export_*")
            .OrderByDescending(d => Directory.GetCreationTime(d))
            .Take(10)
            .ToList();

        if (exportFolders.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠️  No se encontraron carpetas de exportación");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Especifica manualmente la ruta:");
            Console.Write("> ");
            return Task.FromResult(Console.ReadLine()?.Trim());
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"📁 Carpetas de exportación encontradas:");
        Console.ResetColor();
        Console.WriteLine();

        for (int i = 0; i < exportFolders.Count; i++)
        {
            var folderName = Path.GetFileName(exportFolders[i]);
            var creationTime = Directory.GetCreationTime(exportFolders[i]);
            var csvCount = Directory.GetFiles(exportFolders[i], "*.csv").Length;
            
            Console.WriteLine($"  {i + 1}. {folderName}");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"     {creationTime:yyyy-MM-dd HH:mm:ss} - {csvCount} archivo(s) CSV");
            Console.ResetColor();
        }

        Console.WriteLine();
        Console.Write("Seleccione el número de carpeta (Enter para cancelar): ");
        var input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
            return Task.FromResult<string?>(null);

        if (int.TryParse(input, out int selection) && selection > 0 && selection <= exportFolders.Count)
        {
            return Task.FromResult<string?>(exportFolders[selection - 1]);
        }

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("❌ Selección inválida");
        Console.ResetColor();
        return Task.FromResult<string?>(null);
    }

    private static Task<string?> GetDestinationConnectionString()
    {
        Console.WriteLine("Datos de conexión LOCAL:");
        Console.WriteLine();

        Console.Write("Host [localhost]: ");
        var host = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(host)) host = "localhost";

        Console.Write("Puerto [5432]: ");
        var portInput = Console.ReadLine()?.Trim();
        var port = string.IsNullOrWhiteSpace(portInput) ? "5432" : portInput;

        Console.Write("Base de datos [gestiontime]: ");
        var database = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(database)) database = "gestiontime";

        Console.Write("Usuario [postgres]: ");
        var username = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(username)) username = "postgres";

        Console.Write("Password: ");
        var password = ReadPassword();
        Console.WriteLine();
        Console.WriteLine();

        var connectionUrl = $"postgresql://{username}:{password}@{host}:{port}/{database}";
        return Task.FromResult<string?>(ConvertToNpgsqlConnectionString(connectionUrl));
    }

    private static string ConvertToNpgsqlConnectionString(string databaseUrl)
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':');

        var requireSsl = !uri.Host.Contains("localhost") && !uri.Host.StartsWith("127.0.0.1") && !uri.Host.StartsWith("192.168.");
        var sslMode = requireSsl ? "Require" : "Disable";

        return $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode={sslMode};Trust Server Certificate=true";
    }

    private static async Task ImportFromCSV(string exportFolder, string connectionString)
    {
        Console.WriteLine();
        Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
        Console.ForegroundColor = ConsoleColor.Cyan;
        var folderName = Path.GetFileName(exportFolder);
        Console.WriteLine($"║         📥 IMPORTANDO: {folderName.PadRight(32)} ║");
        Console.ResetColor();
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        var csvFiles = Directory.GetFiles(exportFolder, "*.csv")
            .Where(f => !Path.GetFileName(f).Equals("README.txt", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f)
            .ToList();

        // DEBUG: Mostrar todos los archivos encontrados
        var allFiles = Directory.GetFiles(exportFolder);
        if (allFiles.Length > 0)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"📋 Archivos en carpeta: {allFiles.Length}");
            foreach (var file in allFiles.Take(5))
            {
                Console.WriteLine($"   - {Path.GetFileName(file)}");
            }
            if (allFiles.Length > 5)
                Console.WriteLine($"   ... y {allFiles.Length - 5} más");
            Console.ResetColor();
            Console.WriteLine();
        }

        if (csvFiles.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠️  No se encontraron archivos CSV");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine($"📁 Carpeta verificada: {exportFolder}");
            return;
        }

        Console.WriteLine($"📦 Encontrados {csvFiles.Count} archivo(s) CSV");
        Console.WriteLine();

        // Schema de destino fijo: gtdefault
        var schemaName = "gtdefault";
        
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"🎯 Schema de destino: {schemaName}");
        Console.ResetColor();
        Console.WriteLine();

        // Modo de importación
        Console.WriteLine("Modo de importación:");
        Console.WriteLine("  1. TRUNCATE - Vaciar tablas y luego insertar (RECOMENDADO)");
        Console.WriteLine("  2. APPEND - Agregar datos (puede causar duplicados)");
        Console.WriteLine("  3. DROP & CREATE - Eliminar y recrear tablas");
        Console.WriteLine();
        Console.Write("Selecciona modo [1]: ");
        var modeInput = Console.ReadLine()?.Trim();
        var mode = string.IsNullOrWhiteSpace(modeInput) ? "1" : modeInput;

        try
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ Conectado a la base de datos");
            Console.ResetColor();

            // Crear schema si no existe
            Console.WriteLine($"🔧 Verificando schema '{schemaName}'...");
            var createSchemaCmd = new NpgsqlCommand($"CREATE SCHEMA IF NOT EXISTS \"{schemaName}\"", conn);
            await createSchemaCmd.ExecuteNonQueryAsync();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ Schema listo");
            Console.ResetColor();
            Console.WriteLine();

            var summary = new StringBuilder();
            summary.AppendLine($"IMPORTACIÓN A SCHEMA: {schemaName}");
            summary.AppendLine("=============================");
            summary.AppendLine($"Fecha: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            summary.AppendLine($"Carpeta origen: {exportFolder}");
            summary.AppendLine($"Total de archivos: {csvFiles.Count}");
            summary.AppendLine($"Modo: {(mode == "1" ? "TRUNCATE" : mode == "2" ? "APPEND" : "DROP & CREATE")}");
            summary.AppendLine();

            int contador = 0;
            int exitosos = 0;
            int fallidos = 0;

            foreach (var csvFile in csvFiles)
            {
                contador++;
                var tableName = Path.GetFileNameWithoutExtension(csvFile);

                Console.Write($"[{contador}/{csvFiles.Count}] {tableName}...");

                try
                {
                    var rowsImported = await ImportTableFromCSV(conn, schemaName, tableName, csvFile, mode);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($" ✅ {rowsImported} registros");
                    Console.ResetColor();

                    summary.AppendLine($"✅ {tableName}: {rowsImported} registros");
                    exitosos++;
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($" ❌ ERROR");
                    Console.ResetColor();
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine($"   {ex.Message}");
                    Console.ResetColor();

                    summary.AppendLine($"❌ {tableName}: {ex.Message}");
                    fallidos++;
                }
            }

            summary.AppendLine();
            summary.AppendLine("RESUMEN:");
            summary.AppendLine($"✅ Exitosos: {exitosos}");
            summary.AppendLine($"❌ Fallidos: {fallidos}");

            // Guardar log de importación
            var logPath = Path.Combine(exportFolder, $"IMPORT_LOG_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            await File.WriteAllTextAsync(logPath, summary.ToString());

            // Resumen final
            Console.WriteLine();
            Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("║               ✅ IMPORTACIÓN COMPLETADA ✅                ║");
            Console.ResetColor();
            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✅ Exitosos: {exitosos}");
            Console.ResetColor();
            
            if (fallidos > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Fallidos: {fallidos}");
                Console.ResetColor();
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"📁 Log: {logPath}");
            Console.ResetColor();
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ ERROR: {ex.Message}");
            Console.ResetColor();
            Console.WriteLine(ex.StackTrace);
        }
    }

    private static async Task<int> ImportTableFromCSV(NpgsqlConnection conn, string schema, string tableName, string csvPath, string mode)
    {
        var lines = await File.ReadAllLinesAsync(csvPath);
        if (lines.Length == 0) return 0;

        // DEBUG: Mostrar info del CSV
        Console.Write($" ({lines.Length - 1} filas)");

        var headers = ParseCsvLine(lines[0]);
        var columnNames = string.Join(", ", headers.Select(h => $"\"{h}\""));

        // Verificar si la tabla existe
        var tableExistsQuery = $@"
            SELECT EXISTS (
                SELECT FROM information_schema.tables 
                WHERE table_schema = '{schema}' 
                AND table_name = '{tableName}'
            )";
        
        await using var checkCmd = new NpgsqlCommand(tableExistsQuery, conn);
        var tableExists = (bool)(await checkCmd.ExecuteScalarAsync() ?? false);

        if (!tableExists)
        {
            // Crear tabla automáticamente con todas las columnas como TEXT
            var columnDefinitions = string.Join(", ", headers.Select(h => $"\"{h}\" TEXT"));
            var createTableSql = $"CREATE TABLE \"{schema}\".\"{tableName}\" ({columnDefinitions})";
            
            await using var createCmd = new NpgsqlCommand(createTableSql, conn);
            await createCmd.ExecuteNonQueryAsync();
        }
        else
        {
            // Aplicar modo de importación solo si la tabla existe
            if (mode == "1") // TRUNCATE
            {
                try
                {
                    var truncateCmd = new NpgsqlCommand($"TRUNCATE TABLE \"{schema}\".\"{tableName}\" CASCADE", conn);
                    await truncateCmd.ExecuteNonQueryAsync();
                }
                catch
                {
                    // Ignorar errores de TRUNCATE
                }
            }
            else if (mode == "3") // DROP & CREATE
            {
                var dropCmd = new NpgsqlCommand($"DROP TABLE IF EXISTS \"{schema}\".\"{tableName}\" CASCADE", conn);
                await dropCmd.ExecuteNonQueryAsync();
                
                // Recrear tabla
                var columnDefinitions = string.Join(", ", headers.Select(h => $"\"{h}\" TEXT"));
                var createTableSql = $"CREATE TABLE \"{schema}\".\"{tableName}\" ({columnDefinitions})";
                
                await using var createCmd = new NpgsqlCommand(createTableSql, conn);
                await createCmd.ExecuteNonQueryAsync();
            }
        }

        int rowsImported = 0;
        int errorsCount = 0;

        // Importar datos línea por línea
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            
            var values = ParseCsvLine(lines[i]);
            
            // Validación mejorada
            if (values.Length == 0)
            {
                Console.WriteLine($"\n      ⚠️ Fila {i}: vacía, saltando...");
                continue;
            }
            
            if (values.Length != headers.Length)
            {
                Console.WriteLine($"\n      ⚠️ Fila {i}: {values.Length} columnas encontradas, esperadas {headers.Length}, saltando...");
                errorsCount++;
                continue;
            }

            var valuePlaceholders = string.Join(", ", Enumerable.Range(1, values.Length).Select(n => $"@p{n}"));
            var insertSql = $"INSERT INTO \"{schema}\".\"{tableName}\" ({columnNames}) VALUES ({valuePlaceholders})";

            try
            {
                await using var cmd = new NpgsqlCommand(insertSql, conn);
                for (int j = 0; j < values.Length; j++)
                {
                    var value = values[j].Trim();
                    cmd.Parameters.AddWithValue($"@p{j + 1}", string.IsNullOrEmpty(value) ? DBNull.Value : (object)value);
                }

                await cmd.ExecuteNonQueryAsync();
                rowsImported++;
            }
            catch (Exception ex)
            {
                errorsCount++;
                if (errorsCount <= 3) // Solo mostrar los primeros 3 errores
                {
                    Console.WriteLine($"\n      ❌ Error fila {i}: {ex.Message}");
                    // Mostrar valores de la fila para debug
                    Console.WriteLine($"         Valores: {string.Join(" | ", values.Take(3).Select(v => v.Length > 20 ? v.Substring(0, 20) + "..." : v))}...");
                }
            }
        }

        if (errorsCount > 3)
        {
            Console.WriteLine($"\n      ⚠️ ... y {errorsCount - 3} errores más");
        }

        return rowsImported;
    }

    private static string[] ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        values.Add(current.ToString());
        return values.ToArray();
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
