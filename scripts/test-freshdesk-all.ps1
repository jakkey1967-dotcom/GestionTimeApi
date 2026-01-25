# 🧪 Test completo de endpoints Freshdesk (SIN AUTENTICACIÓN REQUERIDA)
Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║     🧪 Test Completo de Freshdesk - SIN LOGIN           ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "https://localhost:2502/api/v1/freshdesk"

# Ignorar SSL
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}

Write-Host "🏓 1. Probando PING..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/ping" -Method GET
    if ($response.ok) {
        Write-Host "✅ PING OK" -ForegroundColor Green
        Write-Host "   Agent: $($response.agent)" -ForegroundColor Gray
    } else {
        Write-Host "❌ PING FALLÓ: $($response.error)" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "🧪 2. Probando TEST-CONNECTION (sin email)..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/test-connection" -Method GET
    if ($response.success) {
        Write-Host "✅ TEST-CONNECTION OK" -ForegroundColor Green
        Write-Host "   Message: $($response.message)" -ForegroundColor Gray
    } else {
        Write-Host "❌ TEST-CONNECTION FALLÓ: $($response.error)" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "🔍 3. Probando TEST-CONNECTION (con email)..." -ForegroundColor Yellow
$testEmail = "psantos@global-retail.com"
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/test-connection?email=$testEmail" -Method GET
    if ($response.success) {
        Write-Host "✅ TEST-CONNECTION OK" -ForegroundColor Green
        Write-Host "   Message: $($response.message)" -ForegroundColor Gray
        if ($response.agentId) {
            Write-Host "   Agent ID: $($response.agentId)" -ForegroundColor Gray
        }
    } else {
        Write-Host "⚠️  $($response.message)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║              ✅ PRUEBAS COMPLETADAS                      ║" -ForegroundColor Green
Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""
Write-Host "💡 Nota: Los endpoints de tickets y tags SÍ requieren login" -ForegroundColor Cyan
Write-Host ""
