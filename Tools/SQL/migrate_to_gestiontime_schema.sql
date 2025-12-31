-- ============================================
-- MIGRAR TABLAS DE public A gestiontime
-- Base de datos: Render PostgreSQL
-- ============================================

-- PASO 1: Crear schema gestiontime si no existe
CREATE SCHEMA IF NOT EXISTS gestiontime;

-- PASO 2: Mover todas las tablas de public a gestiontime
ALTER TABLE IF EXISTS public."__EFMigrationsHistory" SET SCHEMA gestiontime;
ALTER TABLE IF EXISTS public.users SET SCHEMA gestiontime;
ALTER TABLE IF EXISTS public.roles SET SCHEMA gestiontime;
ALTER TABLE IF EXISTS public.user_roles SET SCHEMA gestiontime;
ALTER TABLE IF EXISTS public.refresh_tokens SET SCHEMA gestiontime;
ALTER TABLE IF EXISTS public.user_profiles SET SCHEMA gestiontime;
ALTER TABLE IF EXISTS public.cliente SET SCHEMA gestiontime;
ALTER TABLE IF EXISTS public.grupo SET SCHEMA gestiontime;
ALTER TABLE IF EXISTS public.tipo SET SCHEMA gestiontime;
ALTER TABLE IF EXISTS public.partesdetrabajo SET SCHEMA gestiontime;

-- PASO 3: Verificar que todas las tablas están en gestiontime
SELECT 
    schemaname,
    tablename,
    (SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = schemaname AND table_name = tablename) as columnas
FROM 
    pg_tables
WHERE 
    schemaname IN ('public', 'gestiontime')
ORDER BY 
    schemaname, tablename;
