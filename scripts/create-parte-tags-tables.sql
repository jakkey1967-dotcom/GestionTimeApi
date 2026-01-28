-- =====================================================
-- SCRIPT DDL: Soporte de TAGS para Partes de Trabajo
-- Schema: pss_dvnx
-- Autor: Sistema GestionTime
-- Fecha: 2026-01-25
-- =====================================================

-- Este script crea las tablas necesarias para el soporte de tags
-- en partes de trabajo. Estas tablas ya existen en algunos entornos
-- pero este script es idempotente (puede ejecutarse múltiples veces)

-- =====================================================
-- 1. Tabla de catálogo de tags (freshdesk_tags)
-- =====================================================
-- Esta tabla almacena todos los tags (de Freshdesk y locales)

CREATE TABLE IF NOT EXISTS pss_dvnx.freshdesk_tags (
    name VARCHAR(100) PRIMARY KEY,           -- Nombre del tag (PK)
    source VARCHAR(20) NOT NULL DEFAULT 'local',  -- Origen: 'freshdesk', 'local', 'both'
    last_seen_at TIMESTAMP NOT NULL DEFAULT NOW(),  -- Última vez que se usó
    created_at TIMESTAMP NOT NULL DEFAULT NOW()     -- Fecha de creación
);

-- Índice para búsquedas por fecha
CREATE INDEX IF NOT EXISTS idx_freshdesk_tags_last_seen 
    ON pss_dvnx.freshdesk_tags (last_seen_at DESC);

-- Índice para filtrar por origen
CREATE INDEX IF NOT EXISTS idx_freshdesk_tags_source 
    ON pss_dvnx.freshdesk_tags (source);

-- =====================================================
-- 2. Tabla de relación N:N (parte_tags)
-- =====================================================
-- Esta tabla relaciona partes con tags (relación muchos a muchos)

CREATE TABLE IF NOT EXISTS pss_dvnx.parte_tags (
    parte_id BIGINT NOT NULL,               -- FK a partesdetrabajo.id
    tag_name VARCHAR(100) NOT NULL,         -- FK a freshdesk_tags.name
    
    -- Clave primaria compuesta
    PRIMARY KEY (parte_id, tag_name),
    
    -- FK: Parte (ON DELETE CASCADE - si se borra el parte, se borran sus tags)
    CONSTRAINT fk_parte_tags_parte 
        FOREIGN KEY (parte_id) 
        REFERENCES pss_dvnx.partesdetrabajo(id) 
        ON DELETE CASCADE,
    
    -- FK: Tag (ON DELETE RESTRICT - no permitir borrar tag si está en uso)
    CONSTRAINT fk_parte_tags_tag 
        FOREIGN KEY (tag_name) 
        REFERENCES pss_dvnx.freshdesk_tags(name) 
        ON DELETE RESTRICT
);

-- Índices para mejorar el performance de queries
CREATE INDEX IF NOT EXISTS idx_parte_tags_parte_id 
    ON pss_dvnx.parte_tags (parte_id);

CREATE INDEX IF NOT EXISTS idx_parte_tags_tag_name 
    ON pss_dvnx.parte_tags (tag_name);

-- =====================================================
-- 3. Verificación de tablas creadas
-- =====================================================

DO $$
DECLARE
    v_count_freshdesk_tags INT;
    v_count_parte_tags INT;
    v_count_fk INT;
BEGIN
    -- Contar tablas
    SELECT COUNT(*) INTO v_count_freshdesk_tags
    FROM information_schema.tables 
    WHERE table_schema = 'pss_dvnx' 
      AND table_name = 'freshdesk_tags';
    
    SELECT COUNT(*) INTO v_count_parte_tags
    FROM information_schema.tables 
    WHERE table_schema = 'pss_dvnx' 
      AND table_name = 'parte_tags';
    
    -- Contar foreign keys
    SELECT COUNT(*) INTO v_count_fk
    FROM information_schema.table_constraints 
    WHERE table_schema = 'pss_dvnx' 
      AND table_name = 'parte_tags'
      AND constraint_type = 'FOREIGN KEY';
    
    -- Mostrar resultados
    RAISE NOTICE '================================================';
    RAISE NOTICE 'VERIFICACIÓN DE TABLAS DE TAGS';
    RAISE NOTICE '================================================';
    RAISE NOTICE 'Tabla freshdesk_tags: %', 
        CASE WHEN v_count_freshdesk_tags > 0 THEN '✓ EXISTE' ELSE '✗ NO EXISTE' END;
    RAISE NOTICE 'Tabla parte_tags: %', 
        CASE WHEN v_count_parte_tags > 0 THEN '✓ EXISTE' ELSE '✗ NO EXISTE' END;
    RAISE NOTICE 'Foreign Keys: % de 2', v_count_fk;
    RAISE NOTICE '================================================';
END $$;

-- =====================================================
-- 4. Consultas de verificación
-- =====================================================

-- Ver estructura de freshdesk_tags
SELECT 
    column_name,
    data_type,
    character_maximum_length,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_schema = 'pss_dvnx' 
  AND table_name = 'freshdesk_tags'
ORDER BY ordinal_position;

-- Ver estructura de parte_tags
SELECT 
    column_name,
    data_type,
    character_maximum_length,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_schema = 'pss_dvnx' 
  AND table_name = 'parte_tags'
ORDER BY ordinal_position;

-- Ver foreign keys de parte_tags
SELECT
    tc.constraint_name,
    tc.table_name,
    kcu.column_name,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name,
    rc.delete_rule
FROM information_schema.table_constraints AS tc
    JOIN information_schema.key_column_usage AS kcu
      ON tc.constraint_name = kcu.constraint_name
      AND tc.table_schema = kcu.table_schema
    JOIN information_schema.constraint_column_usage AS ccu
      ON ccu.constraint_name = tc.constraint_name
      AND ccu.table_schema = tc.table_schema
    JOIN information_schema.referential_constraints AS rc
      ON rc.constraint_name = tc.constraint_name
WHERE tc.constraint_type = 'FOREIGN KEY'
  AND tc.table_schema = 'pss_dvnx'
  AND tc.table_name = 'parte_tags';

-- Ver índices de parte_tags
SELECT
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'pss_dvnx'
  AND tablename IN ('freshdesk_tags', 'parte_tags')
ORDER BY tablename, indexname;

-- =====================================================
-- 5. Estadísticas actuales (opcional)
-- =====================================================

-- Tags disponibles
SELECT 
    COUNT(*) AS total_tags,
    COUNT(*) FILTER (WHERE source = 'freshdesk') AS tags_freshdesk,
    COUNT(*) FILTER (WHERE source = 'local') AS tags_local,
    COUNT(*) FILTER (WHERE source = 'both') AS tags_both
FROM pss_dvnx.freshdesk_tags;

-- Relaciones parte-tag
SELECT 
    COUNT(*) AS total_relaciones,
    COUNT(DISTINCT parte_id) AS partes_con_tags,
    COUNT(DISTINCT tag_name) AS tags_en_uso
FROM pss_dvnx.parte_tags;

-- Top 10 tags más usados
SELECT 
    pt.tag_name,
    COUNT(*) AS cantidad_partes
FROM pss_dvnx.parte_tags pt
GROUP BY pt.tag_name
ORDER BY COUNT(*) DESC
LIMIT 10;

-- =====================================================
-- NOTAS:
-- =====================================================
-- 1. Este script es IDEMPOTENTE (puede ejecutarse múltiples veces)
-- 2. Las tablas ya pueden existir en algunos entornos (desarrollo/render)
-- 3. Los IF NOT EXISTS evitan errores si las tablas ya existen
-- 4. Las FK con ON DELETE CASCADE aseguran que al borrar un parte
--    se borren automáticamente sus tags
-- 5. Las FK con ON DELETE RESTRICT protegen los tags en el catálogo
-- 6. Los índices mejoran el performance de las consultas comunes
-- =====================================================

-- =====================================================
-- FIN DEL SCRIPT
-- =====================================================
