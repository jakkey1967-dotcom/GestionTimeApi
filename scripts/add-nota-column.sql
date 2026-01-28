-- Migración: Agregar columna 'nota' a la tabla cliente
-- Fecha: 2026-01-25
-- Descripción: Agrega la columna 'nota' (TEXT) a la tabla pss_dvnx.cliente

-- Agregar columna si no existe
ALTER TABLE pss_dvnx.cliente 
ADD COLUMN IF NOT EXISTS nota TEXT;

-- Verificar la columna
SELECT 
    column_name, 
    data_type, 
    is_nullable,
    column_default
FROM information_schema.columns 
WHERE table_schema = 'pss_dvnx' 
  AND table_name = 'cliente' 
  AND column_name = 'nota';

-- Contar clientes con nota
SELECT 
    COUNT(*) FILTER (WHERE nota IS NOT NULL AND nota != '') AS con_nota,
    COUNT(*) FILTER (WHERE nota IS NULL OR nota = '') AS sin_nota,
    COUNT(*) AS total
FROM pss_dvnx.cliente;
