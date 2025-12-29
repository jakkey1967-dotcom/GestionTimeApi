using GestionTime.Domain.Auth;
using GestionTime.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Serilog;

namespace GestionTime.Api.Startup;

public static class DbSeeder
{
    private static readonly SemaphoreSlim _migrationLock = new(1, 1);
    
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

            // Bloquear para evitar múltiples instancias migrando
            await _migrationLock.WaitAsync(TimeSpan.FromMinutes(2));
            
            try
            {
                await ApplyMigrationsAsync(db);
            }
            finally
            {
                _migrationLock.Release();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "❌ Error en proceso de seed");
            
            // Si es error de tabla duplicada, continuar
            if (IsDuplicateTableError(ex))
            {
                Log.Warning("⚠️ Tabla de migraciones duplicada - Verificando estado...");
                await VerifyMigrationsAsync(db);
            }
            else
            {
                throw;
            }
        }

        // Verificar datos
        await VerifyExistingDataAsync(db);
    }

    private static async Task ApplyMigrationsAsync(GestionTimeDbContext db)
    {
        try
        {
            var appliedMigrations = await db.Database.GetAppliedMigrationsAsync();
            var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
            
            Log.Information("📊 Estado de migraciones:");
            Log.Information("  • Aplicadas: {Applied}", appliedMigrations.Count());
            Log.Information("  • Pendientes: {Pending}", pendingMigrations.Count());

            if (!pendingMigrations.Any())
            {
                Log.Information("✅ Base de datos actualizada");
                return;
            }

            // 🔴 DETECTAR TABLA CORRUPTA: 0 migraciones aplicadas pero tabla existe
            if (!appliedMigrations.Any() && pendingMigrations.Any())
            {
                Log.Warning("⚠️ Detectado: 0 migraciones aplicadas pero tabla existe");
                Log.Information("🔧 Intentando limpiar y recrear tabla de migraciones...");
                
                try
                {
                    // Intentar eliminar la tabla corrupta
                    await db.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS \"__EFMigrationsHistory\";");
                    Log.Information("✅ Tabla de migraciones limpiada");
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "⚠️ No se pudo limpiar tabla de migraciones, continuando...");
                }
            }

            Log.Information("🔄 Aplicando {Count} migraciones...", pendingMigrations.Count());
            await db.Database.MigrateAsync();
            Log.Information("✅ Migraciones aplicadas correctamente");
        }
        catch (PostgresException pgEx) when (pgEx.SqlState == "42P07")
        {
            Log.Warning("⚠️ Error 42P07 (tabla duplicada) capturado");
            await HandleDuplicateTableErrorAsync(db);
        }
        catch (Exception ex) when (ex.InnerException is PostgresException inner && inner.SqlState == "42P07")
        {
            Log.Warning("⚠️ Error 42P07 en excepción interna");
            await HandleDuplicateTableErrorAsync(db);
        }
    }

    private static async Task HandleDuplicateTableErrorAsync(GestionTimeDbContext db)
    {
        try
        {
            Log.Information("🔍 Verificando estado real de migraciones...");
            
            // Esperar un momento para que otras instancias terminen
            await Task.Delay(2000);
            
            var pending = await db.Database.GetPendingMigrationsAsync();
            
            if (pending.Any())
            {
                Log.Warning("⚠️ Hay {Count} migraciones pendientes después del error", pending.Count());
                Log.Information("🔄 Reintentando aplicar migraciones...");
                
                try
                {
                    await db.Database.MigrateAsync();
                    Log.Information("✅ Migraciones aplicadas en segundo intento");
                }
                catch
                {
                    Log.Error("❌ No se pudieron aplicar migraciones en segundo intento");
                }
            }
            else
            {
                Log.Information("✅ Todas las migraciones están aplicadas");
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "⚠️ Error verificando estado post-error, continuando");
        }
    }

    private static async Task VerifyMigrationsAsync(GestionTimeDbContext db)
    {
        try
        {
            var pending = await db.Database.GetPendingMigrationsAsync();
            
            if (pending.Any())
            {
                Log.Warning("⚠️ Quedan {Count} migraciones pendientes", pending.Count());
            }
            else
            {
                Log.Information("✅ Migraciones verificadas correctamente");
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "⚠️ No se pudo verificar migraciones");
        }
    }

    private static async Task VerifyExistingDataAsync(GestionTimeDbContext db)
    {
        Log.Information("📋 Verificando datos en base de datos...");
        
        try
        {
            var rolesCount = await db.Roles.CountAsync();
            var usersCount = await db.Users.CountAsync();
            var tiposCount = await db.Tipos.CountAsync();
            var gruposCount = await db.Grupos.CountAsync(); 
            var clientesCount = await db.Clientes.CountAsync();
            
            Log.Information("📊 Resumen de datos:");
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
                
                Log.Information("👥 Usuarios clave:");
                Log.Information("  • admin@gestiontime.local: {Status}", adminExists ? "✅" : "❌");
                Log.Information("  • psantos@global-retail.com: {Status}", psantosExists ? "✅" : "❌");
                Log.Information("  • tecnico1@global-retail.com: {Status}", tecnicoExists ? "✅" : "❌");
            }
            else
            {
                Log.Warning("⚠️ No hay usuarios en la base de datos");
            }
            
            Log.Information("✅ Verificación completada");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "❌ Error verificando datos: {Message}", ex.Message);
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

