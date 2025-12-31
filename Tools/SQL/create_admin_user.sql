-- ============================================
-- SCRIPT SQL PARA CREAR USUARIO ADMIN
-- Base de datos: Render PostgreSQL
-- Schema: pss_dvnx
-- ============================================

-- PASO 1: Verificar y crear roles (si no existen)
DO $$
BEGIN
    -- Verificar si existe ADMIN
    IF NOT EXISTS (SELECT 1 FROM pss_dvnx.roles WHERE name = 'ADMIN') THEN
        INSERT INTO pss_dvnx.roles (name) VALUES ('ADMIN');
        RAISE NOTICE 'Rol ADMIN creado';
    ELSE
        RAISE NOTICE 'Rol ADMIN ya existe';
    END IF;

    -- Verificar si existe USER
    IF NOT EXISTS (SELECT 1 FROM pss_dvnx.roles WHERE name = 'USER') THEN
        INSERT INTO pss_dvnx.roles (name) VALUES ('USER');
        RAISE NOTICE 'Rol USER creado';
    ELSE
        RAISE NOTICE 'Rol USER ya existe';
    END IF;

    -- Verificar si existe TECH
    IF NOT EXISTS (SELECT 1 FROM pss_dvnx.roles WHERE name = 'TECH') THEN
        INSERT INTO pss_dvnx.roles (name) VALUES ('TECH');
        RAISE NOTICE 'Rol TECH creado';
    ELSE
        RAISE NOTICE 'Rol TECH ya existe';
    END IF;
END $$;

-- PASO 2: Eliminar usuario admin si existe (para reemplazarlo)
DELETE FROM pss_dvnx.user_roles WHERE user_id IN (
    SELECT id FROM pss_dvnx.users WHERE email = 'admin@admin.com'
);
DELETE FROM pss_dvnx.users WHERE email = 'admin@admin.com';

-- PASO 3: Crear usuario admin
-- Password: rootadmin
-- Hash BCrypt generado: $2a$11$2HQ7DMD7VJpjRwJBuAiZOec6TEpyPNjEJ4Pt4zdnvkkpN7HLhZMRq
-- NOTA: Usando TODAS las columnas necesarias (incluye email_confirmed)
INSERT INTO pss_dvnx.users (
    id, 
    email, 
    password_hash, 
    full_name, 
    enabled,
    email_confirmed,
    must_change_password,
    password_expiration_days
) VALUES (
    gen_random_uuid(),
    'admin@admin.com',
    '$2a$11$2HQ7DMD7VJpjRwJBuAiZOec6TEpyPNjEJ4Pt4zdnvkkpN7HLhZMRq',
    'Administrador del Sistema',
    true,
    true,
    false,
    90
);

-- PASO 4: Asignar rol ADMIN al usuario
INSERT INTO pss_dvnx.user_roles (user_id, role_id)
SELECT 
    u.id,
    r.id
FROM 
    pss_dvnx.users u,
    pss_dvnx.roles r
WHERE 
    u.email = 'admin@admin.com' 
    AND r.name = 'ADMIN';

-- PASO 5: Verificar que se creó correctamente
SELECT 
    u.id,
    u.email,
    u.full_name,
    u.enabled,
    r.name as role
FROM 
    pss_dvnx.users u
    JOIN pss_dvnx.user_roles ur ON u.id = ur.user_id
    JOIN pss_dvnx.roles r ON ur.role_id = r.id
WHERE 
    u.email = 'admin@admin.com';

-- ============================================
-- RESULTADO ESPERADO:
-- Usuario: admin@admin.com
-- Password: rootadmin
-- Rol: ADMIN
-- Estado: Habilitado y confirmado
-- ============================================
