-- View: pss_dvnx.v_partes_stats_full
-- Crea/actualiza la vista. NO cambia OWNER.

CREATE OR REPLACE VIEW pss_dvnx.v_partes_stats_full AS
SELECT
  p.id,
  p.fecha_trabajo,
  p.hora_inicio,
  p.hora_fin,
  round(
    EXTRACT(epoch FROM
      CASE
        WHEN p.hora_inicio IS NULL OR p.hora_fin IS NULL THEN NULL::interval
        WHEN p.hora_fin >= p.hora_inicio THEN (p.hora_fin - p.hora_inicio)
        ELSE (p.hora_fin - p.hora_inicio) + interval '24 hours'
      END
    ) / 3600.0
  , 2) AS duracion_horas,
  (EXTRACT(epoch FROM
      CASE
        WHEN p.hora_inicio IS NULL OR p.hora_fin IS NULL THEN NULL::interval
        WHEN p.hora_fin >= p.hora_inicio THEN (p.hora_fin - p.hora_inicio)
        ELSE (p.hora_fin - p.hora_inicio) + interval '24 hours'
      END
    ) / 60)::int AS duracion_min,
  p.accion,
  p.ticket,
  p.id_cliente,
  p.tienda,
  p.id_grupo,
  p.id_tipo,
  p.id_usuario,
  p.estado,
  tp.tag_principal AS tags,
  p.created_at,
  p.updated_at,
  p.fecha_trabajo AS fecha_dia,
  to_char(p.fecha_trabajo, 'IW')::int AS semana_iso,
  EXTRACT(month FROM p.fecha_trabajo)::int AS mes,
  EXTRACT(year  FROM p.fecha_trabajo)::int AS anio,
  u.full_name AS agente_nombre,
  u.email     AS agente_email,
  c.nombre    AS cliente_nombre,
  g.nombre    AS grupo_nombre,
  t.nombre    AS tipo_nombre,
  round(EXTRACT(epoch FROM (p.updated_at - p.created_at)) / 3600.0, 2) AS duracion_horas_ts,
  (EXTRACT(epoch FROM (p.updated_at - p.created_at)) / 60)::int        AS duracion_min_ts
FROM pss_dvnx.partesdetrabajo p
LEFT JOIN (
  SELECT
    parte_id,
    MIN(tag_name::text) AS tag_principal
  FROM pss_dvnx.parte_tags
  GROUP BY parte_id
) tp ON tp.parte_id = p.id
LEFT JOIN pss_dvnx.users   u ON u.id = p.id_usuario
LEFT JOIN pss_dvnx.cliente c ON c.id = p.id_cliente
LEFT JOIN pss_dvnx.grupo   g ON g.id_grupo = p.id_grupo
LEFT JOIN pss_dvnx.tipo    t ON t.id_tipo  = p.id_tipo;
