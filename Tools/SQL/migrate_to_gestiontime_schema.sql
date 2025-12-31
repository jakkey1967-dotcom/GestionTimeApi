-- ============================================
-- MIGRAR TABLAS DE public A gestiontime
-- Base de datos: Render PostgreSQL
-- ============================================

-- PASO 1: Crear schema gestiontime si no existe
CREATE SCHEMA IF NOT EXISTS gestiontime;

-- PASO 2: Mover todas las tablas de public a gestiontime
ALTER TABLE public.__EFMigrationsHistory SET SCHEMA gestiontime;
ALTER TABLE public.users SET SCHEMA gestiontime;
ALTER TABLE public.roles SET SCHEMA gestiontime;
ALTER TABLE public.user_roles SET SCHEMA gestiontime;
ALTER TABLE public.refresh_tokens SET SCHEMA gestiontime;
ALTER TABLE public.user_profiles SET SCHEMA gestiontime;
ALTER TABLE public.cliente SET SCHEMA gestiontime;
ALTER TABLE public.grupo SET SCHEMA gestiontime;
ALTER TABLE public.tipo SET SCHEMA gestiontime;
ALTER TABLE public.partesdetrabajo SET SCHEMA gestiontime;

-- PASO 3: Verificar que todas las tablas están en gestiontime
SELECT 
    schemaname,
    tablename
FROM 
    pg_tables
WHERE 
    schemaname IN ('public', 'gestiontime')
ORDER BY 
    schemaname, tablename;
