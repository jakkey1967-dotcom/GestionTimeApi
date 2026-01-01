-- ═══════════════════════════════════════════════════════════════
-- 🔍 SCRIPT DE VERIFICACIÓN DE BASE DE DATOS
-- ═══════════════════════════════════════════════════════════════
-- Ejecutar este script para verificar que la BD está correctamente
-- configurada después del primer arranque de la aplicación.
--
-- Ejecutar como:
-- psql -U postgres -d gestiontime_test -f verify_database.sql
-- ═══════════════════════════════════════════════════════════════

\echo ''
\echo '╔══════════════════════════════════════════════════════════════╗'
\echo '║     🔍 VERIFICACIÓN DE BASE DE DATOS - GestionTime          ║'
\echo '╚══════════════════════════════════════════════════════════════╝'
\echo ''

-- ═══════════════════════════════════════════════════════════════
-- 1️⃣ VERIFICAR EXTENSIÓN PGCRYPTO
-- ═══════════════════════════════════════════════════════════════
\echo '1️⃣  Verificando extensión pgcrypto...'
SELECT 
    CASE 
        WHEN COUNT(*) > 0 THEN '✅ pgcrypto está instalado (versión: ' || MAX(extversion) || ')'
        ELSE '❌ pgcrypto NO está instalado'
    END AS resultado
FROM pg_extension 
WHERE extname = 'pgcrypto';

\echo ''

-- ═══════════════════════════════════════════════════════════════
-- 2️⃣ VERIFICAR SCHEMAS
-- ═══════════════════════════════════════════════════════════════
\echo '2️⃣  Verificando schemas personalizados...'
SELECT 
    '✅ ' || schema_name AS schemas_encontrados
FROM information_schema.schemata 
WHERE schema_name NOT IN ('pg_catalog', 'information_schema', 'pg_toast')
ORDER BY schema_name;

\echo ''

-- ═══════════════════════════════════════════════════════════════
-- 3️⃣ VERIFICAR TABLAS EN SCHEMA pss_dvnx
-- ═══════════════════════════════════════════════════════════════
\echo '3️⃣  Verificando tablas en schema pss_dvnx...'
SELECT 
    COUNT(*) || ' tablas encontradas' AS resultado
FROM information_schema.tables 
WHERE table_schema = 'pss_dvnx';

\echo ''
\echo 'Listado de tablas:'
SELECT 
    '   📋 ' || table_name AS tabla
FROM information_schema.tables 
WHERE table_schema = 'pss_dvnx'
ORDER BY table_name;

\echo ''

-- ═══════════════════════════════════════════════════════════════
-- 4️⃣ VERIFICAR TABLA DE MIGRACIONES
-- ═══════════════════════════════════════════════════════════════
\echo '4️⃣  Verificando migraciones aplicadas...'
SELECT 
    COUNT(*) || ' migración(es) aplicada(s)' AS resultado
FROM pss_dvnx.__efmigrationshistory;

\echo ''
\echo 'Historial de migraciones:'
SELECT 
    '   🔹 ' || migration_id || ' (aplicada: ' || applied::date || ')' AS migracion
FROM pss_dvnx.__efmigrationshistory
ORDER BY applied DESC;

\echo ''

-- ═══════════════════════════════════════════════════════════════
-- 5️⃣ VERIFICAR ROLES
-- ═══════════════════════════════════════════════════════════════
\echo '5️⃣  Verificando roles del sistema...'
SELECT 
    COUNT(*) || ' rol(es) configurado(s)' AS resultado
FROM pss_dvnx.roles;

\echo ''
SELECT 
    '   🎭 ' || name AS rol
FROM pss_dvnx.roles
ORDER BY name;

\echo ''

-- ═══════════════════════════════════════════════════════════════
-- 6️⃣ VERIFICAR TIPOS DE TRABAJO
-- ═══════════════════════════════════════════════════════════════
\echo '6️⃣  Verificando tipos de trabajo...'
SELECT 
    COUNT(*) || ' tipo(s) de trabajo' AS resultado
FROM pss_dvnx.tipo;

\echo ''

-- ═══════════════════════════════════════════════════════════════
-- 7️⃣ VERIFICAR GRUPOS DE TRABAJO
-- ═══════════════════════════════════════════════════════════════
\echo '7️⃣  Verificando grupos de trabajo...'
SELECT 
    COUNT(*) || ' grupo(s) de trabajo' AS resultado
FROM pss_dvnx.grupo;

\echo ''

-- ═══════════════════════════════════════════════════════════════
-- 8️⃣ VERIFICAR USUARIOS
-- ═══════════════════════════════════════════════════════════════
\echo '8️⃣  Verificando usuarios del sistema...'
SELECT 
    COUNT(*) || ' usuario(s) creado(s)' AS resultado
FROM pss_dvnx.users;

\echo ''
\echo 'Usuarios registrados:'
SELECT 
    '   👤 ' || email || 
    ' (' || full_name || ')' ||
    CASE WHEN enabled THEN ' ✅' ELSE ' ❌' END ||
    CASE WHEN email_confirmed THEN ' 📧' ELSE '' END AS usuario
FROM pss_dvnx.users
ORDER BY email;

\echo ''

-- ═══════════════════════════════════════════════════════════════
-- 9️⃣ VERIFICAR USUARIO ADMIN
-- ═══════════════════════════════════════════════════════════════
\echo '9️⃣  Verificando usuario administrador...'
SELECT 
    CASE 
        WHEN COUNT(*) > 0 THEN '✅ Usuario admin@admin.com existe'
        ELSE '❌ Usuario admin@admin.com NO existe'
    END AS resultado
FROM pss_dvnx.users
WHERE email = 'admin@admin.com';

\echo ''

-- Detalles del usuario admin
\echo 'Detalles del usuario admin:'
SELECT 
    '   Email:           ' || email AS detalle
FROM pss_dvnx.users
WHERE email = 'admin@admin.com'
UNION ALL
SELECT 
    '   Nombre:          ' || full_name
FROM pss_dvnx.users
WHERE email = 'admin@admin.com'
UNION ALL
SELECT 
    '   Habilitado:      ' || CASE WHEN enabled THEN 'Sí ✅' ELSE 'No ❌' END
FROM pss_dvnx.users
WHERE email = 'admin@admin.com'
UNION ALL
SELECT 
    '   Email confirmado: ' || CASE WHEN email_confirmed THEN 'Sí ✅' ELSE 'No ❌' END
FROM pss_dvnx.users
WHERE email = 'admin@admin.com'
UNION ALL
SELECT 
    '   Roles:           ' || STRING_AGG(r.name, ', ')
FROM pss_dvnx.users u
JOIN pss_dvnx.user_roles ur ON u.id = ur.user_id
JOIN pss_dvnx.roles r ON ur.role_id = r.id
WHERE u.email = 'admin@admin.com';

\echo ''

-- ═══════════════════════════════════════════════════════════════
-- 🔟 PROBAR FUNCIONES DE PGCRYPTO
-- ═══════════════════════════════════════════════════════════════
\echo '🔟 Probando funciones de pgcrypto...'

-- Generar hash de prueba
\echo 'Generando hash bcrypt de "Test123"...'
SELECT 
    '   Hash generado: ' || LEFT(crypt('Test123', gen_salt('bf', 10)), 20) || '...' AS resultado;

\echo ''

-- Verificar hash
\echo 'Verificando hash (debe devolver hash si coincide):'
WITH test AS (
    SELECT crypt('Test123', gen_salt('bf', 10)) AS hash
)
SELECT 
    CASE 
        WHEN crypt('Test123', hash) = hash THEN '✅ Verificación exitosa'
        ELSE '❌ Verificación fallida'
    END AS resultado
FROM test;

\echo ''

-- ═══════════════════════════════════════════════════════════════
-- ✅ RESUMEN FINAL
-- ═══════════════════════════════════════════════════════════════
\echo ''
\echo '╔══════════════════════════════════════════════════════════════╗'
\echo '║                    ✅ RESUMEN FINAL                          ║'
\echo '╚══════════════════════════════════════════════════════════════╝'
\echo ''

-- Contadores
SELECT 
    '📊 Estadísticas de la base de datos:' AS resumen
UNION ALL
SELECT 
    '   • Tablas:            ' || COUNT(*)
FROM information_schema.tables 
WHERE table_schema = 'pss_dvnx'
UNION ALL
SELECT 
    '   • Roles:             ' || COUNT(*)
FROM pss_dvnx.roles
UNION ALL
SELECT 
    '   • Usuarios:          ' || COUNT(*)
FROM pss_dvnx.users
UNION ALL
SELECT 
    '   • Tipos de trabajo:  ' || COUNT(*)
FROM pss_dvnx.tipo
UNION ALL
SELECT 
    '   • Grupos:            ' || COUNT(*)
FROM pss_dvnx.grupo
UNION ALL
SELECT 
    '   • Migraciones:       ' || COUNT(*)
FROM pss_dvnx.__efmigrationshistory;

\echo ''
\echo '═══════════════════════════════════════════════════════════════'
\echo '✅ Verificación completada'
\echo '═══════════════════════════════════════════════════════════════'
\echo ''
\echo '💡 CREDENCIALES DE ACCESO:'
\echo '   Email:    admin@admin.com'
\echo '   Password: Admin@2025'
\echo ''
\echo '⚠️  Cambiar la contraseña después del primer login en producción'
\echo ''
