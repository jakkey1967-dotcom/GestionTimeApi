-- ================================================================
-- VERIFICACIÓN DE FRESHDESK AGENTS CACHE
-- ================================================================

-- 1. Verificar que la tabla existe
SELECT 
    '✅ Tabla freshdesk_agents_cache existe' as verificacion
FROM information_schema.tables
WHERE table_schema = 'pss_dvnx' 
  AND table_name = 'freshdesk_agents_cache';

-- 2. Estadísticas generales
SELECT 
    '📊 Estadísticas de agents' as seccion,
    COUNT(*) as total_agents,
    COUNT(*) FILTER (WHERE is_active = true) as agents_activos,
    COUNT(*) FILTER (WHERE is_active = false) as agents_inactivos,
    COUNT(DISTINCT agent_email) as emails_unicos
FROM pss_dvnx.freshdesk_agents_cache;

-- 3. Últimas sincronizaciones
SELECT 
    '🕐 Última sincronización' as seccion,
    MAX(synced_at) as ultima_sincronizacion,
    MAX(freshdesk_updated_at) as ultima_actualizacion_freshdesk,
    NOW() - MAX(synced_at) as tiempo_desde_sync
FROM pss_dvnx.freshdesk_agents_cache;

-- 4. Sample de agents (últimos 10)
SELECT 
    '👥 Sample de agents (últimos 10 actualizados)' as seccion,
    agent_id,
    agent_email,
    agent_name,
    agent_type,
    is_active,
    language,
    time_zone,
    last_login_at,
    freshdesk_updated_at,
    synced_at
FROM pss_dvnx.freshdesk_agents_cache
ORDER BY freshdesk_updated_at DESC NULLS LAST
LIMIT 10;

-- 5. Distribución por tipo de agente
SELECT 
    '📋 Distribución por tipo' as seccion,
    agent_type,
    COUNT(*) as cantidad,
    COUNT(*) FILTER (WHERE is_active = true) as activos
FROM pss_dvnx.freshdesk_agents_cache
GROUP BY agent_type
ORDER BY cantidad DESC;

-- 6. Distribución por idioma
SELECT 
    '🌐 Distribución por idioma' as seccion,
    language,
    COUNT(*) as cantidad
FROM pss_dvnx.freshdesk_agents_cache
WHERE language IS NOT NULL
GROUP BY language
ORDER BY cantidad DESC;

-- 7. Distribución por timezone
SELECT 
    '🕐 Distribución por timezone' as seccion,
    time_zone,
    COUNT(*) as cantidad
FROM pss_dvnx.freshdesk_agents_cache
WHERE time_zone IS NOT NULL
GROUP BY time_zone
ORDER BY cantidad DESC
LIMIT 10;

-- 8. Agents con último login reciente (últimos 30 días)
SELECT 
    '🔄 Agents activos recientemente (últimos 30 días)' as seccion,
    COUNT(*) as cantidad,
    MIN(last_login_at) as primer_login,
    MAX(last_login_at) as ultimo_login
FROM pss_dvnx.freshdesk_agents_cache
WHERE last_login_at >= NOW() - INTERVAL '30 days';

-- 9. Índices existentes
SELECT 
    '📑 Índices' as seccion,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'pss_dvnx'
  AND tablename = 'freshdesk_agents_cache'
ORDER BY indexname;

-- 10. Tamaño de la tabla
SELECT 
    '💾 Tamaño de datos' as seccion,
    pg_size_pretty(pg_total_relation_size('pss_dvnx.freshdesk_agents_cache')) as tamaño_total,
    pg_size_pretty(pg_relation_size('pss_dvnx.freshdesk_agents_cache')) as tamaño_tabla,
    pg_size_pretty(pg_total_relation_size('pss_dvnx.freshdesk_agents_cache') - pg_relation_size('pss_dvnx.freshdesk_agents_cache')) as tamaño_indices;
