-- =====================================================
-- SCRIPT COMPLETO - ORDEN CORRECTO
-- Crear TODAS las tablas en el orden correcto
-- Schema: pss_dvnx
-- =====================================================

-- =====================================================
-- PASO 1: Crear freshdesk_tags PRIMERO (es dependencia)
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

COMMIT;

-- Verificar
SELECT 'freshdesk_tags creada' as paso_1, COUNT(*) FROM pss_dvnx.freshdesk_tags;

-- =====================================================
-- PASO 2: Crear freshdesk_agent_maps
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

COMMIT;

-- Verificar
SELECT 'freshdesk_agent_maps creada' as paso_2, COUNT(*) FROM pss_dvnx.freshdesk_agent_maps;

-- =====================================================
-- PASO 3: Ahora SÍ crear parte_tags (depende de freshdesk_tags)
-- =====================================================

-- Primero eliminar si existe (para recrear limpia)
DROP TABLE IF EXISTS pss_dvnx.parte_tags CASCADE;

-- Crear tabla
CREATE TABLE pss_dvnx.parte_tags (
    parte_id BIGINT NOT NULL,
    tag_name VARCHAR(100) NOT NULL,
    CONSTRAINT pk_parte_tags PRIMARY KEY (parte_id, tag_name)
);

-- Foreign Key a partesdetrabajo
ALTER TABLE pss_dvnx.parte_tags 
    ADD CONSTRAINT fk_parte_tags_partesdetrabajo_parte_id
    FOREIGN KEY (parte_id) 
    REFERENCES pss_dvnx.partesdetrabajo(id) 
    ON DELETE CASCADE;

-- Foreign Key a freshdesk_tags (DEBE EXISTIR YA)
ALTER TABLE pss_dvnx.parte_tags 
    ADD CONSTRAINT fk_parte_tags_freshdesk_tags_tag_name
    FOREIGN KEY (tag_name) 
    REFERENCES pss_dvnx.freshdesk_tags(name) 
    ON DELETE RESTRICT;

-- Índices
CREATE INDEX idx_parte_tags_parte_id 
    ON pss_dvnx.parte_tags(parte_id);

CREATE INDEX idx_parte_tags_tag_name 
    ON pss_dvnx.parte_tags(tag_name);

COMMENT ON TABLE pss_dvnx.parte_tags IS 
    'Relación N:N entre partes de trabajo y tags';

COMMIT;

-- Verificar
SELECT 'parte_tags creada' as paso_3, COUNT(*) FROM pss_dvnx.parte_tags;

-- =====================================================
-- PASO 4: Registrar migraciones como aplicadas
-- =====================================================
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES 
    ('20260124151520_AddFreshdeskTables', '8.0.0'),
    ('20260125110057_AddPartesTagsWithFreshdeskTags', '8.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;

COMMIT;

-- =====================================================
-- VERIFICACIÓN FINAL
-- =====================================================
SELECT 
    '✅ TODAS LAS TABLAS CREADAS' as resultado;

SELECT 
    table_name as tabla,
    '✅' as estado
FROM information_schema.tables
WHERE table_schema = 'pss_dvnx'
  AND table_name IN ('freshdesk_tags', 'freshdesk_agent_maps', 'parte_tags')
ORDER BY 
    CASE table_name
        WHEN 'freshdesk_tags' THEN 1
        WHEN 'freshdesk_agent_maps' THEN 2
        WHEN 'parte_tags' THEN 3
    END;

-- Ver constraints de parte_tags
SELECT 
    constraint_name,
    constraint_type
FROM information_schema.table_constraints
WHERE table_schema = 'pss_dvnx'
  AND table_name = 'parte_tags';

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
