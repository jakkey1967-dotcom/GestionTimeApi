-- ═══════════════════════════════════════════════════════════════════════════════
-- BACKUP MANUAL - Schema pss_dvnx (PostgreSQL)
-- ═══════════════════════════════════════════════════════════════════════════════
-- Ejecutar ANTES de cualquier migración
-- ═══════════════════════════════════════════════════════════════════════════════

-- OPCIÓN A: Backup usando pg_dump (línea de comandos)
-- ═══════════════════════════════════════════════════════════════════════════════
-- Windows PowerShell / CMD:
-- pg_dump -h HOST -U USER -d DATABASE -n pss_dvnx -F p -f backup_pss_dvnx_20250121.sql

-- Linux / Mac:
-- pg_dump -h HOST -U USER -d DATABASE -n pss_dvnx -F p -f backup_pss_dvnx_20250121.sql

-- Comprimir (opcional):
-- gzip backup_pss_dvnx_20250121.sql


-- OPCIÓN B: Backup de tabla users específica (más rápido)
-- ═══════════════════════════════════════════════════════════════════════════════

-- 1. Exportar estructura de la tabla users
SELECT 
    'CREATE TABLE ' || schemaname || '.' || tablename || ' (' || 
    string_agg(column_name || ' ' || data_type, ', ') || ');'
FROM information_schema.columns
WHERE table_schema = 'pss_dvnx' AND table_name = 'users'
GROUP BY schemaname, tablename;

-- 2. Exportar datos de la tabla users a CSV (para backup)
\COPY pss_dvnx.users TO 'backup_users_20250121.csv' WITH CSV HEADER;

-- O usando SQL:
COPY pss_dvnx.users TO '/tmp/backup_users_20250121.csv' WITH CSV HEADER;


-- OPCIÓN C: Backup solo de columnas críticas (snapshot rápido)
-- ═══════════════════════════════════════════════════════════════════════════════

-- Crear tabla temporal con snapshot de usuarios
CREATE TABLE pss_dvnx.users_backup_20250121 AS 
SELECT * FROM pss_dvnx.users;

-- Verificar backup
SELECT COUNT(*) AS total_respaldado FROM pss_dvnx.users_backup_20250121;

-- Para restaurar (si es necesario):
-- DROP TABLE pss_dvnx.users;
-- ALTER TABLE pss_dvnx.users_backup_20250121 RENAME TO users;


-- OPCIÓN D: Backup en formato pgAdmin (GUI)
-- ═══════════════════════════════════════════════════════════════════════════════
/*
1. Abrir pgAdmin
2. Click derecho en schema "pss_dvnx"
3. Backup... > Format: Plain > Filename: backup_pss_dvnx.sql
4. En "Dump Options" > "Data Options": Marcar "Only data" o "Only schema" según necesites
5. Click "Backup"
*/


-- RESTAURACIÓN (si algo sale mal)
-- ═══════════════════════════════════════════════════════════════════════════════

-- Desde archivo SQL:
-- psql -h HOST -U USER -d DATABASE -f backup_pss_dvnx_20250121.sql

-- Desde tabla temporal:
-- DROP TABLE pss_dvnx.users;
-- ALTER TABLE pss_dvnx.users_backup_20250121 RENAME TO users;

-- Desde CSV:
-- \COPY pss_dvnx.users FROM 'backup_users_20250121.csv' WITH CSV HEADER;


-- VERIFICACIÓN POST-BACKUP
-- ═══════════════════════════════════════════════════════════════════════════════

-- Verificar que el backup existe y tiene datos
SELECT 
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size,
    n_live_tup AS row_count
FROM pg_stat_user_tables
WHERE schemaname = 'pss_dvnx' AND tablename LIKE '%backup%'
ORDER BY tablename;

-- Comparar counts entre tabla original y backup
SELECT 
    'users' AS tabla,
    COUNT(*) AS registros
FROM pss_dvnx.users
UNION ALL
SELECT 
    'users_backup_20250121' AS tabla,
    COUNT(*) AS registros
FROM pss_dvnx.users_backup_20250121;


-- LIMPIEZA DE BACKUPS ANTIGUOS (ejecutar después de verificar)
-- ═══════════════════════════════════════════════════════════════════════════════

-- Listar tablas de backup
SELECT tablename 
FROM pg_tables 
WHERE schemaname = 'pss_dvnx' 
AND tablename LIKE '%backup%'
ORDER BY tablename;

-- Eliminar backups antiguos (¡CUIDADO! Verificar fecha primero)
-- DROP TABLE IF EXISTS pss_dvnx.users_backup_20250120;
