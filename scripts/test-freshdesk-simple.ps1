# 🧪 Test Simple de Freshdesk
Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║          🧪 Test de Freshdesk - SIMPLE                  ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

$email = "psantos@global-retail.com"
$password = "12345678"

Write-Host "📋 Credenciales:" -ForegroundColor Cyan
Write-Host "   Email:    $email" -ForegroundColor White
Write-Host "   Password: $password" -ForegroundColor White
Write-Host ""
Write-Host "🌐 Abre Swagger en tu navegador:" -ForegroundColor Yellow
Write-Host "   https://localhost:2502/swagger" -ForegroundColor White
Write-Host ""
Write-Host "📝 Pasos para probar:" -ForegroundColor Cyan
Write-Host ""
Write-Host "1️⃣  POST /api/v1/auth/login" -ForegroundColor Green
Write-Host "   • Click 'Try it out'" -ForegroundColor Gray
Write-Host "   • Pon el JSON:" -ForegroundColor Gray
Write-Host '     {' -ForegroundColor White
Write-Host '       "email": "psantos@global-retail.com",' -ForegroundColor White
Write-Host '       "password": "12345678"' -ForegroundColor White
Write-Host '     }' -ForegroundColor White
Write-Host "   • Click 'Execute'" -ForegroundColor Gray
Write-Host "   • ✅ Deberías ver 200 OK con cookies" -ForegroundColor Green
Write-Host ""
Write-Host "2️⃣  GET /api/freshdesk/test-connection" -ForegroundColor Green
Write-Host "   • Click 'Try it out'" -ForegroundColor Gray
Write-Host "   • Click 'Execute'" -ForegroundColor Gray
Write-Host "   • ✅ Debería conectarse a Freshdesk" -ForegroundColor Green
Write-Host ""
Write-Host "3️⃣  GET /api/freshdesk/tickets/suggest" -ForegroundColor Green
Write-Host "   • Click 'Try it out'" -ForegroundColor Gray
Write-Host "   • Pon limit: 5" -ForegroundColor Gray
Write-Host "   • Click 'Execute'" -ForegroundColor Gray
Write-Host "   • ✅ Debería traer tickets de Freshdesk" -ForegroundColor Green
Write-Host ""
Write-Host "4️⃣  GET /api/freshdesk/tags/suggest" -ForegroundColor Green
Write-Host "   • Click 'Try it out'" -ForegroundColor Gray
Write-Host "   • Pon limit: 10" -ForegroundColor Gray
Write-Host "   • Click 'Execute'" -ForegroundColor Gray
Write-Host "   • ✅ Debería traer tags" -ForegroundColor Green
Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Yellow
Write-Host "║  💡 Si el login falla con 401, ejecuta primero:         ║" -ForegroundColor Yellow
Write-Host "║     .\scripts\fix-login.ps1                             ║" -ForegroundColor Yellow
Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Yellow
Write-Host ""

# Intentar abrir Swagger automáticamente
try {
    Start-Process "https://localhost:2502/swagger"
    Write-Host "✅ Abriendo Swagger en el navegador..." -ForegroundColor Green
} catch {
    Write-Host "⚠️  No se pudo abrir el navegador automáticamente" -ForegroundColor Yellow
}
