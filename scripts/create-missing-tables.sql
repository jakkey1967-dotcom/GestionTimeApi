-- =====================================================
-- SCRIPT COMPLETO PARA CREAR TABLAS FALTANTES
-- Base de datos: Render PostgreSQL
-- Schema: pss_dvnx
-- =====================================================

-- 1. TABLA: freshdesk_tags
-- =====================================================
CREATE TABLE IF NOT EXISTS pss_dvnx.freshdesk_tags (
    name VARCHAR(100) NOT NULL,
    source VARCHAR(50) NOT NULL DEFAULT 'freshdesk',
    last_seen_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_freshdesk_tags PRIMARY KEY (name)
);

CREATE INDEX IF NOT EXISTS idx_freshdesk_tags_last_seen 
    ON pss_dvnx.freshdesk_tags(last_seen_at DESC);

CREATE INDEX IF NOT EXISTS idx_freshdesk_tags_source 
    ON pss_dvnx.freshdesk_tags(source);

COMMENT ON TABLE pss_dvnx.freshdesk_tags IS 
    'Catálogo unificado de tags (Freshdesk + Partes locales)';

-- =====================================================
-- 2. TABLA: freshdesk_agent_maps
-- =====================================================
CREATE TABLE IF NOT EXISTS pss_dvnx.freshdesk_agent_maps (
    freshdesk_agent_id BIGINT NOT NULL,
    user_id UUID NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_freshdesk_agent_maps PRIMARY KEY (freshdesk_agent_id, user_id)
);

CREATE INDEX IF NOT EXISTS idx_freshdesk_agent_maps_user_id 
    ON pss_dvnx.freshdesk_agent_maps(user_id);

COMMENT ON TABLE pss_dvnx.freshdesk_agent_maps IS 
    'Mapeo entre agentes de Freshdesk y usuarios de GestionTime';

-- =====================================================
-- 3. TABLA: parte_tags (Relación N:N)
-- =====================================================
CREATE TABLE IF NOT EXISTS pss_dvnx.parte_tags (
    parte_id BIGINT NOT NULL,
    tag_name VARCHAR(100) NOT NULL,
    CONSTRAINT pk_parte_tags PRIMARY KEY (parte_id, tag_name)
);

-- Foreign Key a partesdetrabajo
ALTER TABLE pss_dvnx.parte_tags 
    DROP CONSTRAINT IF EXISTS fk_parte_tags_parte;

ALTER TABLE pss_dvnx.parte_tags 
    ADD CONSTRAINT fk_parte_tags_parte 
    FOREIGN KEY (parte_id) 
    REFERENCES pss_dvnx.partesdetrabajo(id) 
    ON DELETE CASCADE;

-- Foreign Key a freshdesk_tags
ALTER TABLE pss_dvnx.parte_tags 
    DROP CONSTRAINT IF EXISTS fk_parte_tags_tag;

ALTER TABLE pss_dvnx.parte_tags 
    ADD CONSTRAINT fk_parte_tags_tag 
    FOREIGN KEY (tag_name) 
    REFERENCES pss_dvnx.freshdesk_tags(name) 
    ON DELETE RESTRICT;

-- Índices
CREATE INDEX IF NOT EXISTS idx_parte_tags_parte_id 
    ON pss_dvnx.parte_tags(parte_id);

CREATE INDEX IF NOT EXISTS idx_parte_tags_tag_name 
    ON pss_dvnx.parte_tags(tag_name);

COMMENT ON TABLE pss_dvnx.parte_tags IS 
    'Relación N:N entre partes de trabajo y tags';

-- =====================================================
-- 4. INSERTAR EN __EFMigrationsHistory
-- =====================================================
-- Registrar migraciones como aplicadas

INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES 
    ('20260124151520_AddFreshdeskTables', '8.0.0'),
    ('20260125110057_AddPartesTagsWithFreshdeskTags', '8.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;

-- =====================================================
-- VERIFICACIÓN
-- =====================================================

-- Verificar que las tablas existen
SELECT 
    table_name,
    CASE 
        WHEN table_name IN ('freshdesk_tags', 'freshdesk_agent_maps', 'parte_tags') 
        THEN '✅ OK'
        ELSE '❌ FALTA'
    END as estado
FROM information_schema.tables
WHERE table_schema = 'pss_dvnx'
  AND table_name IN ('freshdesk_tags', 'freshdesk_agent_maps', 'parte_tags')
ORDER BY table_name;

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

-- Contar registros
SELECT 
    'freshdesk_tags' as tabla,
    COUNT(*) as registros
FROM pss_dvnx.freshdesk_tags
UNION ALL
SELECT 
    'freshdesk_agent_maps' as tabla,
    COUNT(*) as registros
FROM pss_dvnx.freshdesk_agent_maps
UNION ALL
SELECT 
    'parte_tags' as tabla,
    COUNT(*) as registros
FROM pss_dvnx.parte_tags;
