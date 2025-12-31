using Npgsql;
using System.Diagnostics;
using System.Text;

namespace GestionTime.Api.Tools;

/// <summary>
/// Herramienta automatizada para deployment completo a Render con verificación
/// Uso: dotnet run -- deploy-render
/// </summary>
public class DeployToRender
{
    private static readonly string RenderConnectionString = 
        "Host=dpg-d57tobm3jp1c73b6i4ug-a.frankfurt-postgres.render.com;Port=5432;Database=pss_dvnx;Username=gestiontime;Password=BvCDRFguh9SljJJUZOzGpdvpxgf18qnI;SSL Mode=Require;Trust Server Certificate=true";
    
    private static readonly string RenderApiUrl = "https://gestiontime-api.onrender.com";

    public static async Task Main(string[] args)
    {
        Console.WriteLine();
        Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║       🚀 DEPLOYMENT AUTOMATIZADO A RENDER 🚀            ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        var startTime = DateTime.Now;
        var log = new StringBuilder();
        log.AppendLine("=".PadRight(60, '='));
        log.AppendLine($"DEPLOYMENT A RENDER - {startTime:yyyy-MM-dd HH:mm:ss}");
        log.AppendLine("=".PadRight(60, '='));
        log.AppendLine();

        try
        {
            // PASO 1: Verificar estado Git
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("📋 PASO 1: Verificando repositorio Git...");
            Console.ResetColor();
            
            if (!await VerifyGitStatus(log))
            {
                SaveLog(log.ToString());
                return;
            }

            // PASO 2: Hacer commit y push
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("📦 PASO 2: Commit y Push a GitHub...");
            Console.ResetColor();
            
            var commitMessage = args.Length > 0 ? string.Join(" ", args) : "Deploy: Update application";
            if (!await CommitAndPush(commitMessage, log))
            {
                SaveLog(log.ToString());
                return;
            }

            // PASO 3: Verificar estado de Render
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("🔍 PASO 3: Verificando estado de Render...");
            Console.ResetColor();
            
            await CheckRenderStatus(log);

            // PASO 4: Esperar deployment
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("⏳ PASO 4: Esperando deployment (esto puede tardar 5-7 minutos)...");
            Console.ResetColor();
            
            var deployed = await WaitForDeployment(log);

            // PASO 5: Verificar base de datos
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("🗄️ PASO 5: Verificando base de datos...");
            Console.ResetColor();
            
            var dbStatus = await VerifyDatabase(log);

            // PASO 6: Verificar API
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("🌐 PASO 6: Verificando API en línea...");
            Console.ResetColor();
            
            var apiStatus = await VerifyApi(log);

            // RESUMEN FINAL
            var endTime = DateTime.Now;
            var duration = endTime - startTime;

            Console.WriteLine();
            Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
            
            if (deployed && dbStatus && apiStatus)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("║           ✅ DEPLOYMENT EXITOSO ✅                       ║");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("║         ⚠️ DEPLOYMENT CON ADVERTENCIAS ⚠️               ║");
            }
            
            Console.ResetColor();
            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            log.AppendLine();
            log.AppendLine("RESUMEN FINAL:");
            log.AppendLine($"✅ Git Push: Exitoso");
            log.AppendLine($"{(deployed ? "✅" : "❌")} Deployment: {(deployed ? "Completado" : "Falló")}");
            log.AppendLine($"{(dbStatus ? "✅" : "❌")} Base de Datos: {(dbStatus ? "OK" : "Error")}");
            log.AppendLine($"{(apiStatus ? "✅" : "❌")} API: {(apiStatus ? "Online" : "Offline")}");
            log.AppendLine($"⏱️ Tiempo total: {duration.TotalMinutes:F1} minutos");
            log.AppendLine();
            log.AppendLine($"🌐 URL: {RenderApiUrl}");
            log.AppendLine($"📚 Swagger: {RenderApiUrl}/swagger");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("📊 RESULTADOS:");
            Console.ResetColor();
            Console.WriteLine($"  • Tiempo total: {duration.TotalMinutes:F1} minutos");
            Console.WriteLine($"  • URL: {RenderApiUrl}");
            Console.WriteLine($"  • Swagger: {RenderApiUrl}/swagger");
            Console.WriteLine();

            SaveLog(log.ToString());
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ ERROR CRÍTICO: {ex.Message}");
            Console.ResetColor();
            
            log.AppendLine();
            log.AppendLine("ERROR CRÍTICO:");
            log.AppendLine(ex.ToString());
            
            SaveLog(log.ToString());
        }
    }

    private static async Task<bool> VerifyGitStatus(StringBuilder log)
    {
        try
        {
            var branch = await RunCommand("git", "branch --show-current");
            var status = await RunCommand("git", "status --short");

            Console.WriteLine($"  🌿 Branch: {branch.Trim()}");
            
            if (!string.IsNullOrWhiteSpace(status))
            {
                Console.WriteLine($"  📝 Cambios detectados:");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(status.Trim().Split('\n').Take(5).Aggregate("", (a, b) => a + "     " + b + "\n"));
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine($"  ✅ No hay cambios pendientes");
            }

            log.AppendLine($"Branch: {branch.Trim()}");
            log.AppendLine($"Cambios: {(!string.IsNullOrWhiteSpace(status) ? "Sí" : "No")}");
            
            return true;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ❌ Error: {ex.Message}");
            Console.ResetColor();
            log.AppendLine($"ERROR Git: {ex.Message}");
            return false;
        }
    }

    private static async Task<bool> CommitAndPush(string message, StringBuilder log)
    {
        try
        {
            // Git add
            await RunCommand("git", "add -A");
            Console.WriteLine("  ✅ Archivos agregados");

            // Git commit
            var commitResult = await RunCommand("git", $"commit -m \"{message}\"");
            
            if (commitResult.Contains("nothing to commit"))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("  ⚠️ No hay cambios para commitear");
                Console.ResetColor();
                log.AppendLine("No hay cambios nuevos");
            }
            else
            {
                Console.WriteLine("  ✅ Commit realizado");
                log.AppendLine($"Commit: {message}");
            }

            // Git push
            Console.WriteLine("  📤 Pusheando a GitHub...");
            var pushResult = await RunCommand("git", "push origin main");
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  ✅ Push exitoso");
            Console.ResetColor();
            
            log.AppendLine("Push: Exitoso");
            
            return true;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ❌ Error: {ex.Message}");
            Console.ResetColor();
            log.AppendLine($"ERROR Push: {ex.Message}");
            return false;
        }
    }

    private static async Task CheckRenderStatus(StringBuilder log)
    {
        try
        {
            await using var conn = new NpgsqlConnection(RenderConnectionString);
            await conn.OpenAsync();
            
            Console.WriteLine("  ✅ Conexión a BD establecida");
            
            var tablesQuery = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE'";
            await using var cmd = new NpgsqlCommand(tablesQuery, conn);
            var tableCount = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);
            
            Console.WriteLine($"  📊 Tablas en BD: {tableCount}");
            log.AppendLine($"Tablas antes de deploy: {tableCount}");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  ⚠️ No se pudo verificar: {ex.Message}");
            Console.ResetColor();
        }
    }

    private static async Task<bool> WaitForDeployment(StringBuilder log)
    {
        var maxWaitTime = TimeSpan.FromMinutes(10);
        var checkInterval = TimeSpan.FromSeconds(30);
        var startWait = DateTime.Now;
        var attempts = 0;

        Console.WriteLine();
        
        while (DateTime.Now - startWait < maxWaitTime)
        {
            attempts++;
            var elapsed = (DateTime.Now - startWait).TotalMinutes;
            
            Console.Write($"  🔄 Intento {attempts} ({elapsed:F1} min)... ");
            
            try
            {
                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                var response = await httpClient.GetAsync($"{RenderApiUrl}/health");
                
                if (response.IsSuccessStatusCode)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✅ API respondiendo");
                    Console.ResetColor();
                    
                    log.AppendLine($"Deployment detectado en {elapsed:F1} minutos");
                    return true;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"⏳ Esperando... (Status: {(int)response.StatusCode})");
                    Console.ResetColor();
                }
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("⏳ Aún no disponible...");
                Console.ResetColor();
            }

            await Task.Delay(checkInterval);
        }

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("  ❌ Timeout esperando deployment");
        Console.ResetColor();
        
        log.AppendLine("Timeout: Deployment no detectado en 10 minutos");
        return false;
    }

    private static async Task<bool> VerifyDatabase(StringBuilder log)
    {
        try
        {
            await using var conn = new NpgsqlConnection(RenderConnectionString);
            await conn.OpenAsync();

            // Contar tablas
            var tablesQuery = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE'";
            await using var cmd = new NpgsqlCommand(tablesQuery, conn);
            var tableCount = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);

            Console.WriteLine($"  📊 Tablas: {tableCount}");

            if (tableCount == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("  ❌ No hay tablas en la base de datos");
                Console.ResetColor();
                log.AppendLine("BD: Vacía (0 tablas)");
                return false;
            }

            // Verificar tabla users tiene columnas correctas
            var columnsQuery = @"
                SELECT column_name 
                FROM information_schema.columns 
                WHERE table_schema = 'public' AND table_name = 'users'
                ORDER BY ordinal_position";
            
            var columns = new List<string>();
            await using var colCmd = new NpgsqlCommand(columnsQuery, conn);
            await using var reader = await colCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                columns.Add(reader.GetString(0));
            }

            var hasEmailConfirmed = columns.Contains("email_confirmed");
            
            Console.WriteLine($"  📋 Columnas en 'users': {columns.Count}");
            Console.WriteLine($"  {(hasEmailConfirmed ? "✅" : "❌")} Columna 'email_confirmed': {(hasEmailConfirmed ? "Presente" : "Falta")}");

            log.AppendLine($"BD: {tableCount} tablas, users: {columns.Count} columnas");
            log.AppendLine($"Migración completa: {hasEmailConfirmed}");

            return hasEmailConfirmed;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ❌ Error: {ex.Message}");
            Console.ResetColor();
            log.AppendLine($"ERROR BD: {ex.Message}");
            return false;
        }
    }

    private static async Task<bool> VerifyApi(StringBuilder log)
    {
        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            
            // Health check
            var healthResponse = await httpClient.GetAsync($"{RenderApiUrl}/health");
            var healthContent = await healthResponse.Content.ReadAsStringAsync();
            
            Console.WriteLine($"  🏥 Health: {(healthResponse.IsSuccessStatusCode ? "✅ OK" : "❌ Error")}");
            
            // Swagger
            var swaggerResponse = await httpClient.GetAsync($"{RenderApiUrl}/swagger/index.html");
            Console.WriteLine($"  📚 Swagger: {(swaggerResponse.IsSuccessStatusCode ? "✅ OK" : "❌ Error")}");

            log.AppendLine($"API Health: {healthResponse.StatusCode}");
            log.AppendLine($"Swagger: {swaggerResponse.StatusCode}");

            return healthResponse.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ❌ Error: {ex.Message}");
            Console.ResetColor();
            log.AppendLine($"ERROR API: {ex.Message}");
            return false;
        }
    }

    private static async Task<string> RunCommand(string command, string arguments)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
        {
            throw new Exception(error);
        }

        return output;
    }

    private static void SaveLog(string content)
    {
        var logPath = Path.Combine(
            Directory.GetCurrentDirectory(), 
            $"DEPLOY_LOG_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
        );
        
        File.WriteAllText(logPath, content);
        
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"📁 Log guardado: {logPath}");
        Console.ResetColor();
        Console.WriteLine();
    }
}
