-- ========================================
-- SCRIPT COMPLETO DE INICIALIZACIÓN
-- ========================================
-- 
-- DESCRIPCIÓN:
--   Crea un usuario administrador con email admin@admin.com
--   y le asigna el rol ADMIN con todos los permisos.
--   Además, inicializa los datos básicos del sistema:
--   - Roles (ADMIN, EDITOR, USER)
--   - Tipos de Trabajo (10 tipos)
--   - Grupos de Trabajo (8 grupos)
--
-- IMPORTANTE:
--   1. Modifica las variables al inicio del script según tus necesidades
--   2. Ejecuta este script en pgAdmin o desde psql
--   3. El script es idempotente (puede ejecutarse múltiples veces)
--   4. Los datos existentes NO se duplican (usa ON CONFLICT)
--
-- USO EN pgAdmin:
--   1. Conecta a tu base de datos PostgreSQL
--   2. Abre Query Tool (F5)
--   3. Copia y pega este script
--   4. Modifica las variables si es necesario
--   5. Ejecuta (F5)
--
-- USO EN psql:
--   psql -h localhost -U postgres -d gestiontime -f create_admin_user_complete.sql
--
-- ========================================

-- ==================== CONFIGURACIÓN ====================
-- ⚠️ MODIFICA ESTOS VALORES ANTES DE EJECUTAR
-- ========================================

DO $$
DECLARE
    -- ✅ CONFIGURAR ESTOS VALORES
    v_email VARCHAR(200) := 'admin@admin.com';
    v_password_plain VARCHAR(100) := 'Admin@2025';
    v_full_name VARCHAR(200) := 'Administrador del Sistema';
    v_schema VARCHAR(50) := 'pss_dvnx';  -- Cambiar según tu cliente: pss_dvnx, cliente_abc, etc.
    
    -- Variables internas (no modificar)
    v_user_id UUID;
    v_admin_role_id INT;
    v_password_hash TEXT;
    v_existing_user_count INT;
    v_roles_count INT;
    v_tipos_count INT;
    v_grupos_count INT;
BEGIN
    -- ==================== 1. ESTABLECER SCHEMA ====================
    EXECUTE format('SET search_path TO %I', v_schema);
    
    RAISE NOTICE '╔══════════════════════════════════════════════════════════╗';
    RAISE NOTICE '║    👤 CREAR USUARIO ADMINISTRADOR + DATOS INICIALES 👤   ║';
    RAISE NOTICE '╚══════════════════════════════════════════════════════════╝';
    RAISE NOTICE '';
    RAISE NOTICE '📋 CONFIGURACIÓN:';
    RAISE NOTICE '   Email: %', v_email;
    RAISE NOTICE '   Nombre: %', v_full_name;
    RAISE NOTICE '   Schema: %', v_schema;
    RAISE NOTICE '';
    
    -- ==================== 2. VERIFICAR SI EL USUARIO YA EXISTE ====================
    RAISE NOTICE '🔍 Verificando si el usuario ya existe...';
    
    SELECT COUNT(*) INTO v_existing_user_count
    FROM users
    WHERE email = v_email;
    
    IF v_existing_user_count > 0 THEN
        RAISE NOTICE '';
        RAISE NOTICE '⚠️  EL USUARIO YA EXISTE';
        RAISE NOTICE '';
        RAISE NOTICE 'Si deseas recrearlo:';
        RAISE NOTICE '1. Elimina el usuario existente:';
        RAISE NOTICE '   DELETE FROM users WHERE email = ''%'';', v_email;
        RAISE NOTICE '2. Ejecuta este script nuevamente';
        RAISE NOTICE '';
        RAISE EXCEPTION 'Usuario % ya existe. Usa otro email o elimina el usuario existente.', v_email;
    END IF;
    
    RAISE NOTICE '✅ El usuario no existe, continuando...';
    RAISE NOTICE '';
    
    -- ==================== 3. CREAR DATOS INICIALES (SEED) ====================
    
    RAISE NOTICE '📦 Creando datos iniciales del sistema...';
    RAISE NOTICE '';
    
    -- 3.1 ROLES
    RAISE NOTICE '🎭 Creando roles...';
    
    INSERT INTO roles (name)
    VALUES 
        ('ADMIN'),
        ('EDITOR'),
        ('USER')
    ON CONFLICT (name) DO NOTHING;
    
    SELECT COUNT(*) INTO v_roles_count FROM roles;
    RAISE NOTICE '   ✅ Roles: % registrados', v_roles_count;
    
    -- 3.2 TIPOS DE TRABAJO
    RAISE NOTICE '📋 Creando tipos de trabajo...';
    
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
    
    -- Resetear secuencia
    PERFORM setval(pg_get_serial_sequence('tipo', 'id_tipo'), (SELECT MAX(id_tipo) FROM tipo));
    
    SELECT COUNT(*) INTO v_tipos_count FROM tipo;
    RAISE NOTICE '   ✅ Tipos: % registrados', v_tipos_count;
    
    -- 3.3 GRUPOS DE TRABAJO
    RAISE NOTICE '👥 Creando grupos de trabajo...';
    
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
    
    -- Resetear secuencia
    PERFORM setval(pg_get_serial_sequence('grupo', 'id_grupo'), (SELECT MAX(id_grupo) FROM grupo));
    
    SELECT COUNT(*) INTO v_grupos_count FROM grupo;
    RAISE NOTICE '   ✅ Grupos: % registrados', v_grupos_count;
    RAISE NOTICE '';
    
    -- ==================== 4. GENERAR HASH DE CONTRASEÑA ====================
    -- ⚠️ IMPORTANTE: Este script usa crypt() con bcrypt
    -- Crear extensión pgcrypto si no existe (requerido para gen_salt y crypt)
    
    RAISE NOTICE '🔐 Verificando extensión pgcrypto...';
    
    -- Crear extensión pgcrypto si no existe
    CREATE EXTENSION IF NOT EXISTS pgcrypto;
    
    RAISE NOTICE '✅ Extensión pgcrypto disponible';
    RAISE NOTICE '🔐 Generando hash BCrypt de contraseña...';
    
    -- Usar bcrypt para el hash (compatible con BCrypt.Net en C#)
    v_password_hash := crypt(v_password_plain, gen_salt('bf', 10));
    
    RAISE NOTICE '✅ Hash generado correctamente';
    RAISE NOTICE '';
    
    -- ==================== 5. CREAR USUARIO ADMINISTRADOR ====================
    RAISE NOTICE '👤 Creando usuario administrador...';
    
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
        true,           -- Habilitado
        true,           -- Email confirmado
        false,          -- No requiere cambio de contraseña
        NOW(),          -- Contraseña recién cambiada
        999             -- No expira (casi nunca)
    );
    
    RAISE NOTICE '✅ Usuario creado con ID: %', v_user_id;
    RAISE NOTICE '';
    
    -- ==================== 6. ASIGNAR ROL ADMIN ====================
    RAISE NOTICE '🎭 Asignando rol ADMIN...';
    
    SELECT id INTO v_admin_role_id
    FROM roles
    WHERE name = 'ADMIN';
    
    INSERT INTO user_roles (user_id, role_id)
    VALUES (v_user_id, v_admin_role_id);
    
    RAISE NOTICE '✅ Rol ADMIN asignado correctamente';
    RAISE NOTICE '';
    
    -- ==================== 7. CREAR PERFIL DE USUARIO ====================
    RAISE NOTICE '📝 Creando perfil de usuario...';
    
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
    
    RAISE NOTICE '✅ Perfil creado correctamente';
    RAISE NOTICE '';
    
    -- ==================== 8. RESUMEN FINAL ====================
    RAISE NOTICE '╔══════════════════════════════════════════════════════════╗';
    RAISE NOTICE '║            ✅ INICIALIZACIÓN EXITOSA ✅                  ║';
    RAISE NOTICE '╚══════════════════════════════════════════════════════════╝';
    RAISE NOTICE '';
    RAISE NOTICE '👤 USUARIO ADMINISTRADOR:';
    RAISE NOTICE '   📧 Email: %', v_email;
    RAISE NOTICE '   🔑 Password: %', v_password_plain;
    RAISE NOTICE '   👤 Nombre: %', v_full_name;
    RAISE NOTICE '   🆔 User ID: %', v_user_id;
    RAISE NOTICE '   🎭 Rol: ADMIN';
    RAISE NOTICE '   ✅ Email confirmado: Sí';
    RAISE NOTICE '   🔐 Contraseña expira: No (999 días)';
    RAISE NOTICE '';
    RAISE NOTICE '📊 DATOS INICIALES:';
    RAISE NOTICE '   ✅ Roles: % (ADMIN, EDITOR, USER)', v_roles_count;
    RAISE NOTICE '   ✅ Tipos de Trabajo: %', v_tipos_count;
    RAISE NOTICE '   ✅ Grupos: %', v_grupos_count;
    RAISE NOTICE '';
    RAISE NOTICE '⚠️  IMPORTANTE: Cambia la contraseña después del primer login';
    RAISE NOTICE '';
    
END $$;

-- ==================== CONSULTA DE VERIFICACIÓN ====================
-- Mostrar información del usuario recién creado

SELECT 
    u.id AS user_id,
    u.email,
    u.full_name,
    u.enabled AS habilitado,
    u.email_confirmed AS email_confirmado,
    u.must_change_password AS requiere_cambio_password,
    u.password_expiration_days AS dias_expiracion,
    array_agg(r.name ORDER BY r.name) AS roles,
    up.department AS departamento,
    up.position AS posicion
FROM users u
LEFT JOIN user_roles ur ON u.id = ur.user_id
LEFT JOIN roles r ON ur.role_id = r.id
LEFT JOIN user_profiles up ON u.id = up.id
WHERE u.email = 'admin@admin.com'
GROUP BY u.id, u.email, u.full_name, u.enabled, u.email_confirmed, 
         u.must_change_password, u.password_expiration_days,
         up.department, up.position;

-- ==================== CONSULTAS DE VERIFICACIÓN ADICIONALES ====================

-- Verificar Roles
SELECT 'ROLES' as tabla, COUNT(*) as total FROM roles;

-- Verificar Tipos de Trabajo
SELECT 'TIPOS' as tabla, COUNT(*) as total FROM tipo;

-- Verificar Grupos
SELECT 'GRUPOS' as tabla, COUNT(*) as total FROM grupo;

-- Listar Tipos de Trabajo
SELECT id_tipo, nombre, descripcion FROM tipo ORDER BY id_tipo;

-- Listar Grupos
SELECT id_grupo, nombre, descripcion FROM grupo ORDER BY id_grupo;

-- ==================== INFORMACIÓN ADICIONAL ====================

/*
╔══════════════════════════════════════════════════════════════════════════╗
║                         📋 INFORMACIÓN ADICIONAL                         ║
╚══════════════════════════════════════════════════════════════════════════╝

✅ CREDENCIALES DE ACCESO:
   Email: admin@admin.com
   Password: Admin@2025

📊 DATOS INICIALES CREADOS:

   🎭 ROLES (3):
      - ADMIN   : Acceso completo al sistema
      - EDITOR  : Puede editar pero no administrar
      - USER    : Usuario estándar

   📋 TIPOS DE TRABAJO (10):
      1.  Incidencia
      2.  Instalación
      3.  Aviso
      4.  Petición
      5.  Facturable
      6.  Duda
      7.  Desarrollo
      8.  Tarea
      9.  Ofertado
      10. Llamada Overlay

   👥 GRUPOS (8):
      1. Administración
      2. Comercial
      3. Desarrollo
      4. Gestión Central
      5. Logística
      6. Movilidad
      7. Post-Venta
      8. Tiendas

🌐 ENDPOINTS DISPONIBLES:
   POST /api/v1/auth/login          - Iniciar sesión
   GET  /api/v1/auth/me             - Información del usuario actual
   GET  /api/v1/admin/users         - Listar usuarios (solo ADMIN)
   GET  /api/v1/tipos               - Listar tipos de trabajo
   GET  /api/v1/grupos              - Listar grupos
   GET  /api/v1/partes              - Listar partes de trabajo

🔐 PERMISOS DEL ROL ADMIN:
   ✅ Acceso completo a todos los endpoints
   ✅ Gestión de usuarios (crear, editar, eliminar)
   ✅ Gestión de roles
   ✅ Acceso a estadísticas y reportes
   ✅ Configuración del sistema

⚙️  SCHEMAS DISPONIBLES:
   - pss_dvnx      : Cliente PSS DVNX (GestionTime Global-retail.com)
   - cliente_abc   : Cliente ABC
   - cliente_xyz   : Cliente XYZ
   - gestiontime   : Schema por defecto (desarrollo)

🔧 MODIFICAR PARA OTRO CLIENTE:
   Para crear el usuario admin en otro schema, edita la variable al inicio:
   
   v_schema := 'cliente_abc';  -- Cambiar según tu cliente

📝 NOTAS:
   - Este script es idempotente (puede ejecutarse múltiples veces)
   - Si el usuario ya existe, el script fallará con un mensaje claro
   - Los datos de Tipos y Grupos NO se duplican (usa ON CONFLICT)
   - Las secuencias se resetean automáticamente
   - El hash de contraseña usa BCrypt (compatible con BCrypt.Net en C#)

🐛 SOLUCIÓN DE PROBLEMAS:
   
   ERROR: function gen_salt(unknown, integer) does not exist
   SOLUCIÓN: Instala la extensión pgcrypto:
   CREATE EXTENSION IF NOT EXISTS pgcrypto;
   
   ERROR: relation "users" does not exist
   SOLUCIÓN: Verifica que el schema sea correcto y que las migraciones
   se hayan ejecutado.
   
   ERROR: usuario ya existe
   SOLUCIÓN: Elimina el usuario existente o usa otro email:
   DELETE FROM users WHERE email = 'admin@admin.com';

   ERROR: duplicate key value violates unique constraint "tipo_pkey"
   SOLUCIÓN: Los tipos ya existen. Este error se puede ignorar si ya
   tienes datos, o elimina los existentes:
   DELETE FROM tipo WHERE id_tipo <= 10;

═══════════════════════════════════════════════════════════════════════════╝
*/

-- FIN DEL SCRIPT
