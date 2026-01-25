-- Migración para tablas de Freshdesk
-- Ejecutar en el schema pss_dvnx

SET search_path TO pss_dvnx;

-- Tabla para cachear el mapeo de usuarios a agentes de Freshdesk
CREATE TABLE IF NOT EXISTS freshdesk_agent_map (
    user_id UUID PRIMARY KEY,
    email VARCHAR(200) NOT NULL,
    agent_id BIGINT NOT NULL,
    synced_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_freshdesk_agent_email ON freshdesk_agent_map(email);

-- Tabla para tags sincronizados desde Freshdesk
CREATE TABLE IF NOT EXISTS freshdesk_tags (
    name VARCHAR(100) PRIMARY KEY,
    source VARCHAR(50) NOT NULL DEFAULT 'freshdesk',
    last_seen_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_freshdesk_tags_last_seen ON freshdesk_tags(last_seen_at);
