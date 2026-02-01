create table if not exists pss_dvnx.freshdesk_ticket_header (
  ticket_id            bigint primary key,
  subject              text not null,

  status               int null,
  priority             int null,
  type                 text null,

  requester_id         bigint null,
  responder_id         bigint null,
  group_id             bigint null,
  company_id           bigint null,

  created_at           timestamptz not null,
  updated_at           timestamptz not null,

  -- Opcional: nombre del cliente/empresa si luego lo resuelves
  company_name         text null,

  -- Opcional: datos compactos
  tags                 jsonb null,
  custom_fields        jsonb null
);

-- 3) Índices útiles
create index if not exists ix_fd_hdr_updated_at
  on pss_dvnx.freshdesk_ticket_header (updated_at desc);

create index if not exists ix_fd_hdr_responder_updated
  on pss_dvnx.freshdesk_ticket_header (responder_id, updated_at desc);

create index if not exists ix_fd_hdr_company_updated
  on pss_dvnx.freshdesk_ticket_header (company_id, updated_at desc);

-- 4) Estado del sync (cursor incremental)
create table if not exists pss_dvnx.freshdesk_sync_state (
  scope               text primary key,
  last_sync_at         timestamptz null,
  last_updated_since   timestamptz null,
  last_max_updated_at  timestamptz null,
  last_result_count    int null,
  last_error           text null
);

-- 5) Seed opcional del scope para que exista desde el inicio
insert into pss_dvnx.freshdesk_sync_state(scope)
values ('ticket_headers')
on conflict (scope) do nothing;