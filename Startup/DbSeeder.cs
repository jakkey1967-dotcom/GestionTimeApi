using GestionTime.Domain.Auth;
using GestionTime.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace GestionTime.Api.Startup;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GestionTimeDbContext>();

        try
        {
            Log.Information("Verificando conexión a base de datos...");
            
            // Verificar si podemos conectar a la BD
            var canConnect = await db.Database.CanConnectAsync();
            if (!canConnect)
            {
                Log.Error("No se puede conectar a la base de datos");
                return;
            }

            Log.Information("Verificando estado de migraciones...");
            
            // Verificar si hay migraciones pendientes
            var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
            var appliedMigrations = await db.Database.GetAppliedMigrationsAsync();
            
            Log.Information("Migraciones aplicadas: {Applied}", appliedMigrations.Count());
            Log.Information("Migraciones pendientes: {Pending}", pendingMigrations.Count());
            
            if (pendingMigrations.Any())
            {
                Log.Information("Aplicando migraciones pendientes...");
                await db.Database.MigrateAsync();
                Log.Information("Migraciones aplicadas correctamente");
            }
            else
            {
                Log.Information("No hay migraciones pendientes");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error durante las migraciones: {Message}", ex.Message);
            
            // Si es el error específico de tabla existente, intentamos continuar
            if (ex.Message.Contains("already exists") && ex.Message.Contains("__EFMigrationsHistory"))
            {
                Log.Warning("La tabla de migraciones ya existe, continuando con la verificación de datos...");
            }
            else
            {
                // Para otros errores, re-lanzar
                throw;
            }
        }

        // ⚠️ IMPORTANTE: Todos los datos (Usuarios, Roles, Catálogos) son datos REALES
        // copiados exactamente desde la BD local. NO modificar ni sobrescribir.
        
        Log.Information("Verificando datos reales copiados desde localhost...");
        
        try
        {
            var rolesCount = await db.Roles.CountAsync();
            var usersCount = await db.Users.CountAsync();
            var tiposCount = await db.Tipos.CountAsync();
            var gruposCount = await db.Grupos.CountAsync(); 
            var clientesCount = await db.Clientes.CountAsync();
            
            Log.Information("Datos reales existentes:");
            Log.Information("- Roles: {RolesCount}", rolesCount);
            Log.Information("- Usuarios: {UsersCount}", usersCount);
            Log.Information("- Tipos: {TiposCount}", tiposCount);
            Log.Information("- Grupos: {GruposCount}", gruposCount);
            Log.Information("- Clientes: {ClientesCount}", clientesCount);

            // Verificar que los usuarios principales existen
            var adminCount = await db.Users.CountAsync(u => u.Email == "admin@gestiontime.local");
            var psantosCount = await db.Users.CountAsync(u => u.Email == "psantos@global-retail.com");
            var tecnicoCount = await db.Users.CountAsync(u => u.Email == "tecnico1@global-retail.com");
            
            Log.Information("Usuarios específicos:");
            Log.Information("- admin@gestiontime.local: {AdminExists}", adminCount > 0 ? "✓" : "✗");
            Log.Information("- psantos@global-retail.com: {PsantosExists}", psantosCount > 0 ? "✓" : "✗");
            Log.Information("- tecnico1@global-retail.com: {TecnicoExists}", tecnicoCount > 0 ? "✓" : "✗");

            Log.Information("Seed de datos completado - Todos los datos reales preservados");
            Log.Information("⚠️  NO se crean usuarios adicionales - usando usuarios reales de localhost");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al verificar datos existentes: {Message}", ex.Message);
            Log.Warning("Continuando con el inicio de la aplicación...");
        }
    }
}

