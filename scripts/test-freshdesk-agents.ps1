# Test de sincronización de Agents desde Freshdesk
# Endpoint: POST /api/v1/integrations/freshdesk/agents/sync
# Requiere: Usuario ADMIN autenticado

$baseUrl = "http://localhost:2501"
$username = "admin@gestiontime.com"
$password = "Admin123!"

Write-Host "🚀 Test de sincronización de AGENTS desde Freshdesk" -ForegroundColor Cyan
Write-Host ""

# 1. Login
Write-Host "1️⃣ Autenticando usuario ADMIN..." -ForegroundColor Yellow
$loginBody = @{
    email = $username
    password = $password
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/v1/auth/login" `
        -Method POST `
        -Body $loginBody `
        -ContentType "application/json" `
        -SessionVariable session `
        -ErrorAction Stop

    Write-Host "✅ Login exitoso" -ForegroundColor Green
    Write-Host "   Usuario: $($loginResponse.user.email)" -ForegroundColor Gray
    Write-Host "   Role: $($loginResponse.user.role)" -ForegroundColor Gray
    Write-Host ""
}
catch {
    Write-Host "❌ Error en login: $_" -ForegroundColor Red
    exit 1
}

# 2. Sincronizar agents
Write-Host "2️⃣ Sincronizando AGENTS desde Freshdesk..." -ForegroundColor Yellow
Write-Host "   Endpoint: POST /api/v1/integrations/freshdesk/agents/sync" -ForegroundColor Gray
Write-Host ""

try {
    $syncResponse = Invoke-RestMethod -Uri "$baseUrl/api/v1/integrations/freshdesk/agents/sync" `
        -Method POST `
        -WebSession $session `
        -ErrorAction Stop

    Write-Host "✅ Sincronización completada" -ForegroundColor Green
    Write-Host ""
    Write-Host "📊 RESULTADOS:" -ForegroundColor Cyan
    Write-Host "   Success: $($syncResponse.success)" -ForegroundColor White
    Write-Host "   Páginas obtenidas: $($syncResponse.pagesFetched)" -ForegroundColor White
    Write-Host "   Agents upserted: $($syncResponse.agentsUpserted)" -ForegroundColor White
    Write-Host "   Duración: $($syncResponse.durationMs)ms" -ForegroundColor White
    Write-Host ""
    
    if ($syncResponse.sampleFirst3 -and $syncResponse.sampleFirst3.Count -gt 0) {
        Write-Host "👥 Primeros 3 agents sincronizados:" -ForegroundColor Cyan
        foreach ($agent in $syncResponse.sampleFirst3) {
            Write-Host "   - ID: $($agent.agent_id) | Name: $($agent.name) | Email: $($agent.email)" -ForegroundColor Gray
        }
        Write-Host ""
    }
    
    Write-Host "🕐 Timestamps:" -ForegroundColor Cyan
    Write-Host "   Inicio: $($syncResponse.startedAt)" -ForegroundColor Gray
    Write-Host "   Fin: $($syncResponse.completedAt)" -ForegroundColor Gray
    Write-Host ""
}
catch {
    Write-Host "❌ Error en sincronización: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Response completo:" -ForegroundColor Yellow
    $_.ErrorDetails.Message | ConvertFrom-Json | ConvertTo-Json -Depth 10 | Write-Host
    exit 1
}

# 3. Verificar estado
Write-Host "3️⃣ Verificando estado de sincronización..." -ForegroundColor Yellow
try {
    $statusResponse = Invoke-RestMethod -Uri "$baseUrl/api/v1/integrations/freshdesk/agents/status" `
        -Method GET `
        -WebSession $session `
        -ErrorAction Stop

    Write-Host "✅ Estado obtenido" -ForegroundColor Green
    Write-Host ""
    Write-Host "📊 ESTADO ACTUAL:" -ForegroundColor Cyan
    Write-Host "   Total agents: $($statusResponse.totalAgents)" -ForegroundColor White
    Write-Host "   Agents activos: $($statusResponse.activeAgents)" -ForegroundColor White
    Write-Host "   Última actualización en Freshdesk: $($statusResponse.maxUpdatedAt)" -ForegroundColor White
    Write-Host "   Última sincronización: $($statusResponse.maxSyncedAt)" -ForegroundColor White
    Write-Host ""
}
catch {
    Write-Host "⚠️ Error al obtener estado: $_" -ForegroundColor Yellow
}

Write-Host "🎉 Test completado" -ForegroundColor Green
