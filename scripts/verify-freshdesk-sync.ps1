# Script para verificar datos sincronizados de Freshdesk en PostgreSQL
param(
    [string]$Server = "localhost",
    [int]$Port = 5434,
    [string]$Database = "pss_dvnx",
    [string]$Username = "postgres",
    [string]$Password = "postgres"
)

Write-Host "╔══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "║ VERIFICACIÓN DE DATOS SINCRONIZADOS DE FRESHDESK" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════════" -ForegroundColor Cyan

Write-Host "`n📋 Configuración:" -ForegroundColor Yellow
Write-Host "   Server: $Server" -ForegroundColor White
Write-Host "   Port: $Port" -ForegroundColor White
Write-Host "   Database: $Database" -ForegroundColor White
Write-Host "   Username: $Username" -ForegroundColor White

# Función para ejecutar query
function Invoke-PostgresQuery {
    param(
        [string]$Query,
        [string]$Title
    )
    
    Write-Host "`n$Title" -ForegroundColor Cyan
    Write-Host ("─" * 60) -ForegroundColor Gray
    
    try {
        $env:PGPASSWORD = $Password
        $result = & psql -h $Server -p $Port -U $Username -d $Database -t -A -F "|" -c $Query 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            $result | ForEach-Object {
                Write-Host "   $_" -ForegroundColor White
            }
        } else {
            Write-Host "   ❌ Error: $result" -ForegroundColor Red
        }
    } catch {
        Write-Host "   ❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# 1. Conteo total
Invoke-PostgresQuery -Title "1️⃣  TOTAL DE TICKETS" -Query @"
SELECT COUNT(*) FROM pss_dvnx.freshdesk_ticket_header;
"@

# 2. Tickets por estado
Invoke-PostgresQuery -Title "2️⃣  TICKETS POR ESTADO" -Query @"
SELECT 
    CASE status
        WHEN 2 THEN 'Open'
        WHEN 3 THEN 'Pending'
        WHEN 4 THEN 'Resolved'
        WHEN 5 THEN 'Closed'
        WHEN 6 THEN 'Waiting Customer'
        WHEN 7 THEN 'Waiting 3rd Party'
        ELSE 'Unknown'
    END || ': ' || COUNT(*) as estado
FROM pss_dvnx.freshdesk_ticket_header
GROUP BY status
ORDER BY status;
"@

# 3. Tickets por prioridad
Invoke-PostgresQuery -Title "3️⃣  TICKETS POR PRIORIDAD" -Query @"
SELECT 
    CASE priority
        WHEN 1 THEN 'Low'
        WHEN 2 THEN 'Medium'
        WHEN 3 THEN 'High'
        WHEN 4 THEN 'Urgent'
        ELSE 'Unknown'
    END || ': ' || COUNT(*) as prioridad
FROM pss_dvnx.freshdesk_ticket_header
GROUP BY priority
ORDER BY priority;
"@

# 4. Estado de sincronización
Invoke-PostgresQuery -Title "4️⃣  ESTADO DE ÚLTIMA SINCRONIZACIÓN" -Query @"
SELECT 
    'Scope: ' || scope || chr(10) ||
    'Última sync: ' || last_sync_at::text || chr(10) ||
    'Tickets procesados: ' || last_result_count || chr(10) ||
    'Max updated_at: ' || COALESCE(last_max_updated_at::text, 'NULL') || chr(10) ||
    'Updated_since: ' || COALESCE(last_updated_since::text, 'NULL') || chr(10) ||
    'Error: ' || COALESCE(last_error, 'Ninguno') || chr(10) ||
    'Horas desde sync: ' || ROUND(EXTRACT(EPOCH FROM (NOW() - last_sync_at))/3600, 2)::text
FROM pss_dvnx.freshdesk_sync_state
WHERE scope = 'ticket_headers';
"@

# 5. Tickets más recientes (últimos 5)
Invoke-PostgresQuery -Title "5️⃣  ÚLTIMOS 5 TICKETS ACTUALIZADOS" -Query @"
SELECT 
    'ID: ' || ticket_id || ' | ' || 
    LEFT(subject, 40) || ' | ' || 
    CASE status
        WHEN 2 THEN 'Open'
        WHEN 3 THEN 'Pending'
        WHEN 4 THEN 'Resolved'
        WHEN 5 THEN 'Closed'
        ELSE 'Other'
    END || ' | ' ||
    TO_CHAR(updated_at, 'YYYY-MM-DD HH24:MI')
FROM pss_dvnx.freshdesk_ticket_header
ORDER BY updated_at DESC
LIMIT 5;
"@

# 6. Tickets sin asignar
Invoke-PostgresQuery -Title "6️⃣  TICKETS SIN ASIGNAR" -Query @"
SELECT 
    'Total sin asignar: ' || COUNT(*) || chr(10) ||
    'Abiertos sin asignar: ' || SUM(CASE WHEN status IN (2,3,6,7) THEN 1 ELSE 0 END) || chr(10) ||
    'Cerrados sin asignar: ' || SUM(CASE WHEN status IN (4,5) THEN 1 ELSE 0 END)
FROM pss_dvnx.freshdesk_ticket_header
WHERE responder_id IS NULL;
"@

# 7. Tickets actualizados hoy
Invoke-PostgresQuery -Title "7️⃣  TICKETS ACTUALIZADOS HOY" -Query @"
SELECT COUNT(*) || ' tickets actualizados hoy'
FROM pss_dvnx.freshdesk_ticket_header
WHERE DATE(updated_at) = CURRENT_DATE;
"@

# 8. Top 5 técnicos con más tickets
Invoke-PostgresQuery -Title "8️⃣  TOP 5 TÉCNICOS CON MÁS TICKETS" -Query @"
SELECT 
    'Técnico ' || responder_id || ': ' || COUNT(*) || ' tickets (' ||
    SUM(CASE WHEN status IN (2,3,6,7) THEN 1 ELSE 0 END) || ' abiertos)'
FROM pss_dvnx.freshdesk_ticket_header
WHERE responder_id IS NOT NULL
GROUP BY responder_id
ORDER BY COUNT(*) DESC
LIMIT 5;
"@

# 9. Tickets con/sin tags
Invoke-PostgresQuery -Title "9️⃣  TICKETS CON TAGS" -Query @"
SELECT 
    'Con tags: ' || SUM(CASE WHEN tags IS NOT NULL AND tags::text != 'null' THEN 1 ELSE 0 END) || chr(10) ||
    'Sin tags: ' || SUM(CASE WHEN tags IS NULL OR tags::text = 'null' THEN 1 ELSE 0 END)
FROM pss_dvnx.freshdesk_ticket_header;
"@

# 10. Resumen general
Write-Host "`n🔟 RESUMEN GENERAL" -ForegroundColor Cyan
Write-Host ("─" * 60) -ForegroundColor Gray

$queries = @{
    "Total Tickets" = "SELECT COUNT(*) FROM pss_dvnx.freshdesk_ticket_header"
    "Abiertos" = "SELECT COUNT(*) FROM pss_dvnx.freshdesk_ticket_header WHERE status IN (2,3,6,7)"
    "Cerrados" = "SELECT COUNT(*) FROM pss_dvnx.freshdesk_ticket_header WHERE status IN (4,5)"
    "Sin Asignar" = "SELECT COUNT(*) FROM pss_dvnx.freshdesk_ticket_header WHERE responder_id IS NULL"
    "Urgentes" = "SELECT COUNT(*) FROM pss_dvnx.freshdesk_ticket_header WHERE priority = 4"
}

foreach ($key in $queries.Keys) {
    $env:PGPASSWORD = $Password
    $result = & psql -h $Server -p $Port -U $Username -d $Database -t -A -c $queries[$key] 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   $key : $result" -ForegroundColor White
    }
}

Write-Host "`n╔══════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "║ VERIFICACIÓN COMPLETADA" -ForegroundColor Green
Write-Host "╚══════════════════════════════════════════════════════════════" -ForegroundColor Green

Write-Host "`n💡 Comandos útiles:" -ForegroundColor Yellow
Write-Host "   Ver script SQL completo: cat scripts\verify-freshdesk-sync-data.sql" -ForegroundColor White
Write-Host "   Ejecutar en psql: psql -h $Server -p $Port -U $Username -d $Database -f scripts\verify-freshdesk-sync-data.sql" -ForegroundColor White
Write-Host "   Ver ticket específico: SELECT * FROM pss_dvnx.freshdesk_ticket_header WHERE ticket_id = XXXXX;" -ForegroundColor White
