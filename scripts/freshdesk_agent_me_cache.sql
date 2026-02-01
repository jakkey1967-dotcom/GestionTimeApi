
-- ================================================================
-- FRESHDESK AGENT ME CACHE TABLE
-- Schema: pss_dvnx
-- Descripción: Tabla para almacenar el agente actual de Freshdesk (/api/v2/agents/me)
-- ================================================================

-- Tabla: freshdesk_agent_me_cache
CREATE TABLE IF NOT EXISTS pss_dvnx.freshdesk_agent_me_cache (
  agent_id              bigint PRIMARY KEY,
  agent_email           text NOT NULL,
  agent_name            text NULL,
  agent_type            text NULL,
  is_active             boolean NULL,
  language              text NULL,
  time_zone             text NULL,
  mobile                text NULL,
  phone                 text NULL,
  last_login_at         timestamptz NULL,
  freshdesk_created_at  timestamptz NULL,
  freshdesk_updated_at  timestamptz NULL,
  raw                   jsonb NOT NULL,
  synced_at             timestamptz NOT NULL DEFAULT NOW()
);

-- Índices útiles
CREATE INDEX IF NOT EXISTS ix_fd_agent_me_email
  ON pss_dvnx.freshdesk_agent_me_cache (agent_email);

CREATE INDEX IF NOT EXISTS ix_fd_agent_me_synced_at
  ON pss_dvnx.freshdesk_agent_me_cache (synced_at DESC);

-- Comentarios
COMMENT ON TABLE pss_dvnx.freshdesk_agent_me_cache IS 
  'Caché del agente actual de Freshdesk (GET /api/v2/agents/me)';
COMMENT ON COLUMN pss_dvnx.freshdesk_agent_me_cache.agent_id IS 
  'ID del agente en Freshdesk (primary key)';
COMMENT ON COLUMN pss_dvnx.freshdesk_agent_me_cache.agent_email IS 
  'Email del agente';
COMMENT ON COLUMN pss_dvnx.freshdesk_agent_me_cache.raw IS 
  'JSON completo original de Freshdesk (sin signature)';
COMMENT ON COLUMN pss_dvnx.freshdesk_agent_me_cache.synced_at IS 
  'Timestamp de cuando se sincronizó este registro';

-- Verificación
SELECT 'freshdesk_agent_me_cache' as tabla, COUNT(*) as registros 
FROM pss_dvnx.freshdesk_agent_me_cache;
