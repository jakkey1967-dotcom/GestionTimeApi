using Npgsql;
using System.Text;

namespace GestionTime.Api.Tools;

/// <summary>
/// Herramienta para sincronizar datos directamente entre bases de datos PostgreSQL
/// Uso: 
///   dotnet run -- sync-schema
/// </summary>
public class SyncSchema
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine();
        Console.WriteLine("????????????????????????????????????????????????????????????");
        Console.WriteLine("?       ?? SINCRONIZADOR DE SCHEMAS POSTGRESQL ??         ?");
        Console.WriteLine("????????????????????????????????????????????????????????????");
        Console.WriteLine();

        // ORIGEN: Render (PostgreSQL remoto)
        var sourceConnectionString = "Host=dpg-d57tobm3jp1c73b6i4ug-a.frankfurt-postgres.render.com;Port=5432;Database=pss_dvnx;Username=gestiontime;Password=BvCDRFguh9SljJJUZOzGpdvpxgf18qnI;SSL Mode=Require;Trust Server Certificate=true";

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("?? ORIGEN: Render (PostgreSQL remoto)");
        Console.ResetColor();
        Console.WriteLine();

        // Conectar primero para detectar schemas
        Console.WriteLine("?? Detectando schemas disponibles en Render...");
        await using var testConn = new NpgsqlConnection(sourceConnectionString);
        await testConn.OpenAsync();

        var schemasQuery = @"
            SELECT table_schema, COUNT(table_name) as table_count
            FROM information_schema.tables 
            WHERE table_schema NOT IN ('pg_catalog', 'information_schema', 'pg_toast')
              AND table_type = 'BASE TABLE'
            GROUP BY table_schema
            ORDER BY table_schema";

        var availableSchemas = new List<(string Name, int TableCount)>();
        await using (var cmd = new NpgsqlCommand(schemasQuery, testConn))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                availableSchemas.Add((reader.GetString(0), reader.GetInt32(1)));
            }
        }

        if (availableSchemas.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("? No se encontraron schemas con tablas en Render");
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"? Schemas encontrados:");
        Console.ResetColor();
        foreach (var (name, count) in availableSchemas)
        {
            Console.WriteLine($"   • {name}: {count} tabla(s)");
        }
        Console.WriteLine();

        // Usar el schema con más tablas
        var sourceSchema = availableSchemas.OrderByDescending(s => s.TableCount).First().Name;
        
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"?? Schema seleccionado: {sourceSchema} ({availableSchemas.First(s => s.Name == sourceSchema).TableCount} tablas)");
        Console.ResetColor();
        Console.WriteLine();

        // DESTINO: Localhost
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("?? DESTINO: Localhost (gtdefault)");
        Console.ResetColor();
        Console.WriteLine();

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

        var destConnectionString = $"Host=localhost;Port={port};Database={database};Username={username};Password={password};SSL Mode=Disable";
        var destSchema = "gtdefault"; // Schema de destino fijo

        Console.WriteLine();

        await SyncDatabases(sourceConnectionString, sourceSchema, destConnectionString, destSchema);
    }

    private static async Task SyncDatabases(string sourceConnStr, string sourceSchema, string destConnStr, string destSchema)
    {
        try
        {
            // Conectar a ambas bases de datos
            await using var sourceConn = new NpgsqlConnection(sourceConnStr);
            await using var destConn = new NpgsqlConnection(destConnStr);

            Console.WriteLine("?? Conectando a bases de datos...");
            
            await sourceConn.OpenAsync();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("? Conectado a ORIGEN (Render)");
            Console.ResetColor();

            await destConn.OpenAsync();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("? Conectado a DESTINO (Localhost)");
            Console.ResetColor();
            Console.WriteLine();

            // Crear schema de destino si no existe
            Console.WriteLine($"?? Verificando schema '{destSchema}'...");
            var createSchemaCmd = new NpgsqlCommand($"CREATE SCHEMA IF NOT EXISTS \"{destSchema}\"", destConn);
            await createSchemaCmd.ExecuteNonQueryAsync();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("? Schema listo");
            Console.ResetColor();
            Console.WriteLine();

            // Obtener lista de tablas del origen
            Console.WriteLine($"?? Obteniendo tablas de '{sourceSchema}'...");
            var tablesQuery = $@"
                SELECT table_name 
                FROM information_schema.tables 
                WHERE table_schema = '{sourceSchema}' 
                  AND table_type = 'BASE TABLE'
                ORDER BY table_name";

            var tables = new List<string>();
            await using (var cmd = new NpgsqlCommand(tablesQuery, sourceConn))
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    tables.Add(reader.GetString(0));
                }
            }

            if (tables.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"??  No se encontraron tablas en schema '{sourceSchema}'");
                Console.ResetColor();
                return;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"? Encontradas {tables.Count} tabla(s)");
            Console.ResetColor();
            Console.WriteLine();

            // Modo de sincronización
            Console.WriteLine("Modo de sincronización:");
            Console.WriteLine("  1. TRUNCATE - Vaciar tablas destino y copiar (RECOMENDADO)");
            Console.WriteLine("  2. APPEND - Agregar datos (puede causar duplicados)");
            Console.WriteLine("  3. DROP & CREATE - Eliminar y recrear tablas");
            Console.WriteLine();
            Console.Write("Selecciona modo [1]: ");
            var modeInput = Console.ReadLine()?.Trim();
            var mode = string.IsNullOrWhiteSpace(modeInput) ? "1" : modeInput;
            Console.WriteLine();

            var summary = new StringBuilder();
            summary.AppendLine("SINCRONIZACIÓN DE SCHEMAS");
            summary.AppendLine("=============================");
            summary.AppendLine($"Fecha: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            summary.AppendLine($"Origen: {sourceSchema} (Render)");
            summary.AppendLine($"Destino: {destSchema} (Localhost)");
            summary.AppendLine($"Total de tablas: {tables.Count}");
            summary.AppendLine($"Modo: {(mode == "1" ? "TRUNCATE" : mode == "2" ? "APPEND" : "DROP & CREATE")}");
            summary.AppendLine();

            int contador = 0;
            int exitosos = 0;
            int fallidos = 0;

            foreach (var tableName in tables)
            {
                contador++;
                Console.Write($"[{contador}/{tables.Count}] {tableName}...");

                try
                {
                    var rowsCopied = await SyncTable(sourceConn, sourceSchema, destConn, destSchema, tableName, mode);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($" ? {rowsCopied} registros");
                    Console.ResetColor();

                    summary.AppendLine($"? {tableName}: {rowsCopied} registros");
                    exitosos++;
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($" ? ERROR");
                    Console.ResetColor();
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine($"   {ex.Message}");
                    Console.ResetColor();

                    summary.AppendLine($"? {tableName}: {ex.Message}");
                    fallidos++;
                }
            }

            summary.AppendLine();
            summary.AppendLine("RESUMEN:");
            summary.AppendLine($"? Exitosos: {exitosos}");
            summary.AppendLine($"? Fallidos: {fallidos}");

            // Guardar log
            var logPath = Path.Combine(Directory.GetCurrentDirectory(), $"SYNC_LOG_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            await File.WriteAllTextAsync(logPath, summary.ToString());

            // Resumen final
            Console.WriteLine();
            Console.WriteLine("????????????????????????????????????????????????????????????");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("?             ? SINCRONIZACIÓN COMPLETADA ?               ?");
            Console.ResetColor();
            Console.WriteLine("????????????????????????????????????????????????????????????");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"? Exitosos: {exitosos}");
            Console.ResetColor();

            if (fallidos > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"? Fallidos: {fallidos}");
                Console.ResetColor();
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"?? Log: {logPath}");
            Console.ResetColor();
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

    private static async Task<int> SyncTable(NpgsqlConnection sourceConn, string sourceSchema, NpgsqlConnection destConn, string destSchema, string tableName, string mode)
    {
        // Obtener estructura de la tabla de origen
        var columnsQuery = $@"
            SELECT column_name, data_type, character_maximum_length
            FROM information_schema.columns
            WHERE table_schema = '{sourceSchema}' AND table_name = '{tableName}'
            ORDER BY ordinal_position";

        var columns = new List<(string Name, string Type)>();
        await using (var cmd = new NpgsqlCommand(columnsQuery, sourceConn))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                var colName = reader.GetString(0);
                var dataType = reader.GetString(1);
                var maxLength = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2);

                // Simplificar tipos para compatibilidad
                var simpleType = dataType.ToLower() switch
                {
                    "character varying" => maxLength.HasValue ? $"VARCHAR({maxLength})" : "TEXT",
                    "timestamp without time zone" => "TIMESTAMP",
                    "timestamp with time zone" => "TIMESTAMPTZ",
                    _ => dataType.ToUpper()
                };

                columns.Add((colName, simpleType));
            }
        }

        // Verificar si la tabla existe en destino
        var tableExistsQuery = $@"
            SELECT EXISTS (
                SELECT FROM information_schema.tables 
                WHERE table_schema = '{destSchema}' 
                AND table_name = '{tableName}'
            )";

        await using var checkCmd = new NpgsqlCommand(tableExistsQuery, destConn);
        var tableExists = (bool)(await checkCmd.ExecuteScalarAsync() ?? false);

        if (!tableExists)
        {
            // Crear tabla en destino
            var columnDefinitions = string.Join(", ", columns.Select(c => $"\"{c.Name}\" {c.Type}"));
            var createTableSql = $"CREATE TABLE \"{destSchema}\".\"{tableName}\" ({columnDefinitions})";

            await using var createCmd = new NpgsqlCommand(createTableSql, destConn);
            await createCmd.ExecuteNonQueryAsync();
        }
        else
        {
            // Aplicar modo de sincronización
            if (mode == "1") // TRUNCATE
            {
                try
                {
                    var truncateCmd = new NpgsqlCommand($"TRUNCATE TABLE \"{destSchema}\".\"{tableName}\" CASCADE", destConn);
                    await truncateCmd.ExecuteNonQueryAsync();
                }
                catch { }
            }
            else if (mode == "3") // DROP & CREATE
            {
                var dropCmd = new NpgsqlCommand($"DROP TABLE IF EXISTS \"{destSchema}\".\"{tableName}\" CASCADE", destConn);
                await dropCmd.ExecuteNonQueryAsync();

                var columnDefinitions = string.Join(", ", columns.Select(c => $"\"{c.Name}\" {c.Type}"));
                var createTableSql = $"CREATE TABLE \"{destSchema}\".\"{tableName}\" ({columnDefinitions})";

                await using var createCmd = new NpgsqlCommand(createTableSql, destConn);
                await createCmd.ExecuteNonQueryAsync();
            }
        }

        // Copiar datos
        var columnNames = string.Join(", ", columns.Select(c => $"\"{c.Name}\""));
        var selectQuery = $"SELECT {columnNames} FROM \"{sourceSchema}\".\"{tableName}\"";

        int rowsCopied = 0;

        await using (var selectCmd = new NpgsqlCommand(selectQuery, sourceConn))
        await using (var reader = await selectCmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                var values = new object[reader.FieldCount];
                reader.GetValues(values);

                var valuePlaceholders = string.Join(", ", Enumerable.Range(1, values.Length).Select(n => $"@p{n}"));
                var insertSql = $"INSERT INTO \"{destSchema}\".\"{tableName}\" ({columnNames}) VALUES ({valuePlaceholders})";

                await using var insertCmd = new NpgsqlCommand(insertSql, destConn);
                for (int i = 0; i < values.Length; i++)
                {
                    insertCmd.Parameters.AddWithValue($"@p{i + 1}", values[i] == DBNull.Value ? DBNull.Value : values[i]);
                }

                try
                {
                    await insertCmd.ExecuteNonQueryAsync();
                    rowsCopied++;
                }
                catch
                {
                    // Ignorar errores de inserción individual (duplicados, etc.)
                }
            }
        }

        return rowsCopied;
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
