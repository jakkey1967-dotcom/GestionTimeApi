-- ═══════════════════════════════════════════════════════════════
-- DIAGNÓSTICO: Perfil devuelto NO coincide con usuario logueado
-- ═══════════════════════════════════════════════════════════════
-- PROBLEMA:
--   • Login: wsanchez@global-retail.com → Wilson Sánchez
--   • API /profiles/me devuelve: Francisco Santos
-- ═══════════════════════════════════════════════════════════════

-- 🔍 PASO 1: Verificar estructura de relación users <-> user_profiles
-- ============================================================

-- ¿Qué usuarios existen y cuál es su relación con profiles?
SELECT 
    u.id AS user_id,
    u.email,
    u.name AS user_name,
    u.role,
    u.created_at AS user_created_at,
    -- Si existe columna user_id en user_profiles:
    -- p.id AS profile_id,
    -- p.first_name,
    -- p.last_name,
    -- p.phone
    '(verificar relación)' AS profile_relation
FROM pss_dvnx.users u
-- LEFT JOIN pss_dvnx.user_profiles p ON p.user_id = u.id
ORDER BY u.id;

-- 🔍 PASO 2: Buscar usuario específico (wsanchez@global-retail.com)
-- ============================================================

SELECT 
    u.id AS user_id,
    u.email,
    u.name AS user_name,
    u.role,
    u.created_at
FROM pss_dvnx.users u
WHERE u.email = 'wsanchez@global-retail.com';

-- 🔍 PASO 3: Buscar perfil de "Francisco Santos"
-- ============================================================

SELECT 
    p.id AS profile_id,
    p.first_name,
    p.last_name,
    p.phone,
    p.created_at,
    -- Buscar si hay alguna columna que relacione con users
    p.* 
FROM pss_dvnx.user_profiles p
WHERE 
    p.first_name ILIKE '%Francisco%' 
    OR p.last_name ILIKE '%Santos%';

-- 🔍 PASO 4: Buscar perfil de "Wilson Sánchez"
-- ============================================================

SELECT 
    p.id AS profile_id,
    p.first_name,
    p.last_name,
    p.phone,
    p.created_at,
    p.*
FROM pss_dvnx.user_profiles p
WHERE 
    p.first_name ILIKE '%Wilson%' 
    OR p.last_name ILIKE '%Sánchez%';

-- 🔍 PASO 5: Verificar TODOS los perfiles existentes
-- ============================================================

SELECT 
    p.id AS profile_id,
    p.first_name,
    p.last_name,
    p.phone,
    p.mobile,
    p.email AS profile_email, -- ¿Existe esta columna?
    p.created_at
FROM pss_dvnx.user_profiles p
ORDER BY p.id;

-- ═══════════════════════════════════════════════════════════════
-- 🎯 ANÁLISIS ESPERADO:
-- ═══════════════════════════════════════════════════════════════
-- 
-- POSIBLES CAUSAS:
-- 
-- 1. ❌ NO HAY RELACIÓN user_id en user_profiles
--    → La tabla user_profiles NO tiene FK a users
--    → El endpoint /profiles/me devuelve SIEMPRE el primer perfil
--    → SOLUCIÓN: Añadir columna user_id a user_profiles
-- 
-- 2. ❌ RELACIÓN INCORRECTA
--    → El user_id en user_profiles apunta al usuario equivocado
--    → wsanchez@global-retail.com tiene user_id=X pero profile.user_id=Y
--    → SOLUCIÓN: Actualizar la FK correctamente
-- 
-- 3. ❌ MÚLTIPLES PERFILES SIN DISTINCIÓN
--    → Hay varios perfiles pero sin forma de saber cuál pertenece a quién
--    → SOLUCIÓN: Añadir user_id y migrar datos existentes
-- 
-- 4. ❌ TOKEN JWT CON USER_ID INCORRECTO
--    → El token JWT generado en login contiene un user_id equivocado
--    → SOLUCIÓN: Verificar JwtService.GenerateToken() usa el user_id correcto
-- 
-- ═══════════════════════════════════════════════════════════════

-- 🔍 PASO 6: Verificar estructura completa de user_profiles
-- ============================================================

SELECT 
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE 
    table_schema = 'pss_dvnx' 
    AND table_name = 'user_profiles'
ORDER BY ordinal_position;

-- 🔍 PASO 7: Verificar foreign keys existentes
-- ============================================================

SELECT
    tc.constraint_name,
    tc.table_name,
    kcu.column_name,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu
    ON tc.constraint_name = kcu.constraint_name
    AND tc.table_schema = kcu.table_schema
JOIN information_schema.constraint_column_usage AS ccu
    ON ccu.constraint_name = tc.constraint_name
    AND ccu.table_schema = tc.table_schema
WHERE 
    tc.constraint_type = 'FOREIGN KEY' 
    AND tc.table_schema = 'pss_dvnx'
    AND tc.table_name = 'user_profiles';

-- ═══════════════════════════════════════════════════════════════
-- 📝 PRÓXIMOS PASOS SEGÚN RESULTADO:
-- ═══════════════════════════════════════════════════════════════
-- 
-- SI NO EXISTE user_id en user_profiles:
--   1. Crear migración para añadir columna user_id
--   2. Migrar datos existentes (mapear perfiles a usuarios)
--   3. Añadir FK constraint
--   4. Actualizar endpoint /profiles/me para filtrar por user_id del token
-- 
-- SI EXISTE user_id PERO ESTÁ MAL:
--   1. Verificar qué user_id tiene el perfil de Francisco Santos
--   2. Verificar qué user_id tiene el usuario wsanchez@global-retail.com
--   3. Actualizar la relación correctamente
-- 
-- SI TODO ESTÁ BIEN EN BBDD:
--   1. Verificar que el endpoint /profiles/me usa el user_id del token JWT
--   2. Decodificar el token JWT para ver qué user_id contiene
--   3. Verificar que JwtService.GenerateToken() usa el user_id correcto
-- 
-- ═══════════════════════════════════════════════════════════════
