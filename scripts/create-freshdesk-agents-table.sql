-- ================================================================
-- FRESHDESK AGENTS CACHE TABLE
-- Schema: pss_dvnx
-- Descripción: Tabla para almacenar todos los agentes de Freshdesk (/api/v2/agents)
-- ================================================================

-- Tabla: freshdesk_agents_cache
CREATE TABLE IF NOT EXISTS pss_dvnx.freshdesk_agents_cache (
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
CREATE INDEX IF NOT EXISTS ix_fd_agents_email
  ON pss_dvnx.freshdesk_agents_cache (agent_email);

CREATE INDEX IF NOT EXISTS ix_fd_agents_active
  ON pss_dvnx.freshdesk_agents_cache (is_active) WHERE is_active = true;

CREATE INDEX IF NOT EXISTS ix_fd_agents_updated_at
  ON pss_dvnx.freshdesk_agents_cache (freshdesk_updated_at DESC);

CREATE INDEX IF NOT EXISTS ix_fd_agents_synced_at
  ON pss_dvnx.freshdesk_agents_cache (synced_at DESC);

-- Comentarios
COMMENT ON TABLE pss_dvnx.freshdesk_agents_cache IS 
  'Caché de todos los agentes sincronizados desde Freshdesk (GET /api/v2/agents)';
COMMENT ON COLUMN pss_dvnx.freshdesk_agents_cache.agent_id IS 
  'ID del agente en Freshdesk (primary key)';
COMMENT ON COLUMN pss_dvnx.freshdesk_agents_cache.agent_email IS 
  'Email del agente';
COMMENT ON COLUMN pss_dvnx.freshdesk_agents_cache.is_active IS 
  'Indica si el agente está activo';
COMMENT ON COLUMN pss_dvnx.freshdesk_agents_cache.raw IS 
  'JSON completo original de Freshdesk';
COMMENT ON COLUMN pss_dvnx.freshdesk_agents_cache.synced_at IS 
  'Timestamp de cuando se sincronizó este registro';

-- Verificación
SELECT 'freshdesk_agents_cache' as tabla, COUNT(*) as registros 
FROM pss_dvnx.freshdesk_agents_cache;
