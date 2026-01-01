using GestionTime.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Serilog;
using System.Text;

namespace GestionTime.Api.Startup;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GestionTimeDbContext>();
        var schemaConfig = scope.ServiceProvider.GetRequiredService<DatabaseSchemaConfig>();

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
            Log.Information("📋 Schema configurado: {Schema}", schemaConfig.Schema);
            
            // Verificar si ya existe usuario admin
            var adminExists = await CheckAdminExistsAsync(db);
            
            if (adminExists)
            {
                Log.Information("ℹ️  Usuario admin ya existe, omitiendo seed");
                return;
            }
            
            // Ejecutar script SQL completo
            await ExecuteInitializationScriptAsync(db, schemaConfig.Schema);
            
            Log.Information("✅ Inicialización completada exitosamente");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "❌ Error en proceso de seed");
            // No lanzar excepción para no detener el arranque
        }
    }

    private static async Task<bool> CheckAdminExistsAsync(GestionTimeDbContext db)
    {
        try
        {
            return await db.Users.AnyAsync(u => u.Email == "admin@admin.com");
        }
        catch
        {
            // Si hay error al consultar (tabla no existe, etc), continuar con seed
            return false;
        }
    }

    private static async Task ExecuteInitializationScriptAsync(GestionTimeDbContext db, string schema)
    {
        Log.Information("🚀 Ejecutando script de inicialización completa...");
        
        // Obtener connection string
        var connectionString = db.Database.GetConnectionString();
        
        if (string.IsNullOrEmpty(connectionString))
        {
            Log.Error("❌ No se pudo obtener connection string");
            return;
        }

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        // Script SQL completo con variables configurables
        var sql = GenerateInitializationScript(schema);

        try
        {
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.CommandTimeout = 60; // 60 segundos timeout
            
            await cmd.ExecuteNonQueryAsync();
            
            Log.Information("✅ Script ejecutado correctamente");
            Log.Information("📧 Credenciales: admin@admin.com / Admin@2025");
        }
        catch (PostgresException pgEx) when (pgEx.SqlState == "P0001")
        {
            // Error controlado desde el script (usuario ya existe)
            Log.Warning("⚠️  {Message}", pgEx.MessageText);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "❌ Error ejecutando script de inicialización");
            throw;
        }
    }

    private static string GenerateInitializationScript(string schema)
    {
        // Script SQL completo idempotente - Sin usar crypt() para evitar problemas con pgcrypto
        return $@"
DO $$
DECLARE
    v_email VARCHAR(200) := 'admin@admin.com';
    v_password_plain VARCHAR(100) := 'Admin@2025';
    v_full_name VARCHAR(200) := 'Administrador del Sistema';
    v_schema VARCHAR(50) := '{schema}';
    
    v_user_id UUID;
    v_admin_role_id INT;
    v_password_hash TEXT;
    v_existing_user_count INT;
    v_roles_count INT;
BEGIN
    -- Establecer schema
    EXECUTE format('SET search_path TO %I', v_schema);
    
    -- Verificar si el usuario ya existe
    SELECT COUNT(*) INTO v_existing_user_count
    FROM users
    WHERE email = v_email;
    
    IF v_existing_user_count > 0 THEN
        RAISE NOTICE '⚠️  El usuario % ya existe', v_email;
        RETURN;
    END IF;
    
    RAISE NOTICE '📦 Iniciando creación de datos iniciales...';
    
    -- 1. CREAR ROLES
    INSERT INTO roles (name)
    VALUES 
        ('ADMIN'),
        ('EDITOR'),
        ('USER')
    ON CONFLICT (name) DO NOTHING;
    
    -- 2. CREAR TIPOS DE TRABAJO
    INSERT INTO tipo (id_tipo, nombre, descripcion)
    VALUES
        (1,  'Incidencia',       NULL),
        (2,  'Instalación',      NULL),
        (3,  'Aviso',            NULL),
        (4,  'Petición',         NULL),
        (5,  'Facturable',       NULL),
        (6,  'Duda',             NULL),
        (7,  'Desarrollo',       NULL),
        (8,  'Tarea',            NULL),
        (9,  'Ofertado',         NULL),
        (10, 'Llamada Overlay',  '')
    ON CONFLICT (id_tipo) DO NOTHING;
    
    -- Resetear secuencia de tipo
    PERFORM setval(pg_get_serial_sequence('tipo', 'id_tipo'), COALESCE((SELECT MAX(id_tipo) FROM tipo), 1));
    
    -- 3. CREAR GRUPOS DE TRABAJO
    INSERT INTO grupo (id_grupo, nombre, descripcion)
    VALUES
        (1, 'Administración',  NULL),
        (2, 'Comercial',       NULL),
        (3, 'Desarrollo',      NULL),
        (4, 'Gestión Central', NULL),
        (5, 'Logística',       NULL),
        (6, 'Movilidad',       NULL),
        (7, 'Post-Venta',      NULL),
        (8, 'Tiendas',         NULL)
    ON CONFLICT (id_grupo) DO NOTHING;
    
    -- Resetear secuencia de grupo
    PERFORM setval(pg_get_serial_sequence('grupo', 'id_grupo'), COALESCE((SELECT MAX(id_grupo) FROM grupo), 1));
    
    -- 4. GENERAR HASH TEMPORAL DE CONTRASEÑA
    -- ⚠️ La aplicación C# usará BCrypt.Net para generar el hash correcto
    -- Este es solo un placeholder que la aplicación detectará
    v_password_hash := 'TEMP_HASH_' || v_password_plain;
    
    -- 5. CREAR USUARIO ADMINISTRADOR
    v_user_id := gen_random_uuid();
    
    INSERT INTO users (
        id,
        email,
        password_hash,
        full_name,
        enabled,
        email_confirmed,
        must_change_password,
        password_changed_at,
        password_expiration_days
    )
    VALUES (
        v_user_id,
        v_email,
        v_password_hash,
        v_full_name,
        true,
        true,
        true,  -- ✅ FORZAR cambio de contraseña en primer login
        NOW(),
        999
    );
    
    -- 6. ASIGNAR ROL ADMIN
    SELECT id INTO v_admin_role_id
    FROM roles
    WHERE name = 'ADMIN';
    
    INSERT INTO user_roles (user_id, role_id)
    VALUES (v_user_id, v_admin_role_id);
    
    -- 7. CREAR PERFIL DE USUARIO
    INSERT INTO user_profiles (
        id,
        first_name,
        last_name,
        department,
        position,
        employee_type,
        hire_date,
        created_at,
        updated_at
    )
    VALUES (
        v_user_id,
        'Admin',
        'Sistema',
        'Administración',
        'Administrador del Sistema',
        'Administrador',
        NOW(),
        NOW(),
        NOW()
    );
    
    -- Verificar totales
    SELECT COUNT(*) INTO v_roles_count FROM roles;
    
    RAISE NOTICE '✅ Inicialización completada:';
    RAISE NOTICE '   👤 Usuario: %', v_email;
    RAISE NOTICE '   🔑 Password TEMPORAL: %', v_password_plain;
    RAISE NOTICE '   ⚠️  DEBE CAMBIAR PASSWORD EN PRIMER LOGIN';
    RAISE NOTICE '   🎭 Roles: %', v_roles_count;
    RAISE NOTICE '   📋 Tipos: 10';
    RAISE NOTICE '   👥 Grupos: 8';
    
END $$;
";
    }
}

