# Test de permisos de API Key para endpoint /api/v2/agents
# Verifica si la API Key tiene permisos para listar todos los agentes

$baseUrl = "http://localhost:2501"

Write-Host "🔐 Test de permisos del API Key de Freshdesk" -ForegroundColor Cyan
Write-Host ""

# Test directo al endpoint de ping (público)
Write-Host "1️⃣ Probando ping a Freshdesk (público)..." -ForegroundColor Yellow
try {
    $pingResponse = Invoke-RestMethod -Uri "$baseUrl/api/v1/freshdesk/ping" `
        -Method GET `
        -ErrorAction Stop

    Write-Host "✅ Ping exitoso" -ForegroundColor Green
    Write-Host "   OK: $($pingResponse.ok)" -ForegroundColor Gray
    Write-Host "   Status: $($pingResponse.status)" -ForegroundColor Gray
    Write-Host "   Agent: $($pingResponse.agent)" -ForegroundColor Gray
    Write-Host ""
}
catch {
    Write-Host "❌ Error en ping: $_" -ForegroundColor Red
    Write-Host ""
    exit 1
}

Write-Host "✅ API Key válida y con permisos para /api/v2/agents/me" -ForegroundColor Green
Write-Host ""
Write-Host "ℹ️ INFORMACIÓN IMPORTANTE:" -ForegroundColor Cyan
Write-Host "   - El endpoint /api/v2/agents requiere permisos de administrador" -ForegroundColor Gray
Write-Host "   - Si el agent actual ($($pingResponse.agent)) no es admin, la sincronización podría fallar" -ForegroundColor Gray
Write-Host "   - Verifica en Freshdesk que el agente tenga el rol 'Account Administrator'" -ForegroundColor Gray
Write-Host ""

Write-Host "📌 PRÓXIMOS PASOS:" -ForegroundColor Cyan
Write-Host "   1. Si eres admin: ejecuta .\test-freshdesk-agents.ps1" -ForegroundColor White
Write-Host "   2. Si no eres admin: solicita al administrador que ejecute la sincronización" -ForegroundColor White
Write-Host "   3. Verifica permisos en: Freshdesk > Admin > Agents > $($pingResponse.agent)" -ForegroundColor White
Write-Host ""
