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
            Log.Information("🔌 Verificando conexión a base de datos...");
            
            var canConnect = await db.Database.CanConnectAsync();
            if (!canConnect)
            {
                Log.Error("❌ No se puede conectar a la base de datos");
                return;
            }

            Log.Information("✅ Conexión establecida");
            
            // Verificar y crear datos iniciales
            await VerifyAndSeedDataAsync(db);
            
            Log.Information("✅ Verificación de datos completada");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "❌ Error en proceso de seed");
            throw;
        }
    }

    private static async Task VerifyAndSeedDataAsync(GestionTimeDbContext db)
    {
        Log.Information("📋 Verificando datos...");
        
        try
        {
            var rolesCount = await db.Roles.CountAsync();
            var usersCount = await db.Users.CountAsync();
            var tiposCount = await db.Tipos.CountAsync();
            var gruposCount = await db.Grupos.CountAsync(); 
            var clientesCount = await db.Clientes.CountAsync();
            
            Log.Information("📊 Estado actual:");
            Log.Information("  • Roles: {Count}", rolesCount);
            Log.Information("  • Usuarios: {Count}", usersCount);
            Log.Information("  • Tipos: {Count}", tiposCount);
            Log.Information("  • Grupos: {Count}", gruposCount);
            Log.Information("  • Clientes: {Count}", clientesCount);

            // Insertar datos iniciales si no existen
            if (rolesCount == 0)
            {
                Log.Information("📝 Insertando roles iniciales...");
                await InsertInitialRolesAsync(db);
            }

            if (usersCount == 0)
            {
                Log.Information("📝 Insertando usuario admin...");
                await InsertInitialUsersAsync(db);
            }

            // Mostrar usuarios existentes
            if (usersCount > 0 || await db.Users.AnyAsync())
            {
                var adminExists = await db.Users.AnyAsync(u => u.Email == "admin@gestiontime.local");
                
                Log.Information("👥 Usuarios:");
                Log.Information("  • admin@gestiontime.local: {Status}", adminExists ? "✅" : "❌");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "❌ Error verificando/creando datos");
            throw;
        }
    }

    private static async Task InsertInitialRolesAsync(GestionTimeDbContext db)
    {
        // Verificar si ya existen roles
        if (await db.Roles.AnyAsync())
        {
            Log.Information("  ℹ️ Roles ya existen, omitiendo creación");
            return;
        }
        
        var roles = new[]
        {
            new GestionTime.Domain.Auth.Role { Id = 1, Name = "ADMIN" },
            new GestionTime.Domain.Auth.Role { Id = 2, Name = "MANAGER" },
            new GestionTime.Domain.Auth.Role { Id = 3, Name = "USER" }
        };

        db.Roles.AddRange(roles);
        await db.SaveChangesAsync();
        Log.Information("  ✓ 3 roles creados (ADMIN, MANAGER, USER)");
    }

    private static async Task InsertInitialUsersAsync(GestionTimeDbContext db)
    {
        // Verificar si ya existe el usuario admin
        if (await db.Users.AnyAsync(u => u.Email == "admin@gestiontime.local"))
        {
            Log.Information("  ℹ️ Usuario admin ya existe, omitiendo creación");
            return;
        }
        
        // Hash de la contraseña "admin123"
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("admin123");

        var adminUser = new GestionTime.Domain.Auth.User
        {
            Id = Guid.NewGuid(),
            Email = "admin@gestiontime.local",
            PasswordHash = passwordHash,
            FullName = "Administrador del Sistema",
            Enabled = true,
            EmailConfirmed = true
        };

        db.Users.Add(adminUser);
        await db.SaveChangesAsync();

        // Asignar rol ADMIN
        var adminRole = await db.Roles.FirstAsync(r => r.Name == "ADMIN");
        db.UserRoles.Add(new GestionTime.Domain.Auth.UserRole
        {
            UserId = adminUser.Id,
            RoleId = adminRole.Id
        });

        await db.SaveChangesAsync();
        Log.Information("  ✓ Usuario admin creado (admin@gestiontime.local / admin123)");
    }
}

