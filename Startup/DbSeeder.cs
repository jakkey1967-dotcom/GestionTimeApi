using GestionTime.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Serilog;

namespace GestionTime.Api.Startup;

public static class DbSeeder
{
    private const long MIGRATION_LOCK_ID = 1234567890;
    
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GestionTimeDbContext>();

        try
        {
            Log.Information("🔌 Verificando conexión a base de datos...");
            
            var canConnect = await db.Database.CanConnectAsync();
            if (!canConnect)
            {
                Log.Error("❌ No se puede conectar a la base de datos");
                return;
            }

            Log.Information("✅ Conexión establecida");

            // Intentar adquirir lock de PostgreSQL
            var lockAcquired = await TryAcquireAdvisoryLockAsync(db);
            
            if (!lockAcquired)
            {
                Log.Information("⏳ Otra instancia está migrando. Esperando 5 segundos...");
                await Task.Delay(5000);
                
                await VerifyMigrationsAsync(db);
                await VerifyExistingDataAsync(db);
                return;
            }

            Log.Information("🔒 Lock de migraciones adquirido");
            
            try
            {
                await ApplyMigrationsAsync(db);
            }
            finally
            {
                await ReleaseAdvisoryLockAsync(db);
                Log.Information("🔓 Lock liberado");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "❌ Error en proceso de seed");
            
            if (IsDuplicateTableError(ex))
            {
                Log.Warning("⚠️ Tabla duplicada - Verificando estado...");
                await VerifyMigrationsAsync(db);
            }
            else
            {
                throw;
            }
        }

        await VerifyExistingDataAsync(db);
    }

    private static async Task ApplyMigrationsAsync(GestionTimeDbContext db)
    {
        try
        {
            var appliedMigrations = await db.Database.GetAppliedMigrationsAsync();
            var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
            var allMigrations = db.Database.GetMigrations().ToList();
            
            Log.Information("📊 Estado de migraciones:");
            Log.Information("  • Total en código: {Total}", allMigrations.Count);
            Log.Information("  • Aplicadas en BD: {Applied}", appliedMigrations.Count());
            Log.Information("  • Pendientes: {Pending}", pendingMigrations.Count());

            if (!pendingMigrations.Any())
            {
                Log.Information("✅ Base de datos actualizada");
                return;
            }

            // 🔴 DETECTAR: Tabla de historial vacía pero BD con datos
            if (!appliedMigrations.Any() && pendingMigrations.Any())
            {
                Log.Warning("⚠️ Tabla __EFMigrationsHistory vacía detectada");
                
                var tablesExist = await CheckIfTablesExistAsync(db);
                
                if (tablesExist)
                {
                    Log.Information("✅ Las tablas ya existen. Registrando migraciones...");
                    await ForceRegisterMigrationsAsync(db, allMigrations);
                    Log.Information("✅ Migraciones registradas exitosamente");
                    return;
                }
                else
                {
                    Log.Warning("⚠️ BD vacía. Limpiando tabla de historial corrupta...");
                    await db.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS \"__EFMigrationsHistory\";");
                }
            }

            Log.Information("🔄 Aplicando {Count} migraciones...", pendingMigrations.Count());
            await db.Database.MigrateAsync();
            Log.Information("✅ Migraciones aplicadas correctamente");
        }
        catch (PostgresException pgEx) when (pgEx.SqlState == "42P07")
        {
            Log.Warning("⚠️ Error 42P07: Tabla duplicada");
            await HandleDuplicateTableErrorAsync(db);
        }
        catch (Exception ex) when (ex.InnerException is PostgresException inner && inner.SqlState == "42P07")
        {
            Log.Warning("⚠️ Error 42P07 en excepción interna");
            await HandleDuplicateTableErrorAsync(db);
        }
    }

    private static async Task<bool> CheckIfTablesExistAsync(GestionTimeDbContext db)
    {
        try
        {
            // Verificar si las tablas principales existen
            var query = @"
                SELECT COUNT(*) 
                FROM information_schema.tables 
                WHERE table_schema = 'gestiontime' 
                AND table_name IN ('users', 'roles', 'tipo', 'grupo', 'cliente')";
            
            using var connection = db.Database.GetDbConnection();
            await connection.OpenAsync();
            
            using var command = connection.CreateCommand();
            command.CommandText = query;
            
            var result = await command.ExecuteScalarAsync();
            var count = Convert.ToInt32(result);
            
            Log.Information("  • Tablas encontradas: {Count}/5", count);
            
            return count >= 5;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "⚠️ Error verificando tablas existentes");
            return false;
        }
    }

    private static async Task ForceRegisterMigrationsAsync(GestionTimeDbContext db, List<string> migrations)
    {
        try
        {
            var productVersion = "10.0.1";
            
            foreach (var migration in migrations)
            {
                var query = @$"
                    INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
                    VALUES ('{migration}', '{productVersion}')
                    ON CONFLICT (""MigrationId"") DO NOTHING";
                
                await db.Database.ExecuteSqlRawAsync(query);
                Log.Information("  ✓ {Migration}", migration);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "❌ Error registrando migraciones");
            throw;
        }
    }

    private static async Task<bool> TryAcquireAdvisoryLockAsync(GestionTimeDbContext db)
    {
        try
        {
            using var connection = db.Database.GetDbConnection();
            await connection.OpenAsync();
            
            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT pg_try_advisory_lock({MIGRATION_LOCK_ID})";
            
            var result = await command.ExecuteScalarAsync();
            return Convert.ToBoolean(result);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "⚠️ Error adquiriendo lock");
            return false;
        }
    }

    private static async Task ReleaseAdvisoryLockAsync(GestionTimeDbContext db)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync(
                $"SELECT pg_advisory_unlock({MIGRATION_LOCK_ID})");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "⚠️ Error liberando lock");
        }
    }

    private static async Task HandleDuplicateTableErrorAsync(GestionTimeDbContext db)
    {
        try
        {
            Log.Information("🔍 Esperando 5 segundos...");
            await Task.Delay(5000);
            
            var pending = await db.Database.GetPendingMigrationsAsync();
            
            if (pending.Any())
            {
                Log.Warning("⚠️ Aún hay {Count} migraciones pendientes", pending.Count());
            }
            else
            {
                Log.Information("✅ Migraciones completadas por otra instancia");
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "⚠️ Error verificando estado");
        }
    }

    private static async Task VerifyMigrationsAsync(GestionTimeDbContext db)
    {
        try
        {
            var pending = await db.Database.GetPendingMigrationsAsync();
            
            if (pending.Any())
            {
                Log.Warning("⚠️ Migraciones pendientes: {Count}", pending.Count());
                foreach (var migration in pending)
                {
                    Log.Warning("  • {Migration}", migration);
                }
            }
            else
            {
                Log.Information("✅ Base de datos actualizada");
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "⚠️ Error verificando migraciones");
        }
    }

    private static async Task VerifyExistingDataAsync(GestionTimeDbContext db)
    {
        Log.Information("📋 Verificando datos...");
        
        try
        {
            var rolesCount = await db.Roles.CountAsync();
            var usersCount = await db.Users.CountAsync();
            var tiposCount = await db.Tipos.CountAsync();
            var gruposCount = await db.Grupos.CountAsync(); 
            var clientesCount = await db.Clientes.CountAsync();
            
            Log.Information("📊 Datos:");
            Log.Information("  • Roles: {Count}", rolesCount);
            Log.Information("  • Usuarios: {Count}", usersCount);
            Log.Information("  • Tipos: {Count}", tiposCount);
            Log.Information("  • Grupos: {Count}", gruposCount);
            Log.Information("  • Clientes: {Count}", clientesCount);

            if (usersCount > 0)
            {
                var adminExists = await db.Users.AnyAsync(u => u.Email == "admin@gestiontime.local");
                var psantosExists = await db.Users.AnyAsync(u => u.Email == "psantos@global-retail.com");
                var tecnicoExists = await db.Users.AnyAsync(u => u.Email == "tecnico1@global-retail.com");
                
                Log.Information("👥 Usuarios:");
                Log.Information("  • admin: {Status}", adminExists ? "✅" : "❌");
                Log.Information("  • psantos: {Status}", psantosExists ? "✅" : "❌");
                Log.Information("  • tecnico1: {Status}", tecnicoExists ? "✅" : "❌");
            }
            else
            {
                Log.Warning("⚠️ No hay usuarios");
            }
            
            Log.Information("✅ Verificación completada");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "❌ Error verificando datos");
        }
    }

    private static bool IsDuplicateTableError(Exception ex)
    {
        if (ex is PostgresException pgEx && pgEx.SqlState == "42P07")
            return true;
            
        if (ex.InnerException is PostgresException inner && inner.SqlState == "42P07")
            return true;

        var message = ex.Message.ToLowerInvariant();
        return message.Contains("already exists") || message.Contains("42p07");
    }
}

