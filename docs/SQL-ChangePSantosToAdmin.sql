-- ═══════════════════════════════════════════════════════════════════════════════
-- 🔐 CAMBIAR ROL DE psantos@global-retail.com A ADMIN
-- ═══════════════════════════════════════════════════════════════════════════════
-- IMPORTANTE: Ejecutar en la base de datos PostgreSQL del proyecto API
-- Schema: pss_dvnx
-- ═══════════════════════════════════════════════════════════════════════════════

-- Establecer el schema correcto
SET search_path TO pss_dvnx;

-- ═══════════════════════════════════════════════════════════════════════════════
-- PASO 1: VERIFICAR ESTADO ACTUAL
-- ═══════════════════════════════════════════════════════════════════════════════

-- Ver usuario actual y sus roles
SELECT 
    u.id AS user_id,
    u.email,
    u.full_name,
    u.enabled,
    STRING_AGG(r.name, ', ') AS roles_actuales
FROM pss_dvnx.users u
LEFT JOIN pss_dvnx.user_roles ur ON u.id = ur.user_id
LEFT JOIN pss_dvnx.roles r ON ur.role_id = r.id
WHERE u.email = 'psantos@global-retail.com'
GROUP BY u.id, u.email, u.full_name, u.enabled;

-- Ver todos los roles disponibles
SELECT id, name FROM pss_dvnx.roles ORDER BY id;

-- ═══════════════════════════════════════════════════════════════════════════════
-- PASO 2: CAMBIAR ROL A ADMIN
-- ═══════════════════════════════════════════════════════════════════════════════

DO $$
DECLARE
    v_user_id UUID;
    v_admin_role_id INT;
    v_current_roles TEXT;
BEGIN
    -- Buscar el usuario
    SELECT id INTO v_user_id
    FROM pss_dvnx.users
    WHERE email = 'psantos@global-retail.com';
    
    IF v_user_id IS NULL THEN
        RAISE EXCEPTION '❌ Usuario psantos@global-retail.com no encontrado';
    END IF;
    
    RAISE NOTICE '✅ Usuario encontrado: %', v_user_id;
    
    -- Buscar el rol ADMIN
    SELECT id INTO v_admin_role_id
    FROM pss_dvnx.roles
    WHERE name = 'ADMIN';
    
    IF v_admin_role_id IS NULL THEN
        RAISE EXCEPTION '❌ Rol ADMIN no encontrado en la base de datos';
    END IF;
    
    RAISE NOTICE '✅ Rol ADMIN encontrado: ID=%', v_admin_role_id;
    
    -- Ver roles actuales
    SELECT STRING_AGG(r.name, ', ') INTO v_current_roles
    FROM pss_dvnx.user_roles ur
    JOIN pss_dvnx.roles r ON ur.role_id = r.id
    WHERE ur.user_id = v_user_id;
    
    RAISE NOTICE 'ℹ️  Roles actuales: %', COALESCE(v_current_roles, 'NINGUNO');
    
    -- ELIMINAR TODOS LOS ROLES ACTUALES
    DELETE FROM pss_dvnx.user_roles WHERE user_id = v_user_id;
    RAISE NOTICE '🗑️  Roles anteriores eliminados';
    
    -- ASIGNAR ROL ADMIN
    INSERT INTO pss_dvnx.user_roles (user_id, role_id)
    VALUES (v_user_id, v_admin_role_id);
    
    RAISE NOTICE '✅ Rol ADMIN asignado exitosamente';
    RAISE NOTICE '⚠️  psantos@global-retail.com debe CERRAR SESIÓN y volver a entrar';
    
END $$;

-- ═══════════════════════════════════════════════════════════════════════════════
-- PASO 3: VERIFICAR QUE SE APLICÓ EL CAMBIO
-- ═══════════════════════════════════════════════════════════════════════════════

SELECT 
    u.id,
    u.email,
    u.full_name,
    u.enabled,
    STRING_AGG(r.name, ', ') AS roles_actuales
FROM pss_dvnx.users u
LEFT JOIN pss_dvnx.user_roles ur ON u.id = ur.user_id
LEFT JOIN pss_dvnx.roles r ON ur.role_id = r.id
WHERE u.email = 'psantos@global-retail.com'
GROUP BY u.id, u.email, u.full_name, u.enabled;

-- ═══════════════════════════════════════════════════════════════════════════════
-- RESULTADO ESPERADO:
-- ═══════════════════════════════════════════════════════════════════════════════
-- email: psantos@global-retail.com
-- roles_actuales: ADMIN
-- enabled: true
-- ═══════════════════════════════════════════════════════════════════════════════

-- ═══════════════════════════════════════════════════════════════════════════════
-- OPCIONAL: Ver todos los usuarios con sus roles
-- ═══════════════════════════════════════════════════════════════════════════════

SELECT 
    u.email,
    u.full_name,
    u.enabled,
    STRING_AGG(r.name, ', ' ORDER BY r.name) AS roles
FROM pss_dvnx.users u
LEFT JOIN pss_dvnx.user_roles ur ON u.id = ur.user_id
LEFT JOIN pss_dvnx.roles r ON ur.role_id = r.id
GROUP BY u.id, u.email, u.full_name, u.enabled
ORDER BY u.email;
