# ========================================
# 🔍 DIAGNÓSTICO: LOCAL vs RENDER (FRESHDESK)
# ========================================

$ErrorActionPreference = "Continue"

Write-Host "🔍 DIAGNÓSTICO: Comparación Local vs Render" -ForegroundColor Cyan
Write-Host "=" * 60

# ========================================
# 1. CONFIGURACIÓN LOCAL
# ========================================
Write-Host "`n📊 1. CONFIGURACIÓN LOCAL" -ForegroundColor Yellow
Write-Host "-" * 60

$localBaseUrl = "https://localhost:2502"
$renderBaseUrl = "https://gestiontimeapi.onrender.com"

Write-Host "`n🏠 LOCAL:" -ForegroundColor Cyan
Write-Host "   URL: $localBaseUrl" -ForegroundColor Gray
Write-Host "   Freshdesk: DISABLED (appsettings.json)" -ForegroundColor Gray
Write-Host "   SyncEnabled: false" -ForegroundColor Gray

Write-Host "`n☁️  RENDER:" -ForegroundColor Cyan
Write-Host "   URL: $renderBaseUrl" -ForegroundColor Gray
Write-Host "   Freshdesk: ENABLED (variables de entorno)" -ForegroundColor Gray
Write-Host "   SyncEnabled: true" -ForegroundColor Gray
Write-Host "   Domain: alterasoftware (desde FRESHDESK__DOMAIN)" -ForegroundColor Gray
Write-Host "   ApiKey: ****** (desde FRESHDESK__APIKEY)" -ForegroundColor Gray

# ========================================
# 2. LOGIN
# ========================================
Write-Host "`n🔐 2. LOGIN EN RENDER" -ForegroundColor Yellow
Write-Host "-" * 60

try {
    $loginBody = @{
        email = "psantos@global-retail.com"
        password = "12345678"
    } | ConvertTo-Json

    $loginResponse = Invoke-RestMethod `
        -Uri "$renderBaseUrl/api/v1/auth/login-desktop" `
        -Method POST `
        -ContentType "application/json" `
        -Body $loginBody
    
    $token = $loginResponse.accessToken
    Write-Host "✅ Login exitoso" -ForegroundColor Green
    Write-Host "   Email: $($loginResponse.user.email)" -ForegroundColor Gray
    Write-Host "   Role: $($loginResponse.user.role)" -ForegroundColor Gray
}
catch {
    Write-Host "❌ Error en login:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Accept" = "application/json"
    "Content-Type" = "application/json"
}

# ========================================
# 3. TABLAS DE FRESHDESK EN RENDER
# ========================================
Write-Host "`n📊 3. VERIFICAR TABLAS DE FRESHDESK EN RENDER" -ForegroundColor Yellow
Write-Host "-" * 60

$tables = @(
    "freshdesk_tags",
    "freshdesk_ticket_header",
    "freshdesk_companies",
    "freshdesk_agents",
    "freshdesk_agent_me"
)

Write-Host "`nTablas esperadas en pss_dvnx schema:" -ForegroundColor Gray
foreach ($table in $tables) {
    Write-Host "   - $table" -ForegroundColor Gray
}

# ========================================
# 4. TAGS - COMPARAR RENDER
# ========================================
Write-Host "`n🏷️  4. ESTADO DE TAGS EN RENDER" -ForegroundColor Yellow
Write-Host "-" * 60

try {
    $tagsResponse = Invoke-RestMethod `
        -Uri "$renderBaseUrl/api/v1/tags?limit=5" `
        -Method GET `
        -Headers $headers
    
    Write-Host "✅ Tags obtenidos: $($tagsResponse.Count)" -ForegroundColor Green
    
    if ($tagsResponse.Count -gt 0) {
        Write-Host "`n   Primeros tags:" -ForegroundColor Gray
        $tagsResponse | Select-Object -First 5 | ForEach-Object {
            Write-Host "     - $_" -ForegroundColor Gray
        }
    } else {
        Write-Host "⚠️  NO HAY TAGS EN RENDER" -ForegroundColor Yellow
        Write-Host "   Posibles causas:" -ForegroundColor Gray
        Write-Host "     1. Tabla freshdesk_tags vacía" -ForegroundColor Gray
        Write-Host "     2. Tabla freshdesk_tags no existe" -ForegroundColor Gray
        Write-Host "     3. No se ha ejecutado sincronización de tags" -ForegroundColor Gray
    }
}
catch {
    Write-Host "❌ Error obteniendo tags:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    
    if ($_.ErrorDetails.Message) {
        try {
            $errorJson = $_.ErrorDetails.Message | ConvertFrom-Json
            Write-Host "   Detalle: $($errorJson.error)" -ForegroundColor Red
        } catch {
            Write-Host "   Detalle: $($_.ErrorDetails.Message)" -ForegroundColor Red
        }
    }
}

# ========================================
# 5. STATS DE TAGS EN RENDER
# ========================================
Write-Host "`n📈 5. ESTADÍSTICAS DE TAGS EN RENDER" -ForegroundColor Yellow
Write-Host "-" * 60

try {
    $statsResponse = Invoke-RestMethod `
        -Uri "$renderBaseUrl/api/v1/tags/stats" `
        -Method GET `
        -Headers $headers
    
    Write-Host "✅ Estadísticas obtenidas:" -ForegroundColor Green
    Write-Host ($statsResponse | ConvertTo-Json -Depth 3) -ForegroundColor Gray
}
catch {
    Write-Host "❌ Error obteniendo estadísticas:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}

# ========================================
# 6. PING A FRESHDESK DESDE RENDER
# ========================================
Write-Host "`n🏓 6. PING A FRESHDESK DESDE RENDER" -ForegroundColor Yellow
Write-Host "-" * 60

try {
    $pingResponse = Invoke-RestMethod `
        -Uri "$renderBaseUrl/api/v1/freshdesk/ping" `
        -Method GET `
        -Headers @{ "Accept" = "application/json" }
    
    if ($pingResponse.ok) {
        Write-Host "✅ Conexión con Freshdesk OK" -ForegroundColor Green
        Write-Host "   Status: $($pingResponse.status)" -ForegroundColor Gray
        Write-Host "   Agent: $($pingResponse.agent)" -ForegroundColor Gray
    } else {
        Write-Host "❌ Conexión con Freshdesk FALLÓ" -ForegroundColor Red
        Write-Host "   Error: $($pingResponse.error)" -ForegroundColor Red
    }
}
catch {
    Write-Host "❌ Error en ping a Freshdesk:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}

# ========================================
# 7. VERIFICAR ENDPOINT DE SINCRONIZACIÓN
# ========================================
Write-Host "`n🔄 7. ENDPOINTS DE SINCRONIZACIÓN DISPONIBLES" -ForegroundColor Yellow
Write-Host "-" * 60

$syncEndpoints = @(
    "POST /api/v1/integrations/freshdesk/sync/tags",
    "POST /api/v1/integrations/freshdesk/sync/ticket-headers",
    "GET /api/v1/integrations/freshdesk/sync/tags/diagnostics"
)

Write-Host "`nEndpoints de sincronización Freshdesk:" -ForegroundColor Gray
foreach ($endpoint in $syncEndpoints) {
    Write-Host "   - $endpoint" -ForegroundColor Gray
}

# ========================================
# 8. DIFERENCIAS CLAVE
# ========================================
Write-Host "`n⚡ 8. DIFERENCIAS CLAVE: LOCAL vs RENDER" -ForegroundColor Yellow
Write-Host "-" * 60

Write-Host "`n┌─ LOCAL (desarrollo)" -ForegroundColor Cyan
Write-Host "│  ✓ Freshdesk DISABLED" -ForegroundColor Gray
Write-Host "│  ✓ ApiKey = 'DISABLED'" -ForegroundColor Gray
Write-Host "│  ✓ SyncEnabled = false" -ForegroundColor Gray
Write-Host "│  ✓ Sin sincronización automática" -ForegroundColor Gray
Write-Host "│  ✓ Base datos: localhost:5434" -ForegroundColor Gray
Write-Host "└──────────────────────────────" -ForegroundColor Cyan

Write-Host "`n┌─ RENDER (producción)" -ForegroundColor Green
Write-Host "│  ✓ Freshdesk ENABLED" -ForegroundColor Gray
Write-Host "│  ✓ ApiKey desde variable FRESHDESK__APIKEY" -ForegroundColor Gray
Write-Host "│  ✓ SyncEnabled = true" -ForegroundColor Gray
Write-Host "│  ✓ Sincronización automática cada 24h" -ForegroundColor Gray
Write-Host "│  ✓ Base datos: PostgreSQL en Render" -ForegroundColor Gray
Write-Host "└──────────────────────────────" -ForegroundColor Green

# ========================================
# 9. ACCIONES RECOMENDADAS
# ========================================
Write-Host "`n💡 9. ACCIONES RECOMENDADAS" -ForegroundColor Yellow
Write-Host "-" * 60

Write-Host "`n¿Qué hacer si NO HAY TAGS en Render?" -ForegroundColor Cyan

Write-Host "`n1️⃣  Verificar tabla freshdesk_tags existe:" -ForegroundColor White
Write-Host "   - Ir a Render Dashboard → PostgreSQL → Shell" -ForegroundColor Gray
Write-Host "   - Ejecutar: \dt pss_dvnx.freshdesk_tags" -ForegroundColor Gray

Write-Host "`n2️⃣  Ejecutar sincronización manual de tags:" -ForegroundColor White
Write-Host "   - Endpoint: POST /api/v1/integrations/freshdesk/sync/tags" -ForegroundColor Gray
Write-Host "   - Requiere rol Admin" -ForegroundColor Gray
Write-Host "   - Script: .\scripts\test-freshdesk-tags-sync.ps1" -ForegroundColor Gray

Write-Host "`n3️⃣  Verificar variables de entorno en Render:" -ForegroundColor White
Write-Host "   - FRESHDESK__DOMAIN = alterasoftware" -ForegroundColor Gray
Write-Host "   - FRESHDESK__APIKEY = (debe estar configurada)" -ForegroundColor Gray
Write-Host "   - FRESHDESK__SYNCENABLED = true" -ForegroundColor Gray

Write-Host "`n4️⃣  Ver logs de sincronización:" -ForegroundColor White
Write-Host "   - Render Dashboard → gestiontimeapi → Logs" -ForegroundColor Gray
Write-Host "   - Buscar: 'FreshdeskSyncBackgroundService'" -ForegroundColor Gray

Write-Host "`n" + ("=" * 60)
Write-Host "✅ DIAGNÓSTICO COMPLETADO" -ForegroundColor Green
Write-Host ("=" * 60)
