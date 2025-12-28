-- ============================================================================
-- MIGRACIÓN: Cambiar columna estado de VARCHAR a INTEGER
-- Tabla: partesdetrabajo
-- ============================================================================
-- 
-- Estados:
--   0 = Abierto (antes: 'activo', 'abierto')
--   1 = Pausado
--   2 = Cerrado
--   3 = Enviado
--   9 = Anulado
--
-- INSTRUCCIONES:
--   1. Hacer backup de la tabla antes de ejecutar
--   2. Ejecutar en orden los bloques de este script
--   3. Verificar los datos después de cada paso
-- ============================================================================

-- PASO 1: Backup de seguridad (opcional pero recomendado)
-- CREATE TABLE partesdetrabajo_backup AS SELECT * FROM partesdetrabajo;

-- PASO 2: Añadir columna temporal para el nuevo estado
ALTER TABLE partesdetrabajo ADD COLUMN estado_nuevo INTEGER;

-- PASO 3: Migrar los valores existentes
UPDATE partesdetrabajo SET estado_nuevo = CASE
    WHEN LOWER(TRIM(estado)) IN ('activo', 'abierto', '0') THEN 0
    WHEN LOWER(TRIM(estado)) IN ('pausado', '1') THEN 1
    WHEN LOWER(TRIM(estado)) IN ('cerrado', '2') THEN 2
    WHEN LOWER(TRIM(estado)) IN ('enviado', '3') THEN 3
    WHEN LOWER(TRIM(estado)) IN ('anulado', '9') THEN 9
    ELSE 0  -- Por defecto, Abierto
END;

-- PASO 4: Verificar la migración (revisar que no haya NULLs ni valores inesperados)
SELECT 
    estado AS estado_antiguo,
    estado_nuevo,
    COUNT(*) as cantidad
FROM partesdetrabajo
GROUP BY estado, estado_nuevo
ORDER BY estado_nuevo;

-- PASO 5: Eliminar la columna antigua
ALTER TABLE partesdetrabajo DROP COLUMN estado;

-- PASO 6: Renombrar la columna nueva
ALTER TABLE partesdetrabajo RENAME COLUMN estado_nuevo TO estado;

-- PASO 7: Añadir NOT NULL y valor por defecto
ALTER TABLE partesdetrabajo ALTER COLUMN estado SET NOT NULL;
ALTER TABLE partesdetrabajo ALTER COLUMN estado SET DEFAULT 0;

-- PASO 8: Añadir constraint para validar valores permitidos (opcional)
ALTER TABLE partesdetrabajo ADD CONSTRAINT ck_partes_estado_valido 
    CHECK (estado IN (0, 1, 2, 3, 9));

-- PASO 9: Crear índice para búsquedas por estado (opcional pero recomendado)
CREATE INDEX IF NOT EXISTS idx_partes_estado ON partesdetrabajo(estado);

-- ============================================================================
-- VERIFICACIÓN FINAL
-- ============================================================================
SELECT 
    estado,
    CASE estado
        WHEN 0 THEN 'Abierto'
        WHEN 1 THEN 'Pausado'
        WHEN 2 THEN 'Cerrado'
        WHEN 3 THEN 'Enviado'
        WHEN 9 THEN 'Anulado'
        ELSE 'Desconocido'
    END as estado_nombre,
    COUNT(*) as cantidad
FROM partesdetrabajo
GROUP BY estado
ORDER BY estado;

-- ============================================================================
-- ROLLBACK (en caso de error, usar el backup)
-- ============================================================================
-- DROP TABLE partesdetrabajo;
-- ALTER TABLE partesdetrabajo_backup RENAME TO partesdetrabajo;
