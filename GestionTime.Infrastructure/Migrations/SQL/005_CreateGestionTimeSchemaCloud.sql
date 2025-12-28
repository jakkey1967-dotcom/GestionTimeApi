-- ============================================================================
-- MIGRATION: Create gestiontime schema and move tables from public
-- Database: pss_dvnx (Render Cloud)
-- ============================================================================

-- STEP 1: Create gestiontime schema
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.schemata 
        WHERE schema_name = 'gestiontime'
    ) THEN
        CREATE SCHEMA gestiontime;
        RAISE NOTICE 'Schema gestiontime created';
    ELSE
        RAISE NOTICE 'Schema gestiontime already exists';
    END IF;
END $$;

-- STEP 2: Move existing tables to gestiontime schema
-- Check if tables exist in public schema first
DO $$
DECLARE
    table_record RECORD;
    table_names text[] := ARRAY['users', 'roles', 'user_roles', 'refresh_tokens', 'user_profiles', 
                                'cliente', 'grupo', 'tipo', 'partesdetrabajo', '__EFMigrationsHistory'];
BEGIN
    FOR i IN 1..array_length(table_names, 1) LOOP
        IF EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'public' 
            AND table_name = table_names[i]
        ) THEN
            EXECUTE format('ALTER TABLE public.%I SET SCHEMA gestiontime', table_names[i]);
            RAISE NOTICE 'Moved table % to gestiontime schema', table_names[i];
        ELSE
            RAISE NOTICE 'Table % does not exist in public schema', table_names[i];
        END IF;
    END LOOP;
END $$;

-- STEP 3: Verify migration
SELECT 
    schemaname, 
    tablename,
    'SUCCESS' as status
FROM pg_tables 
WHERE schemaname = 'gestiontime'
ORDER BY tablename;

-- STEP 4: Count records in main tables
SELECT 
    'users' as table_name,
    COUNT(*) as record_count
FROM gestiontime.users
UNION ALL
SELECT 
    'roles' as table_name,
    COUNT(*) as record_count
FROM gestiontime.roles
UNION ALL
SELECT 
    'partesdetrabajo' as table_name,
    COUNT(*) as record_count
FROM gestiontime.partesdetrabajo
UNION ALL
SELECT 
    'cliente' as table_name,
    COUNT(*) as record_count
FROM gestiontime.cliente
ORDER BY table_name;

-- STEP 5: Verify data integrity
SELECT 
    'Data verification completed' as message,
    NOW() as timestamp;

-- ============================================================================
-- ROLLBACK INSTRUCTIONS (if needed)
-- ============================================================================
-- To rollback, execute these commands:
-- 
-- ALTER TABLE gestiontime.users SET SCHEMA public;
-- ALTER TABLE gestiontime.roles SET SCHEMA public;
-- ALTER TABLE gestiontime.user_roles SET SCHEMA public;
-- ALTER TABLE gestiontime.refresh_tokens SET SCHEMA public;
-- ALTER TABLE gestiontime.user_profiles SET SCHEMA public;
-- ALTER TABLE gestiontime.cliente SET SCHEMA public;
-- ALTER TABLE gestiontime.grupo SET SCHEMA public;
-- ALTER TABLE gestiontime.tipo SET SCHEMA public;
-- ALTER TABLE gestiontime.partesdetrabajo SET SCHEMA public;
-- ALTER TABLE gestiontime."__EFMigrationsHistory" SET SCHEMA public;
-- DROP SCHEMA IF EXISTS gestiontime;
-- ============================================================================