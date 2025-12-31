-- ============================================
-- MIGRAR TABLAS DE gestiontime A pss_dvnx
-- Base de datos: Render PostgreSQL
-- ============================================

-- PASO 1: Crear schema pss_dvnx si no existe
CREATE SCHEMA IF NOT EXISTS pss_dvnx;

-- PASO 2: Mover todas las tablas de gestiontime a pss_dvnx
ALTER TABLE IF EXISTS gestiontime."__EFMigrationsHistory" SET SCHEMA pss_dvnx;
ALTER TABLE IF EXISTS gestiontime.users SET SCHEMA pss_dvnx;
ALTER TABLE IF EXISTS gestiontime.roles SET SCHEMA pss_dvnx;
ALTER TABLE IF EXISTS gestiontime.user_roles SET SCHEMA pss_dvnx;
ALTER TABLE IF EXISTS gestiontime.refresh_tokens SET SCHEMA pss_dvnx;
ALTER TABLE IF EXISTS gestiontime.user_profiles SET SCHEMA pss_dvnx;
ALTER TABLE IF EXISTS gestiontime.cliente SET SCHEMA pss_dvnx;
ALTER TABLE IF EXISTS gestiontime.grupo SET SCHEMA pss_dvnx;
ALTER TABLE IF EXISTS gestiontime.tipo SET SCHEMA pss_dvnx;
ALTER TABLE IF EXISTS gestiontime.partesdetrabajo SET SCHEMA pss_dvnx;

-- PASO 3: Verificar que todas las tablas están en pss_dvnx
SELECT 
    schemaname,
    tablename,
    (SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = schemaname AND table_name = tablename) as columnas
FROM 
    pg_tables
WHERE 
    schemaname IN ('public', 'gestiontime', 'pss_dvnx')
ORDER BY 
    schemaname, tablename;
