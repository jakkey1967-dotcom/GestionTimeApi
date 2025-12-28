-- ============================================================================
-- MIGRATION: Add column state (INTEGER) keeping estado (VARCHAR)
-- Table: partesdetrabajo
-- ============================================================================
-- 
-- States:
--   0 = Open (before: 'activo', 'abierto')
--   1 = Paused
--   2 = Closed
--   3 = Sent
--   9 = Cancelled
--
-- STRATEGY: Add new column 'state' without removing 'estado'
--           This allows gradual migration and easy rollback
-- ============================================================================

-- STEP 1: Add new column state (INTEGER)
ALTER TABLE partesdetrabajo ADD COLUMN IF NOT EXISTS state INTEGER DEFAULT 0;

-- STEP 2: Migrate existing values from estado (varchar) to state (int)
UPDATE partesdetrabajo SET state = CASE
    WHEN LOWER(TRIM(estado)) IN ('activo', 'abierto', '0') THEN 0
    WHEN LOWER(TRIM(estado)) IN ('pausado', '1') THEN 1
    WHEN LOWER(TRIM(estado)) IN ('cerrado', '2') THEN 2
    WHEN LOWER(TRIM(estado)) IN ('enviado', '3') THEN 3
    WHEN LOWER(TRIM(estado)) IN ('anulado', '9') THEN 9
    ELSE 0
END
WHERE state IS NULL OR state = 0;

-- STEP 3: Verify migration
SELECT 
    estado AS old_estado,
    state AS new_state,
    COUNT(*) as count
FROM partesdetrabajo
GROUP BY estado, state
ORDER BY state;

-- STEP 4: Add NOT NULL constraint to state
ALTER TABLE partesdetrabajo ALTER COLUMN state SET NOT NULL;
ALTER TABLE partesdetrabajo ALTER COLUMN state SET DEFAULT 0;

-- STEP 5: Add constraint for valid values
ALTER TABLE partesdetrabajo DROP CONSTRAINT IF EXISTS ck_partes_state_valid;
ALTER TABLE partesdetrabajo ADD CONSTRAINT ck_partes_state_valid 
    CHECK (state IN (0, 1, 2, 3, 9));

-- STEP 6: Create index for state searches
CREATE INDEX IF NOT EXISTS idx_partes_state ON partesdetrabajo(state);

-- ============================================================================
-- FINAL VERIFICATION
-- ============================================================================
SELECT 
    state,
    CASE state
        WHEN 0 THEN 'Open'
        WHEN 1 THEN 'Paused'
        WHEN 2 THEN 'Closed'
        WHEN 3 THEN 'Sent'
        WHEN 9 THEN 'Cancelled'
        ELSE 'Unknown'
    END as state_name,
    estado as legacy_estado,
    COUNT(*) as count
FROM partesdetrabajo
GROUP BY state, estado
ORDER BY state;

-- ============================================================================
-- PHASE 2 (FUTURE): When everything is migrated and tested
-- ============================================================================
-- Uncomment these lines when you no longer need the estado column:
--
-- ALTER TABLE partesdetrabajo DROP COLUMN estado;
--
-- ============================================================================

-- ============================================================================
-- ROLLBACK (in case of error)
-- ============================================================================
-- ALTER TABLE partesdetrabajo DROP COLUMN state;
-- ============================================================================
