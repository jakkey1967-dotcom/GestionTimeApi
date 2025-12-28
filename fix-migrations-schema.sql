-- Script para mover la tabla __EFMigrationsHistory al schema correcto
-- Este script debe ejecutarse en la base de datos PostgreSQL

-- Crear schema gestiontime si no existe
CREATE SCHEMA IF NOT EXISTS gestiontime;

-- Verificar si la tabla existe en public
DO $$
BEGIN
    IF EXISTS (SELECT FROM information_schema.tables 
               WHERE table_schema = 'public' 
               AND table_name = '__EFMigrationsHistory') THEN
        
        -- Mover la tabla al schema gestiontime
        ALTER TABLE public."__EFMigrationsHistory" 
        SET SCHEMA gestiontime;
        
        RAISE NOTICE 'Tabla __EFMigrationsHistory movida al schema gestiontime';
    ELSE
        RAISE NOTICE 'Tabla __EFMigrationsHistory no encontrada en schema public';
    END IF;
END
$$;

-- Verificar el contenido de la tabla de migraciones
SELECT * FROM gestiontime."__EFMigrationsHistory";