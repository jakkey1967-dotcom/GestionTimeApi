using Npgsql;

namespace GestionTime.Api.Tools;

/// <summary>
/// Herramienta de diagnóstico para verificar el estado de la base de datos de Render
/// Uso: dotnet run -- check-render
/// </summary>
public class CheckRender
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine();
        Console.WriteLine("????????????????????????????????????????????????????????????");
        Console.WriteLine("?       ?? DIAGNÓSTICO BASE DE DATOS RENDER ??            ?");
        Console.WriteLine("????????????????????????????????????????????????????????????");
        Console.WriteLine();

        var connectionString = "Host=dpg-d57tobm3jp1c73b6i4ug-a.frankfurt-postgres.render.com;Port=5432;Database=pss_dvnx;Username=gestiontime;Password=BvCDRFguh9SljJJUZOzGpdvpxgf18qnI;SSL Mode=Require;Trust Server Certificate=true";

        try
        {
            await using var conn = new NpgsqlConnection(connectionString);
            
            Console.WriteLine("?? Conectando a Render...");
            await conn.OpenAsync();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("? Conectado exitosamente");
            Console.ResetColor();
            Console.WriteLine();

            // 1. Listar todos los schemas
            Console.WriteLine("?? SCHEMAS DISPONIBLES:");
            Console.WriteLine("?????????????????????????????????????????");
            var schemasQuery = @"
                SELECT schema_name 
                FROM information_schema.schemata 
                WHERE schema_name NOT IN ('pg_catalog', 'information_schema', 'pg_toast')
                ORDER BY schema_name";

            await using (var cmd = new NpgsqlCommand(schemasQuery, conn))
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                int count = 0;
                while (await reader.ReadAsync())
                {
                    count++;
                    Console.WriteLine($"  {count}. {reader.GetString(0)}");
                }
                
                if (count == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("  ?? No se encontraron schemas personalizados");
                    Console.ResetColor();
                }
            }
            Console.WriteLine();

            // 2. Listar tablas por schema
            Console.WriteLine("?? TABLAS POR SCHEMA:");
            Console.WriteLine("?????????????????????????????????????????");
            var tablesQuery = @"
                SELECT table_schema, table_name, 
                       (SELECT COUNT(*) FROM information_schema.columns 
                        WHERE columns.table_schema = tables.table_schema 
                          AND columns.table_name = tables.table_name) as column_count
                FROM information_schema.tables
                WHERE table_schema NOT IN ('pg_catalog', 'information_schema', 'pg_toast')
                  AND table_type = 'BASE TABLE'
                ORDER BY table_schema, table_name";

            var schemaGroups = new Dictionary<string, List<(string TableName, int ColumnCount)>>();
            
            await using (var cmd = new NpgsqlCommand(tablesQuery, conn))
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var schema = reader.GetString(0);
                    var tableName = reader.GetString(1);
                    var columnCount = Convert.ToInt32(reader.GetValue(2));

                    if (!schemaGroups.ContainsKey(schema))
                        schemaGroups[schema] = new List<(string, int)>();

                    schemaGroups[schema].Add((tableName, columnCount));
                }
            }

            if (schemaGroups.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("? NO HAY TABLAS EN LA BASE DE DATOS");
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine("?? Posibles causas:");
                Console.WriteLine("   1. La base de datos está vacía");
                Console.WriteLine("   2. Las tablas están en un schema diferente");
                Console.WriteLine("   3. No tienes permisos para ver las tablas");
            }
            else
            {
                foreach (var (schema, tables) in schemaGroups.OrderBy(g => g.Key))
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"\n?? Schema: {schema} ({tables.Count} tablas)");
                    Console.ResetColor();
                    
                    foreach (var (tableName, columnCount) in tables)
                    {
                        Console.WriteLine($"   • {tableName} ({columnCount} columnas)");
                        
                        // Obtener conteo de registros
                        try
                        {
                            var countQuery = $"SELECT COUNT(*) FROM \"{schema}\".\"{tableName}\"";
                            await using var countCmd = new NpgsqlCommand(countQuery, conn);
                            var rowCount = (long)(await countCmd.ExecuteScalarAsync() ?? 0);
                            
                            Console.ForegroundColor = rowCount > 0 ? ConsoleColor.Green : ConsoleColor.Gray;
                            Console.WriteLine($"     ? {rowCount} registros");
                            Console.ResetColor();
                        }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"     ? Error al contar: {ex.Message}");
                            Console.ResetColor();
                        }
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine("?????????????????????????????????????????");
            
            // 3. Información de la base de datos
            Console.WriteLine();
            Console.WriteLine("??  INFORMACIÓN GENERAL:");
            Console.WriteLine("?????????????????????????????????????????");
            
            var dbInfoQuery = "SELECT version()";
            await using (var cmd = new NpgsqlCommand(dbInfoQuery, conn))
            {
                var version = await cmd.ExecuteScalarAsync();
                Console.WriteLine($"PostgreSQL: {version?.ToString()?.Split(',')[0]}");
            }

            var dbSizeQuery = "SELECT pg_size_pretty(pg_database_size(current_database()))";
            await using (var cmd2 = new NpgsqlCommand(dbSizeQuery, conn))
            {
                var size = await cmd2.ExecuteScalarAsync();
                Console.WriteLine($"Tamaño BD: {size}");
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("? Diagnóstico completado");
            Console.ResetColor();
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"? ERROR: {ex.Message}");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine(ex.StackTrace);
        }
    }
}
