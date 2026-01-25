-- Migración manual para tablas de Freshdesk
-- Schema: pss_dvnx

-- Tabla: freshdesk_agent_maps (caché de agentes de Freshdesk)
CREATE TABLE IF NOT EXISTS pss_dvnx.freshdesk_agent_maps (
    user_id UUID NOT NULL,
    email VARCHAR(255) NOT NULL,
    agent_id BIGINT NOT NULL,
    synced_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_freshdesk_agent_maps PRIMARY KEY (user_id),
    CONSTRAINT fk_freshdesk_agent_maps_users FOREIGN KEY (user_id) 
        REFERENCES pss_dvnx.users(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_freshdesk_agent_maps_email 
    ON pss_dvnx.freshdesk_agent_maps(email);

-- Tabla: freshdesk_tags (caché de tags de Freshdesk)
CREATE TABLE IF NOT EXISTS pss_dvnx.freshdesk_tags (
    name VARCHAR(100) NOT NULL,
    source VARCHAR(50) NOT NULL DEFAULT 'freshdesk',
    last_seen_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_freshdesk_tags PRIMARY KEY (name)
);

CREATE INDEX IF NOT EXISTS ix_freshdesk_tags_last_seen 
    ON pss_dvnx.freshdesk_tags(last_seen_at DESC);

-- Comentarios para documentación
COMMENT ON TABLE pss_dvnx.freshdesk_agent_maps IS 'Caché de mapeo entre usuarios de GestionTime y agentes de Freshdesk';
COMMENT ON TABLE pss_dvnx.freshdesk_tags IS 'Caché de tags de Freshdesk para autocompletado';

-- Verificar creación
SELECT 'freshdesk_agent_maps' AS tabla, COUNT(*) AS registros FROM pss_dvnx.freshdesk_agent_maps
UNION ALL
SELECT 'freshdesk_tags' AS tabla, COUNT(*) AS registros FROM pss_dvnx.freshdesk_tags;
