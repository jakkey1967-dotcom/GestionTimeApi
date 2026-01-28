-- =====================================================
-- CREAR parte_tags INMEDIATAMENTE
-- Ejecutar en Render Console
-- =====================================================

-- PASO 1: Asegurarse que freshdesk_tags existe
CREATE TABLE IF NOT EXISTS pss_dvnx.freshdesk_tags (
    name VARCHAR(100) NOT NULL,
    source VARCHAR(50) NOT NULL DEFAULT 'freshdesk',
    last_seen_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_freshdesk_tags PRIMARY KEY (name)
);

-- PASO 2: Crear parte_tags
DROP TABLE IF EXISTS pss_dvnx.parte_tags CASCADE;

CREATE TABLE pss_dvnx.parte_tags (
    parte_id BIGINT NOT NULL,
    tag_name VARCHAR(100) NOT NULL,
    CONSTRAINT "PK_parte_tags" PRIMARY KEY (parte_id, tag_name),
    CONSTRAINT "FK_parte_tags_partesdetrabajo_parte_id" 
        FOREIGN KEY (parte_id) 
        REFERENCES pss_dvnx.partesdetrabajo(id) 
        ON DELETE CASCADE,
    CONSTRAINT "FK_parte_tags_freshdesk_tags_tag_name" 
        FOREIGN KEY (tag_name) 
        REFERENCES pss_dvnx.freshdesk_tags(name) 
        ON DELETE RESTRICT
);

CREATE INDEX idx_parte_tags_parte_id ON pss_dvnx.parte_tags(parte_id);
CREATE INDEX idx_parte_tags_tag_name ON pss_dvnx.parte_tags(tag_name);

-- PASO 3: Registrar migración
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260125110057_AddPartesTagsWithFreshdeskTags', '8.0.0')
ON CONFLICT DO NOTHING;

-- VERIFICAR
SELECT 'TABLA CREADA EXITOSAMENTE' as resultado;
SELECT COUNT(*) as registros_en_parte_tags FROM pss_dvnx.parte_tags;
