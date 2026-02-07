-- ========================================
-- CREAR TABLA __EFMigrationsHistory
-- ========================================
-- Esta tabla es requerida por Entity Framework Core
-- para rastrear qué migraciones se han aplicado

-- 1. Conectar al schema correcto
SET search_path TO pss_dvnx, public;

-- 2. Crear tabla si no existe
CREATE TABLE IF NOT EXISTS pss_dvnx."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

-- 3. Verificar que se creó
SELECT 
    schemaname, 
    tablename, 
    tableowner
FROM pg_tables
WHERE schemaname = 'pss_dvnx' 
  AND tablename = '__EFMigrationsHistory';

-- 4. Ver contenido (debe estar vacío inicialmente)
SELECT * FROM pss_dvnx."__EFMigrationsHistory";

COMMENT ON TABLE pss_dvnx."__EFMigrationsHistory" IS 'Tabla de control de migraciones de Entity Framework Core';
