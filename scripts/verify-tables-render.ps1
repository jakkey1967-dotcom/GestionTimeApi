# Script para verificar tablas en la BD de Render

Write-Host "🔍 VERIFICANDO TABLAS EN RENDER" -ForegroundColor Cyan

# CONFIGURAR: External Database URL de Render
$DATABASE_URL = "TU_EXTERNAL_DATABASE_URL_AQUI"

if ($DATABASE_URL -eq "TU_EXTERNAL_DATABASE_URL_AQUI") {
    Write-Host "❌ ERROR: Configura DATABASE_URL primero" -ForegroundColor Red
    exit 1
}

Write-Host "`n📋 Verificando tablas en schema pss_dvnx..." -ForegroundColor Yellow

$query = @"
SELECT 
    table_name,
    CASE 
        WHEN table_name IN ('freshdesk_tags', 'freshdesk_agent_maps', 'parte_tags') 
        THEN '✅'
        ELSE '❌'
    END as estado
FROM information_schema.tables
WHERE table_schema = 'pss_dvnx'
  AND table_name IN ('freshdesk_tags', 'freshdesk_agent_maps', 'parte_tags', 'partesdetrabajo')
ORDER BY table_name;
"@

psql $DATABASE_URL -c $query

Write-Host "`n📊 Conteo de registros:" -ForegroundColor Yellow

$countQuery = @"
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
"@

psql $DATABASE_URL -c $countQuery
