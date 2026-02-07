-- ========================================
-- ACTIVIDAD DE USUARIOS BETA TESTERS (CORREGIDO)
-- ========================================

SET search_path = pss_dvnx, public;

-- 1. RESUMEN GENERAL DE USUARIOS
SELECT 
    '=== RESUMEN GENERAL DE USUARIOS ===' as seccion;

SELECT 
    u.id,
    u.full_name as nombre,
    u.email,
    u.enabled as activo,
    us.created_at as fecha_registro,
    us.last_seen_at as ultima_sesion,
    CASE 
        WHEN us.last_seen_at IS NULL THEN 'Nunca'
        WHEN us.last_seen_at >= NOW() - INTERVAL '1 day' THEN 'Hoy'
        WHEN us.last_seen_at >= NOW() - INTERVAL '7 days' THEN 'Esta semana'
        WHEN us.last_seen_at >= NOW() - INTERVAL '30 days' THEN 'Este mes'
        ELSE 'Hace mas de 1 mes'
    END as estado_actividad,
    (SELECT COUNT(*) FROM pss_dvnx.partesdetrabajo p WHERE p.id_usuario = u.id) as total_partes
FROM pss_dvnx.users u
LEFT JOIN LATERAL (
    SELECT created_at, last_seen_at 
    FROM pss_dvnx.user_sessions 
    WHERE user_id = u.id 
    ORDER BY last_seen_at DESC NULLS LAST 
    LIMIT 1
) us ON true
ORDER BY us.last_seen_at DESC NULLS LAST;

-- 2. ACTIVIDAD DE LOGIN POR USUARIO
SELECT 
    '=== ACTIVIDAD DE LOGIN ===' as seccion;

SELECT 
    u.full_name as nombre,
    u.email,
    us.last_seen_at as ultima_sesion,
    CASE 
        WHEN us.last_seen_at IS NULL THEN 'Nunca ha iniciado sesion'
        ELSE CONCAT(
            EXTRACT(DAY FROM (NOW() - us.last_seen_at))::INT, ' dias, ',
            EXTRACT(HOUR FROM (NOW() - us.last_seen_at))::INT, ' horas'
        )
    END as hace_cuanto,
    us.ip as ultima_ip,
    u.enabled as activo
FROM pss_dvnx.users u
LEFT JOIN LATERAL (
    SELECT last_seen_at, ip 
    FROM pss_dvnx.user_sessions 
    WHERE user_id = u.id 
    ORDER BY last_seen_at DESC NULLS LAST 
    LIMIT 1
) us ON true
ORDER BY us.last_seen_at DESC NULLS LAST;

-- 3. PARTES POR USUARIO (TOTAL)
SELECT 
    '=== PARTES CREADOS POR USUARIO (TOTAL) ===' as seccion;

SELECT 
    u.full_name as usuario,
    u.email,
    COUNT(p.id) as total_partes,
    MIN(p.fecha_trabajo) as primer_parte,
    MAX(p.fecha_trabajo) as ultimo_parte,
    ROUND(
        CAST(SUM(
            EXTRACT(EPOCH FROM (
                (p.fecha_trabajo + p.hora_fin::time) - 
                (p.fecha_trabajo + p.hora_inicio::time)
            )) / 3600.0
        ) AS NUMERIC), 2
    ) as horas_totales
FROM pss_dvnx.users u
LEFT JOIN pss_dvnx.partesdetrabajo p ON p.id_usuario = u.id
GROUP BY u.id, u.full_name, u.email
ORDER BY total_partes DESC;

-- 4. ACTIVIDAD DIARIA POR USUARIO (ULTIMOS 30 DIAS)
SELECT 
    '=== ACTIVIDAD DIARIA POR USUARIO (ULTIMOS 30 DIAS) ===' as seccion;

WITH ultimos_30_dias AS (
    SELECT generate_series(
        CURRENT_DATE - INTERVAL '29 days',
        CURRENT_DATE,
        INTERVAL '1 day'
    )::date as fecha
)
SELECT 
    u.full_name as usuario,
    d.fecha,
    TO_CHAR(d.fecha, 'Day') as dia_semana,
    COUNT(p.id) as partes_creados,
    ROUND(
        CAST(SUM(
            EXTRACT(EPOCH FROM (
                (p.fecha_trabajo + p.hora_fin::time) - 
                (p.fecha_trabajo + p.hora_inicio::time)
            )) / 3600.0
        ) AS NUMERIC), 2
    ) as horas_registradas
FROM pss_dvnx.users u
CROSS JOIN ultimos_30_dias d
LEFT JOIN pss_dvnx.partesdetrabajo p ON p.fecha_trabajo = d.fecha AND p.id_usuario = u.id
WHERE EXISTS (SELECT 1 FROM pss_dvnx.partesdetrabajo WHERE id_usuario = u.id)
GROUP BY u.id, u.full_name, d.fecha
HAVING COUNT(p.id) > 0
ORDER BY u.full_name, d.fecha DESC;

-- 5. PARTES POR USUARIO Y FECHA (DETALLADO)
SELECT 
    '=== PARTES POR USUARIO Y FECHA (ULTIMOS 15 DIAS) ===' as seccion;

SELECT 
    p.fecha_trabajo,
    u.full_name as usuario,
    COUNT(p.id) as partes_del_dia,
    STRING_AGG(
        CONCAT(
            p.hora_inicio, '-', p.hora_fin, 
            ' (', c.nombre, ')'
        ), 
        ', ' 
        ORDER BY p.hora_inicio
    ) as detalle_partes,
    ROUND(
        CAST(SUM(
            EXTRACT(EPOCH FROM (
                (p.fecha_trabajo + p.hora_fin::time) - 
                (p.fecha_trabajo + p.hora_inicio::time)
            )) / 3600.0
        ) AS NUMERIC), 2
    ) as horas_dia
FROM pss_dvnx.partesdetrabajo p
INNER JOIN pss_dvnx.users u ON u.id = p.id_usuario
INNER JOIN pss_dvnx.cliente c ON c.id = p.id_cliente
WHERE p.fecha_trabajo >= CURRENT_DATE - INTERVAL '15 days'
GROUP BY p.fecha_trabajo, u.id, u.full_name
ORDER BY p.fecha_trabajo DESC, u.full_name;

-- 6. USUARIOS SIN ACTIVIDAD
SELECT 
    '=== USUARIOS SIN ACTIVIDAD ===' as seccion;

SELECT 
    u.full_name as nombre,
    u.email,
    u.enabled as activo,
    us.created_at as registrado_el,
    EXTRACT(DAY FROM (NOW() - COALESCE(us.created_at, NOW())))::INT as dias_desde_registro,
    us.last_seen_at as ultima_sesion,
    (SELECT COUNT(*) FROM pss_dvnx.partesdetrabajo p WHERE p.id_usuario = u.id) as partes_creados
FROM pss_dvnx.users u
LEFT JOIN LATERAL (
    SELECT created_at, last_seen_at 
    FROM pss_dvnx.user_sessions 
    WHERE user_id = u.id 
    ORDER BY last_seen_at DESC NULLS LAST 
    LIMIT 1
) us ON true
WHERE 
(
    us.last_seen_at IS NULL 
    OR us.last_seen_at < NOW() - INTERVAL '7 days'
)
OR (SELECT COUNT(*) FROM pss_dvnx.partesdetrabajo p WHERE p.id_usuario = u.id) = 0
ORDER BY us.created_at DESC NULLS LAST;

-- 7. TOP 5 USUARIOS MAS ACTIVOS
SELECT 
    '=== TOP 5 USUARIOS MAS ACTIVOS ===' as seccion;

SELECT 
    u.full_name as nombre,
    u.email,
    COUNT(p.id) as total_partes,
    COUNT(DISTINCT p.fecha_trabajo) as dias_trabajados,
    ROUND(
        CAST(SUM(
            EXTRACT(EPOCH FROM (
                (p.fecha_trabajo + p.hora_fin::time) - 
                (p.fecha_trabajo + p.hora_inicio::time)
            )) / 3600.0
        ) AS NUMERIC), 2
    ) as horas_totales,
    MAX(p.fecha_trabajo) as ultimo_parte,
    (SELECT last_seen_at FROM pss_dvnx.user_sessions WHERE user_id = u.id ORDER BY last_seen_at DESC LIMIT 1) as ultima_sesion
FROM pss_dvnx.users u
INNER JOIN pss_dvnx.partesdetrabajo p ON p.id_usuario = u.id
GROUP BY u.id, u.full_name, u.email
ORDER BY total_partes DESC
LIMIT 5;

-- 8. RESUMEN POR USUARIO
SELECT 
    '=== RESUMEN POR USUARIO ===' as seccion;

SELECT 
    u.full_name as usuario,
    u.email,
    u.enabled as activo,
    us.ultima_sesion,
    CASE 
        WHEN us.ultima_sesion IS NULL THEN 'Nunca ingresó'
        WHEN us.ultima_sesion >= NOW() - INTERVAL '1 day' THEN 'Activo hoy'
        WHEN us.ultima_sesion >= NOW() - INTERVAL '7 days' THEN 'Activo esta semana'
        WHEN us.ultima_sesion >= NOW() - INTERVAL '30 days' THEN 'Activo este mes'
        ELSE 'Inactivo > 30 días'
    END as estado,
    COALESCE(p.total_partes, 0) as total_partes,
    COALESCE(p.partes_ultima_semana, 0) as partes_7dias,
    COALESCE(p.partes_ultimo_mes, 0) as partes_30dias,
    COALESCE(p.horas_totales, 0) as horas_totales,
    COALESCE(p.dias_trabajados, 0) as dias_trabajados,
    p.ultimo_parte
FROM pss_dvnx.users u
LEFT JOIN LATERAL (
    SELECT last_seen_at as ultima_sesion 
    FROM pss_dvnx.user_sessions 
    WHERE user_id = u.id 
    ORDER BY last_seen_at DESC NULLS LAST 
    LIMIT 1
) us ON true
LEFT JOIN LATERAL (
    SELECT 
        COUNT(*) as total_partes,
        COUNT(*) FILTER (WHERE fecha_trabajo >= CURRENT_DATE - INTERVAL '7 days') as partes_ultima_semana,
        COUNT(*) FILTER (WHERE fecha_trabajo >= CURRENT_DATE - INTERVAL '30 days') as partes_ultimo_mes,
        ROUND(
            CAST(SUM(
                EXTRACT(EPOCH FROM (
                    (fecha_trabajo + hora_fin::time) - 
                    (fecha_trabajo + hora_inicio::time)
                )) / 3600.0
            ) AS NUMERIC), 2
        ) as horas_totales,
        COUNT(DISTINCT fecha_trabajo) as dias_trabajados,
        MAX(fecha_trabajo) as ultimo_parte
    FROM pss_dvnx.partesdetrabajo 
    WHERE id_usuario = u.id
) p ON true
ORDER BY COALESCE(p.total_partes, 0) DESC, us.ultima_sesion DESC NULLS LAST;

-- 9. RESUMEN EJECUTIVO GLOBAL
SELECT 
    '=== RESUMEN EJECUTIVO GLOBAL ===' as seccion;

SELECT 
    (SELECT COUNT(*) FROM pss_dvnx.users) as total_usuarios,
    (SELECT COUNT(*) FROM pss_dvnx.users WHERE enabled = true) as usuarios_activos,
    (SELECT COUNT(DISTINCT user_id) FROM pss_dvnx.user_sessions) as usuarios_que_iniciaron_sesion,
    (SELECT COUNT(DISTINCT id_usuario) FROM pss_dvnx.partesdetrabajo) as usuarios_con_partes,
    (SELECT COUNT(*) FROM pss_dvnx.partesdetrabajo) as total_partes_sistema,
    (SELECT COUNT(*) FROM pss_dvnx.partesdetrabajo WHERE fecha_trabajo >= CURRENT_DATE - INTERVAL '7 days') as partes_ultima_semana,
    (SELECT COUNT(*) FROM pss_dvnx.partesdetrabajo WHERE fecha_trabajo >= CURRENT_DATE - INTERVAL '30 days') as partes_ultimo_mes,
    ROUND(
        (SELECT COUNT(DISTINCT id_usuario)::numeric FROM pss_dvnx.partesdetrabajo) * 100.0 / 
        NULLIF((SELECT COUNT(*) FROM pss_dvnx.users WHERE enabled = true), 0),
        2
    ) as porcentaje_adopcion;
