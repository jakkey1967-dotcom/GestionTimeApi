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

        Log.Information("Aplicando migraciones de base de datos...");
        await db.Database.MigrateAsync();
        Log.Information("Migraciones aplicadas correctamente");

        // ⚠️ IMPORTANTE: Todos los datos (Usuarios, Roles, Catálogos) son datos REALES
        // copiados exactamente desde la BD local. NO modificar ni sobrescribir.
        
        Log.Information("Verificando datos reales copiados desde localhost...");
        
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
}

