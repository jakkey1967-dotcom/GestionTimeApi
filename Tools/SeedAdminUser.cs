using Npgsql;
using BCrypt.Net;

namespace GestionTime.Api.Tools;

/// <summary>
/// Herramienta para crear usuario admin directamente en la base de datos
/// Uso: dotnet run -- seed-admin
/// </summary>
public class SeedAdminUser
{
    private static readonly string RenderConnectionString = 
        "Host=dpg-d57tobm3jp1c73b6i4ug-a.frankfurt-postgres.render.com;Port=5432;Database=pss_dvnx;Username=gestiontime;Password=BvCDRFguh9SljJJUZOzGpdvpxgf18qnI;SSL Mode=Require;Trust Server Certificate=true";

    public static async Task Main(string[] args)
    {
        Console.WriteLine();
        Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║         👤 CREAR USUARIO ADMIN EN RENDER 👤             ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        try
        {
            await using var conn = new NpgsqlConnection(RenderConnectionString);
            await conn.OpenAsync();
            
            Console.WriteLine("✅ Conectado a Render");

            // Verificar si ya existe un usuario admin
            var checkUserQuery = "SELECT COUNT(*) FROM public.users WHERE email = @email";
            await using var checkCmd = new NpgsqlCommand(checkUserQuery, conn);
            checkCmd.Parameters.AddWithValue("email", "admin@admin.com");
            
            var userExists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;
            
            if (userExists)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("⚠️  El usuario admin@admin.com ya existe");
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine("¿Deseas reemplazarlo? (s/N): ");
                Console.Write("> ");
                var response = Console.ReadLine()?.Trim().ToLowerInvariant();
                
                if (response != "s" && response != "si" && response != "y" && response != "yes")
                {
                    Console.WriteLine("Operación cancelada");
                    return;
                }
                
                // Eliminar usuario existente
                await using var deleteCmd = new NpgsqlCommand(
                    "DELETE FROM public.users WHERE email = @email", conn);
                deleteCmd.Parameters.AddWithValue("email", "admin@admin.com");
                await deleteCmd.ExecuteNonQueryAsync();
                
                Console.WriteLine("🗑️  Usuario existente eliminado");
            }

            // PASO 1: Verificar/Crear roles
            Console.WriteLine();
            Console.WriteLine("📋 PASO 1: Verificando roles...");
            
            var roles = new[] { "ADMIN", "USER", "TECH" };
            var roleIds = new Dictionary<string, int>();
            
            foreach (var roleName in roles)
            {
                // Buscar rol existente
                var checkRoleQuery = "SELECT id FROM public.roles WHERE name = @name";
                await using var checkRoleCmd = new NpgsqlCommand(checkRoleQuery, conn);
                checkRoleCmd.Parameters.AddWithValue("name", roleName);
                
                var roleIdObj = await checkRoleCmd.ExecuteScalarAsync();
                
                if (roleIdObj != null)
                {
                    // Rol ya existe
                    roleIds[roleName] = Convert.ToInt32(roleIdObj);
                    Console.WriteLine($"  ✓ Rol '{roleName}' ya existe (ID: {roleIds[roleName]})");
                }
                else
                {
                    // Crear rol nuevo (dejamos que PostgreSQL asigne el ID automáticamente)
                    try
                    {
                        var insertRoleQuery = "INSERT INTO public.roles (name) VALUES (@name) RETURNING id";
                        await using var insertRoleCmd = new NpgsqlCommand(insertRoleQuery, conn);
                        insertRoleCmd.Parameters.AddWithValue("name", roleName);
                        
                        var newRoleId = await insertRoleCmd.ExecuteScalarAsync();
                        roleIds[roleName] = Convert.ToInt32(newRoleId!);
                        
                        Console.WriteLine($"  ✅ Rol '{roleName}' creado (ID: {roleIds[roleName]})");
                    }
                    catch (Npgsql.PostgresException ex) when (ex.SqlState == "23505")
                    {
                        // Conflicto de clave única (el rol se creó entre medio)
                        // Volver a buscar
                        var recheckCmd = new NpgsqlCommand(checkRoleQuery, conn);
                        recheckCmd.Parameters.AddWithValue("name", roleName);
                        var recheckId = await recheckCmd.ExecuteScalarAsync();
                        
                        if (recheckId != null)
                        {
                            roleIds[roleName] = Convert.ToInt32(recheckId);
                            Console.WriteLine($"  ✓ Rol '{roleName}' ya existía (ID: {roleIds[roleName]})");
                        }
                        else
                        {
                            throw; // Error inesperado
                        }
                    }
                }
            }

            // PASO 2: Crear usuario admin
            Console.WriteLine();
            Console.WriteLine("👤 PASO 2: Creando usuario admin...");
            
            var adminId = Guid.NewGuid();
            var email = "admin@admin.com";
            var password = "rootadmin";
            var fullName = "Administrador del Sistema";
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            
            var insertUserQuery = @"
                INSERT INTO public.users (id, email, password_hash, full_name, enabled, email_confirmed)
                VALUES (@id, @email, @password_hash, @full_name, @enabled, @email_confirmed)";
            
            await using var insertUserCmd = new NpgsqlCommand(insertUserQuery, conn);
            insertUserCmd.Parameters.AddWithValue("id", adminId);
            insertUserCmd.Parameters.AddWithValue("email", email);
            insertUserCmd.Parameters.AddWithValue("password_hash", passwordHash);
            insertUserCmd.Parameters.AddWithValue("full_name", fullName);
            insertUserCmd.Parameters.AddWithValue("enabled", true);
            insertUserCmd.Parameters.AddWithValue("email_confirmed", true);
            
            await insertUserCmd.ExecuteNonQueryAsync();
            
            Console.WriteLine($"  ✅ Usuario creado: {email}");
            Console.WriteLine($"     ID: {adminId}");

            // PASO 3: Asignar rol ADMIN
            Console.WriteLine();
            Console.WriteLine("🔐 PASO 3: Asignando rol ADMIN...");
            
            var insertUserRoleQuery = @"
                INSERT INTO public.user_roles (user_id, role_id)
                VALUES (@user_id, @role_id)";
            
            await using var insertUserRoleCmd = new NpgsqlCommand(insertUserRoleQuery, conn);
            insertUserRoleCmd.Parameters.AddWithValue("user_id", adminId);
            insertUserRoleCmd.Parameters.AddWithValue("role_id", roleIds["ADMIN"]);
            
            await insertUserRoleCmd.ExecuteNonQueryAsync();
            
            Console.WriteLine("  ✅ Rol ADMIN asignado");

            // RESUMEN
            Console.WriteLine();
            Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("║              ✅ USUARIO ADMIN CREADO ✅                  ║");
            Console.ResetColor();
            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
            Console.WriteLine();
            Console.WriteLine("📊 CREDENCIALES:");
            Console.WriteLine($"   Email:    {email}");
            Console.WriteLine($"   Password: {password}");
            Console.WriteLine($"   Rol:      ADMIN");
            Console.WriteLine();
            Console.WriteLine("🌐 PRUEBA EL LOGIN:");
            Console.WriteLine("   POST https://gestiontime-api.onrender.com/api/v1/auth/login");
            Console.WriteLine("   {");
            Console.WriteLine($"     \"email\": \"{email}\",");
            Console.WriteLine($"     \"password\": \"{password}\"");
            Console.WriteLine("   }");
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ ERROR: {ex.Message}");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Detalles técnicos:");
            Console.WriteLine(ex.ToString());
        }
    }
}
