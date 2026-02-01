-- ================================================================
-- CONSULTAS DE VERIFICACIÓN DE FRESHDESK COMPANIES
-- Base de datos: pss_dvnx
-- ================================================================

-- 1️⃣ CONTEO TOTAL DE COMPANIES
select 
    'Total companies sincronizadas' as metrica,
    count(*) as cantidad
from pss_dvnx.freshdesk_companies_cache;

-- 2️⃣ COMPANIES POR INDUSTRY
select 
    industry,
    count(*) as cantidad
from pss_dvnx.freshdesk_companies_cache
where industry is not null
group by industry
order by cantidad desc
limit 10;

-- 3️⃣ COMPANIES POR ACCOUNT TIER
select 
    account_tier,
    count(*) as cantidad
from pss_dvnx.freshdesk_companies_cache
where account_tier is not null
group by account_tier
order by cantidad desc;

-- 4️⃣ ÚLTIMAS 10 COMPANIES ACTUALIZADAS
select 
    company_id,
    name,
    industry,
    account_tier,
    health_score,
    updated_at,
    synced_at
from pss_dvnx.freshdesk_companies_cache
order by updated_at desc
limit 10;

-- 5️⃣ COMPANIES CON DOMAINS
select 
    company_id,
    name,
    array_length(domains, 1) as num_domains,
    domains
from pss_dvnx.freshdesk_companies_cache
where domains is not null and array_length(domains, 1) > 0
limit 10;

-- 6️⃣ COMPANIES CON CUSTOM FIELDS
select 
    company_id,
    name,
    custom_fields
from pss_dvnx.freshdesk_companies_cache
where custom_fields is not null and custom_fields::text != '{}'
limit 10;

-- 7️⃣ COMPANIES CON MEJOR HEALTH SCORE
select 
    company_id,
    name,
    health_score,
    account_tier,
    industry
from pss_dvnx.freshdesk_companies_cache
where health_score is not null
order by health_score desc
limit 10;

-- 8️⃣ RESUMEN DE SINCRONIZACIÓN
select 
    count(*) as total_companies,
    count(case when industry is not null then 1 end) as con_industry,
    count(case when account_tier is not null then 1 end) as con_account_tier,
    count(case when health_score is not null then 1 end) as con_health_score,
    count(case when domains is not null and array_length(domains, 1) > 0 then 1 end) as con_domains,
    max(updated_at) as max_updated_at,
    max(synced_at) as max_synced_at
from pss_dvnx.freshdesk_companies_cache;

-- 9️⃣ COMPANIES CREADAS EN LOS ÚLTIMOS 30 DÍAS
select 
    date(created_at) as fecha,
    count(*) as companies_creadas
from pss_dvnx.freshdesk_companies_cache
where created_at >= now() - interval '30 days'
group by date(created_at)
order by fecha desc;

-- 🔟 SAMPLE DE RAW JSON (primer company)
select 
    company_id,
    name,
    raw
from pss_dvnx.freshdesk_companies_cache
order by company_id
limit 1;
