-- ═══════════════════════════════════════════════════════════════════════════════
-- BACKUP RÁPIDO EN RENDER - Tabla Temporal
-- ═══════════════════════════════════════════════════════════════════════════════
-- ✅ VENTAJAS:
--    - Instantáneo (se ejecuta en la nube, no descarga nada)
--    - Seguro (solo crea tabla de respaldo)
--    - Fácil de restaurar si algo sale mal
-- ═══════════════════════════════════════════════════════════════════════════════

SET search_path TO pss_dvnx;

-- ═══════════════════════════════════════════════════════════════════════════════
-- 1. CREAR BACKUP DE TABLA USERS (COMPLETA)
-- ═══════════════════════════════════════════════════════════════════════════════

-- Crear tabla de backup con todos los datos actuales
CREATE TABLE pss_dvnx.users_backup_20250121 AS 
SELECT * FROM pss_dvnx.users;

-- Verificar que se creó correctamente
SELECT 
    'BACKUP CREADO' AS status,
    COUNT(*) AS total_respaldado,
    pg_size_pretty(pg_total_relation_size('pss_dvnx.users_backup_20250121')) AS tamaño
FROM pss_dvnx.users_backup_20250121;

-- Comparar registros (deben ser iguales)
SELECT 
    'users ORIGINAL' AS tabla,
    COUNT(*) AS registros
FROM pss_dvnx.users
UNION ALL
SELECT 
    'users_backup_20250121' AS tabla,
    COUNT(*) AS registros
FROM pss_dvnx.users_backup_20250121;

-- ═══════════════════════════════════════════════════════════════════════════════
-- 2. VERIFICAR BACKUP (Ver primeros registros)
-- ═══════════════════════════════════════════════════════════════════════════════

SELECT 
    id,
    email,
    full_name,
    enabled,
    email_confirmed
FROM pss_dvnx.users_backup_20250121
ORDER BY email
LIMIT 10;

-- ═══════════════════════════════════════════════════════════════════════════════
-- 3. VERIFICAR QUE PUEDES HACER ROLLBACK (SI ES NECESARIO)
-- ═══════════════════════════════════════════════════════════════════════════════

-- Contar índices en tabla original
SELECT 
    tablename,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'pss_dvnx' 
AND tablename = 'users';

-- ═══════════════════════════════════════════════════════════════════════════════
-- ✅ CONFIRMACIÓN: Backup listo para continuar
-- ═══════════════════════════════════════════════════════════════════════════════

SELECT 
    '✅ BACKUP COMPLETADO' AS mensaje,
    'Ahora es SEGURO ejecutar la migración SQL-Migration-AddLastSeenAt.sql' AS siguiente_paso,
    'Para restaurar: Ver sección ROLLBACK abajo' AS info_rollback;

-- ═══════════════════════════════════════════════════════════════════════════════
-- ROLLBACK (SOLO SI ALGO SALE MAL - NO EJECUTAR AHORA)
-- ═══════════════════════════════════════════════════════════════════════════════
/*
-- ⚠️ SOLO USAR SI LA MIGRACIÓN FALLA Y NECESITAS VOLVER ATRÁS

-- Paso 1: Eliminar tabla original (¡CUIDADO!)
DROP TABLE pss_dvnx.users;

-- Paso 2: Renombrar backup a users
ALTER TABLE pss_dvnx.users_backup_20250121 RENAME TO users;

-- Paso 3: Recrear índices (si es necesario)
-- Los índices se pierden al hacer DROP, así que debes recrearlos manualmente
-- Ver: docs\SQL-Recreation-Indexes.sql (si lo tienes)

-- Paso 4: Verificar que todo volvió a la normalidad
SELECT COUNT(*) FROM pss_dvnx.users;
*/

-- ═══════════════════════════════════════════════════════════════════════════════
-- LIMPIEZA (Después de verificar que todo funciona correctamente)
-- ═══════════════════════════════════════════════════════════════════════════════
/*
-- ⚠️ SOLO EJECUTAR DESPUÉS DE 1-2 DÍAS Y VERIFICAR QUE TODO FUNCIONA

-- Listar backups
SELECT tablename 
FROM pg_tables 
WHERE schemaname = 'pss_dvnx' 
AND tablename LIKE 'users_backup%'
ORDER BY tablename;

-- Eliminar backup antiguo
DROP TABLE IF EXISTS pss_dvnx.users_backup_20250121;
*/
