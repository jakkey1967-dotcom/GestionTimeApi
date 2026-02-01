-- ================================================================
-- CONSULTAS DE VERIFICACIÓN DE SINCRONIZACIÓN DE FRESHDESK
-- Base de datos: pss_dvnx
-- ================================================================

-- 1️⃣ CONTEO TOTAL DE TICKETS SINCRONIZADOS
-- ================================================================
SELECT 
    'Total de tickets sincronizados' as metrica,
    COUNT(*) as cantidad
FROM pss_dvnx.freshdesk_ticket_header;

-- 2️⃣ TICKETS POR ESTADO (con nombres descriptivos)
-- ================================================================
SELECT 
    status,
    CASE status
        WHEN 2 THEN 'Open'
        WHEN 3 THEN 'Pending'
        WHEN 4 THEN 'Resolved'
        WHEN 5 THEN 'Closed'
        WHEN 6 THEN 'Waiting on Customer'
        WHEN 7 THEN 'Waiting on Third Party'
        ELSE 'Unknown'
    END as status_name,
    COUNT(*) as cantidad
FROM pss_dvnx.freshdesk_ticket_header
GROUP BY status
ORDER BY status;

-- 3️⃣ TICKETS POR PRIORIDAD
-- ================================================================
SELECT 
    priority,
    CASE priority
        WHEN 1 THEN 'Low'
        WHEN 2 THEN 'Medium'
        WHEN 3 THEN 'High'
        WHEN 4 THEN 'Urgent'
        ELSE 'Unknown'
    END as priority_name,
    COUNT(*) as cantidad
FROM pss_dvnx.freshdesk_ticket_header
GROUP BY priority
ORDER BY priority;

-- 4️⃣ ESTADO DE LA ÚLTIMA SINCRONIZACIÓN
-- ================================================================
SELECT 
    scope,
    last_sync_at,
    last_result_count,
    last_max_updated_at,
    last_updated_since,
    last_error,
    EXTRACT(EPOCH FROM (NOW() - last_sync_at))/3600 as horas_desde_ultima_sync
FROM pss_dvnx.freshdesk_sync_state
WHERE scope = 'ticket_headers';

-- 5️⃣ TICKETS MÁS RECIENTES (últimos 10)
-- ================================================================
SELECT 
    ticket_id,
    subject,
    CASE status
        WHEN 2 THEN 'Open'
        WHEN 3 THEN 'Pending'
        WHEN 4 THEN 'Resolved'
        WHEN 5 THEN 'Closed'
        ELSE 'Other'
    END as status_name,
    CASE priority
        WHEN 1 THEN 'Low'
        WHEN 2 THEN 'Medium'
        WHEN 3 THEN 'High'
        WHEN 4 THEN 'Urgent'
        ELSE 'Unknown'
    END as priority_name,
    created_at,
    updated_at
FROM pss_dvnx.freshdesk_ticket_header
ORDER BY updated_at DESC
LIMIT 10;

-- 6️⃣ TICKETS POR TÉCNICO (responder_id)
-- ================================================================
SELECT 
    responder_id,
    COUNT(*) as tickets_asignados,
    COUNT(CASE WHEN status IN (2,3,6,7) THEN 1 END) as tickets_abiertos,
    COUNT(CASE WHEN status IN (4,5) THEN 1 END) as tickets_cerrados
FROM pss_dvnx.freshdesk_ticket_header
WHERE responder_id IS NOT NULL
GROUP BY responder_id
ORDER BY tickets_asignados DESC
LIMIT 10;

-- 7️⃣ TICKETS SIN ASIGNAR
-- ================================================================
SELECT 
    COUNT(*) as tickets_sin_asignar,
    COUNT(CASE WHEN status IN (2,3,6,7) THEN 1 END) as abiertos_sin_asignar,
    COUNT(CASE WHEN status IN (4,5) THEN 1 END) as cerrados_sin_asignar
FROM pss_dvnx.freshdesk_ticket_header
WHERE responder_id IS NULL;

-- 8️⃣ TICKETS CON TAGS (si existen)
-- ================================================================
SELECT 
    COUNT(*) as tickets_con_tags,
    COUNT(CASE WHEN tags IS NULL THEN 1 END) as tickets_sin_tags
FROM pss_dvnx.freshdesk_ticket_header;

-- 9️⃣ DISTRIBUCIÓN TEMPORAL (últimos 30 días)
-- ================================================================
SELECT 
    DATE(created_at) as fecha,
    COUNT(*) as tickets_creados
FROM pss_dvnx.freshdesk_ticket_header
WHERE created_at >= NOW() - INTERVAL '30 days'
GROUP BY DATE(created_at)
ORDER BY fecha DESC
LIMIT 10;

-- 🔟 TICKETS POR EMPRESA (top 10)
-- ================================================================
SELECT 
    company_id,
    company_name,
    COUNT(*) as total_tickets,
    COUNT(CASE WHEN status IN (2,3,6,7) THEN 1 END) as abiertos,
    COUNT(CASE WHEN status IN (4,5) THEN 1 END) as cerrados
FROM pss_dvnx.freshdesk_ticket_header
WHERE company_id IS NOT NULL
GROUP BY company_id, company_name
ORDER BY total_tickets DESC
LIMIT 10;

-- 1️⃣1️⃣ TICKETS ACTUALIZADOS HOY
-- ================================================================
SELECT 
    COUNT(*) as tickets_actualizados_hoy
FROM pss_dvnx.freshdesk_ticket_header
WHERE DATE(updated_at) = CURRENT_DATE;

-- 1️⃣2️⃣ RESUMEN GENERAL
-- ================================================================
SELECT 
    'Total Tickets' as metrica,
    COUNT(*)::text as valor
FROM pss_dvnx.freshdesk_ticket_header
UNION ALL
SELECT 
    'Tickets Abiertos',
    COUNT(*)::text
FROM pss_dvnx.freshdesk_ticket_header
WHERE status IN (2,3,6,7)
UNION ALL
SELECT 
    'Tickets Cerrados',
    COUNT(*)::text
FROM pss_dvnx.freshdesk_ticket_header
WHERE status IN (4,5)
UNION ALL
SELECT 
    'Tickets Sin Asignar',
    COUNT(*)::text
FROM pss_dvnx.freshdesk_ticket_header
WHERE responder_id IS NULL
UNION ALL
SELECT 
    'Tickets Urgentes',
    COUNT(*)::text
FROM pss_dvnx.freshdesk_ticket_header
WHERE priority = 4
UNION ALL
SELECT 
    'Última Sincronización',
    TO_CHAR(last_sync_at, 'YYYY-MM-DD HH24:MI:SS')
FROM pss_dvnx.freshdesk_sync_state
WHERE scope = 'ticket_headers';

-- 1️⃣3️⃣ VERIFICAR INTEGRIDAD DE DATOS
-- ================================================================
SELECT 
    'Tickets sin subject' as verificacion,
    COUNT(*) as cantidad
FROM pss_dvnx.freshdesk_ticket_header
WHERE subject IS NULL OR subject = ''
UNION ALL
SELECT 
    'Tickets sin created_at',
    COUNT(*)
FROM pss_dvnx.freshdesk_ticket_header
WHERE created_at IS NULL
UNION ALL
SELECT 
    'Tickets sin updated_at',
    COUNT(*)
FROM pss_dvnx.freshdesk_ticket_header
WHERE updated_at IS NULL
UNION ALL
SELECT 
    'Tickets con status inválido',
    COUNT(*)
FROM pss_dvnx.freshdesk_ticket_header
WHERE status NOT IN (2,3,4,5,6,7);
