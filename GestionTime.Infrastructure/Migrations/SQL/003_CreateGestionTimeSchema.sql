-- ============================================================================
-- MIGRATION: Create GestionTime schema
-- ============================================================================

-- Crear schema si no existe
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.schemata 
        WHERE schema_name = 'gestiontime'
    ) THEN
        CREATE SCHEMA gestiontime;
    END IF;
END $$;

-- Verificar que se creó
SELECT schema_name FROM information_schema.schemata WHERE schema_name = 'gestiontime';

-- ============================================================================
-- ROLLBACK (en caso de error)
-- ============================================================================
-- DROP SCHEMA IF EXISTS gestiontime CASCADE;
-- ============================================================================