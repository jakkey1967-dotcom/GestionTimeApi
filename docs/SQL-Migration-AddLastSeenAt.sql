-- ═══════════════════════════════════════════════════════════════════════════════
-- MIGRACIÓN: Agregar soporte de presencia (last_seen_at) a la tabla users
-- ═══════════════════════════════════════════════════════════════════════════════
-- Fecha: 2025-01-21
-- Descripción: Agrega el campo last_seen_at para rastrear usuarios online/offline
-- ═══════════════════════════════════════════════════════════════════════════════

SET search_path TO pss_dvnx;

-- Agregar columna last_seen_at (timestamp con zona horaria)
ALTER TABLE pss_dvnx.users 
ADD COLUMN IF NOT EXISTS last_seen_at TIMESTAMP WITH TIME ZONE;

-- Crear índice para optimizar búsquedas de usuarios online
CREATE INDEX IF NOT EXISTS idx_users_last_seen_at 
ON pss_dvnx.users(last_seen_at) 
WHERE last_seen_at IS NOT NULL;

-- Actualizar last_seen_at de usuarios activos a NOW() (para testing inicial)
UPDATE pss_dvnx.users 
SET last_seen_at = NOW() 
WHERE enabled = true;

-- Verificar cambios
SELECT 
    id,
    email,
    full_name,
    enabled,
    last_seen_at,
    CASE 
        WHEN last_seen_at IS NULL THEN 'Never'
        WHEN last_seen_at >= NOW() - INTERVAL '2 minutes' THEN 'ONLINE'
        ELSE 'OFFLINE'
    END AS status
FROM pss_dvnx.users
ORDER BY last_seen_at DESC NULLS LAST;

-- ═══════════════════════════════════════════════════════════════════════════════
-- ROLLBACK (si es necesario deshacer)
-- ═══════════════════════════════════════════════════════════════════════════════
-- DROP INDEX IF EXISTS pss_dvnx.idx_users_last_seen_at;
-- ALTER TABLE pss_dvnx.users DROP COLUMN IF EXISTS last_seen_at;
-- ═══════════════════════════════════════════════════════════════════════════════
