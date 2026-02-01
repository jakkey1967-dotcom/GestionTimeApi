-- ================================================================
-- FRESHDESK COMPANIES CACHE TABLE
-- Schema: pss_dvnx
-- Descripción: Tabla para almacenar empresas de Freshdesk
-- ================================================================

-- Tabla: freshdesk_companies_cache
create table if not exists pss_dvnx.freshdesk_companies_cache (
  company_id       bigint primary key,
  name             text not null,
  description      text null,
  note             text null,
  domains          text[] null,
  health_score     text null,
  account_tier     text null,
  renewal_date     timestamptz null,
  industry         text null,
  phone            text null,
  custom_fields    jsonb null,
  created_at       timestamptz null,
  updated_at       timestamptz null,
  raw              jsonb not null,
  synced_at        timestamptz not null default now()
);

-- Índices útiles
create index if not exists ix_fd_companies_name
  on pss_dvnx.freshdesk_companies_cache (name);

create index if not exists ix_fd_companies_updated_at
  on pss_dvnx.freshdesk_companies_cache (updated_at desc);

create index if not exists ix_fd_companies_synced_at
  on pss_dvnx.freshdesk_companies_cache (synced_at desc);

-- Comentarios
comment on table pss_dvnx.freshdesk_companies_cache is 
  'Caché de empresas sincronizadas desde Freshdesk';
comment on column pss_dvnx.freshdesk_companies_cache.company_id is 
  'ID de la empresa en Freshdesk (primary key)';
comment on column pss_dvnx.freshdesk_companies_cache.raw is 
  'JSON completo original de Freshdesk';
comment on column pss_dvnx.freshdesk_companies_cache.synced_at is 
  'Timestamp de cuando se sincronizó este registro';

-- Verificación
select 'freshdesk_companies_cache' as tabla, count(*) as registros 
from pss_dvnx.freshdesk_companies_cache;
