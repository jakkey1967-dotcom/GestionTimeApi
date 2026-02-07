-- ========================================
-- ACTIVIDAD DE USUARIOS BETA TESTERS
-- ========================================
-- Monitoreo completo de uso de la aplicacion
-- Schema: pss_dvnx

\c pss_dvnx
SET search_path = pss_dvnx, public;

-- ========================================
-- 1?? RESUMEN GENERAL DE USUARIOS
-- ========================================
SELECT 
    '=== RESUMEN GENERAL DE USUARIOS ===' as seccion;

SELECT 
    u.id,
    u.nombre,
    u.email,
    u.activo,
    u.created_at as fecha_registro,
    u.last_login_at as ultima_sesion,
    CASE 
        WHEN u.last_login_at IS NULL THEN '? Nunca'
        WHEN u.last_login_at >= NOW() - INTERVAL '1 day' THEN '?? Hoy'
        WHEN u.last_login_at >= NOW() - INTERVAL '7 days' THEN '?? Esta semana'
        WHEN u.last_login_at >= NOW() - INTERVAL '30 days' THEN '?? Este mes'
        ELSE '?? Hace más de 1 mes'
    END as estado_actividad,
    (SELECT COUNT(*) FROM pss_dvnx.partes_de_trabajo p WHERE p.id_usuario = u.id) as total_partes
FROM pss_dvnx."user" u
ORDER BY u.last_login_at DESC NULLS LAST;

-- ========================================
-- 2?? ACTIVIDAD DE LOGIN POR USUARIO
-- ========================================
SELECT 
    '=== ACTIVIDAD DE LOGIN ===' as seccion;

SELECT 
    u.nombre,
    u.email,
    u.last_login_at as ultima_sesion,
    CASE 
        WHEN u.last_login_at IS NULL THEN 'Nunca ha iniciado sesión'
        ELSE CONCAT(
            EXTRACT(DAY FROM (NOW() - u.last_login_at))::INT, ' días, ',
            EXTRACT(HOUR FROM (NOW() - u.last_login_at))::INT, ' horas'
        )
    END as hace_cuanto,
    u.last_login_ip as ultima_ip,
    u.activo
FROM pss_dvnx."user" u
ORDER BY u.last_login_at DESC NULLS LAST;

-- ========================================
-- 3?? PARTES POR USUARIO (TOTAL)
-- ========================================
SELECT 
    '=== PARTES CREADOS POR USUARIO (TOTAL) ===' as seccion;

SELECT 
    u.nombre as usuario,
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
FROM pss_dvnx."user" u
LEFT JOIN pss_dvnx.partes_de_trabajo p ON p.id_usuario = u.id
WHERE u.activo = true
GROUP BY u.id, u.nombre, u.email
ORDER BY total_partes DESC;

-- ========================================
-- 4?? ACTIVIDAD DIARIA (ÚLTIMOS 30 DÍAS)
-- ========================================
SELECT 
    '=== ACTIVIDAD DIARIA (ÚLTIMOS 30 DÍAS) ===' as seccion;

WITH ultimos_30_dias AS (
    SELECT generate_series(
        CURRENT_DATE - INTERVAL '29 days',
        CURRENT_DATE,
        INTERVAL '1 day'
    )::date as fecha
)
SELECT 
    d.fecha,
    TO_CHAR(d.fecha, 'Day') as dia_semana,
    COUNT(DISTINCT p.id_usuario) as usuarios_activos,
    COUNT(p.id) as partes_creados,
    ROUND(
        CAST(SUM(
            EXTRACT(EPOCH FROM (
                (p.fecha_trabajo + p.hora_fin::time) - 
                (p.fecha_trabajo + p.hora_inicio::time)
            )) / 3600.0
        ) AS NUMERIC), 2
    ) as horas_registradas
FROM ultimos_30_dias d
LEFT JOIN pss_dvnx.partes_de_trabajo p ON p.fecha_trabajo = d.fecha
GROUP BY d.fecha
ORDER BY d.fecha DESC;

-- ========================================
-- 5?? PARTES POR USUARIO Y FECHA (DETALLADO)
-- ========================================
SELECT 
    '=== PARTES POR USUARIO Y FECHA (ÚLTIMOS 15 DÍAS) ===' as seccion;

SELECT 
    p.fecha_trabajo,
    u.nombre as usuario,
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
FROM pss_dvnx.partes_de_trabajo p
INNER JOIN pss_dvnx."user" u ON u.id = p.id_usuario
INNER JOIN pss_dvnx.cliente c ON c.id = p.id_cliente
WHERE p.fecha_trabajo >= CURRENT_DATE - INTERVAL '15 days'
GROUP BY p.fecha_trabajo, u.id, u.nombre
ORDER BY p.fecha_trabajo DESC, u.nombre;

-- ========================================
-- 6?? USUARIOS SIN ACTIVIDAD
-- ========================================
SELECT 
    '=== USUARIOS SIN ACTIVIDAD ===' as seccion;

SELECT 
    u.nombre,
    u.email,
    u.role,
    u.activo,
    u.created_at as registrado_el,
    EXTRACT(DAY FROM (NOW() - u.created_at))::INT as dias_desde_registro,
    u.last_login_at as ultima_sesion,
    (SELECT COUNT(*) FROM pss_dvnx.partes_de_trabajo p WHERE p.id_usuario = u.id) as partes_creados
FROM pss_dvnx."user" u
WHERE 
    u.activo = true
    AND (
        u.last_login_at IS NULL 
        OR u.last_login_at < NOW() - INTERVAL '7 days'
    )
    OR (SELECT COUNT(*) FROM pss_dvnx.partes_de_trabajo p WHERE p.id_usuario = u.id) = 0
ORDER BY u.created_at DESC;

-- ========================================
-- 7?? TOP 5 USUARIOS MÁS ACTIVOS
-- ========================================
SELECT 
    '=== TOP 5 USUARIOS MÁS ACTIVOS ===' as seccion;

SELECT 
    u.nombre,
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
    u.last_login_at as ultima_sesion
FROM pss_dvnx."user" u
INNER JOIN pss_dvnx.partes_de_trabajo p ON p.id_usuario = u.id
WHERE u.activo = true
GROUP BY u.id, u.nombre, u.email, u.last_login_at
ORDER BY total_partes DESC
LIMIT 5;

-- ========================================
-- 8?? ACTIVIDAD POR SEMANA (ÚLTIMAS 4 SEMANAS)
-- ========================================
SELECT 
    '=== ACTIVIDAD SEMANAL (ÚLTIMAS 4 SEMANAS) ===' as seccion;

SELECT 
    DATE_TRUNC('week', p.fecha_trabajo)::date as inicio_semana,
    DATE_TRUNC('week', p.fecha_trabajo)::date + INTERVAL '6 days' as fin_semana,
    COUNT(DISTINCT p.id_usuario) as usuarios_activos,
    COUNT(p.id) as total_partes,
    COUNT(DISTINCT p.fecha_trabajo) as dias_con_actividad,
    ROUND(
        CAST(SUM(
            EXTRACT(EPOCH FROM (
                (p.fecha_trabajo + p.hora_fin::time) - 
                (p.fecha_trabajo + p.hora_inicio::time)
            )) / 3600.0
        ) AS NUMERIC), 2
    ) as horas_semanales
FROM pss_dvnx.partes_de_trabajo p
WHERE p.fecha_trabajo >= CURRENT_DATE - INTERVAL '28 days'
GROUP BY DATE_TRUNC('week', p.fecha_trabajo)
ORDER BY inicio_semana DESC;

-- ========================================
-- 9?? CLIENTES MÁS TRABAJADOS (TOP 10)
-- ========================================
SELECT 
    '=== CLIENTES MÁS TRABAJADOS ===' as seccion;

SELECT 
    c.nombre as cliente,
    COUNT(p.id) as total_partes,
    COUNT(DISTINCT p.id_usuario) as usuarios_diferentes,
    ROUND(
        CAST(SUM(
            EXTRACT(EPOCH FROM (
                (p.fecha_trabajo + p.hora_fin::time) - 
                (p.fecha_trabajo + p.hora_inicio::time)
            )) / 3600.0
        ) AS NUMERIC), 2
    ) as horas_totales,
    MAX(p.fecha_trabajo) as ultimo_trabajo
FROM pss_dvnx.partes_de_trabajo p
INNER JOIN pss_dvnx.cliente c ON c.id = p.id_cliente
GROUP BY c.id, c.nombre
ORDER BY total_partes DESC
LIMIT 10;

-- ========================================
-- ?? RESUMEN EJECUTIVO
-- ========================================
SELECT 
    '=== RESUMEN EJECUTIVO ===' as seccion;

SELECT 
    (SELECT COUNT(*) FROM pss_dvnx."user" WHERE activo = true) as total_usuarios_activos,
    (SELECT COUNT(*) FROM pss_dvnx."user" WHERE activo = true AND last_login_at IS NOT NULL) as usuarios_que_iniciaron_sesion,
    (SELECT COUNT(DISTINCT id_usuario) FROM pss_dvnx.partes_de_trabajo) as usuarios_con_partes,
    (SELECT COUNT(*) FROM pss_dvnx.partes_de_trabajo) as total_partes_sistema,
    (SELECT COUNT(*) FROM pss_dvnx.partes_de_trabajo WHERE fecha_trabajo >= CURRENT_DATE - INTERVAL '7 days') as partes_ultima_semana,
    (SELECT COUNT(*) FROM pss_dvnx.partes_de_trabajo WHERE fecha_trabajo >= CURRENT_DATE - INTERVAL '30 days') as partes_ultimo_mes,
    ROUND(
        (SELECT COUNT(DISTINCT id_usuario)::numeric FROM pss_dvnx.partes_de_trabajo) * 100.0 / 
        NULLIF((SELECT COUNT(*) FROM pss_dvnx."user" WHERE activo = true), 0),
        2
    ) as porcentaje_adopcion;

-- ========================================
-- ?? ESTADÍSTICAS DE USO POR MÓDULO
-- ========================================
SELECT 
    '=== ESTADÍSTICAS POR MÓDULO ===' as seccion;

SELECT 
    'Tags' as modulo,
    COUNT(*) as registros_totales,
    (SELECT COUNT(DISTINCT id_parte) FROM pss_dvnx.parte_tags) as partes_con_tags
FROM pss_dvnx.tag
UNION ALL
SELECT 
    'Clientes' as modulo,
    COUNT(*) as registros_totales,
    (SELECT COUNT(DISTINCT id_cliente) FROM pss_dvnx.partes_de_trabajo) as clientes_con_partes
FROM pss_dvnx.cliente
UNION ALL
SELECT 
    'Grupos' as modulo,
    COUNT(*) as registros_totales,
    (SELECT COUNT(DISTINCT id_grupo) FROM pss_dvnx.partes_de_trabajo WHERE id_grupo IS NOT NULL) as grupos_usados
FROM pss_dvnx.grupo
UNION ALL
SELECT 
    'Tipos' as modulo,
    COUNT(*) as registros_totales,
    (SELECT COUNT(DISTINCT id_tipo) FROM pss_dvnx.partes_de_trabajo WHERE id_tipo IS NOT NULL) as tipos_usados
FROM pss_dvnx.tipo;

