-- ================================================================
-- FRESHDESK COMPANIES CACHE TABLE
-- Schema: pss_dvnx
-- Descripción: Tabla para almacenar empresas de Freshdesk
-- ================================================================

-- Tabla: freshdesk_companies_cache
CREATE TABLE IF NOT EXISTS pss_dvnx.freshdesk_companies_cache (
  company_id       bigint PRIMARY KEY,
  name             text NOT NULL,
  description      text NULL,
  note             text NULL,
  domains          text[] NULL,
  health_score     text NULL,
  account_tier     text NULL,
  renewal_date     timestamptz NULL,
  industry         text NULL,
  phone            text NULL,
  custom_fields    jsonb NULL,
  created_at       timestamptz NULL,
  updated_at       timestamptz NULL,
  raw              jsonb NOT NULL,
  synced_at        timestamptz NOT NULL DEFAULT NOW()
);

-- Índices útiles
CREATE INDEX IF NOT EXISTS ix_fd_companies_name
  ON pss_dvnx.freshdesk_companies_cache (name);

CREATE INDEX IF NOT EXISTS ix_fd_companies_updated_at
  ON pss_dvnx.freshdesk_companies_cache (updated_at DESC);

CREATE INDEX IF NOT EXISTS ix_fd_companies_synced_at
  ON pss_dvnx.freshdesk_companies_cache (synced_at DESC);

CREATE INDEX IF NOT EXISTS ix_fd_companies_industry
  ON pss_dvnx.freshdesk_companies_cache (industry);

-- Comentarios
COMMENT ON TABLE pss_dvnx.freshdesk_companies_cache IS 
  'Caché de empresas sincronizadas desde Freshdesk';
COMMENT ON COLUMN pss_dvnx.freshdesk_companies_cache.company_id IS 
  'ID de la empresa en Freshdesk (primary key)';
COMMENT ON COLUMN pss_dvnx.freshdesk_companies_cache.raw IS 
  'JSON completo original de Freshdesk';
COMMENT ON COLUMN pss_dvnx.freshdesk_companies_cache.synced_at IS 
  'Timestamp de cuando se sincronizó este registro';

-- Verificación
SELECT 'freshdesk_companies_cache' as tabla, COUNT(*) as registros 
FROM pss_dvnx.freshdesk_companies_cache;