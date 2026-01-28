# Script para aplicar migraciones a la BD de Render desde local

Write-Host "🗄️ APLICANDO MIGRACIONES A RENDER" -ForegroundColor Cyan

# IMPORTANTE: Reemplazar con tu External Database URL de Render
$DATABASE_URL = "TU_EXTERNAL_DATABASE_URL_AQUI"

if ($DATABASE_URL -eq "TU_EXTERNAL_DATABASE_URL_AQUI") {
    Write-Host "❌ ERROR: Debes configurar DATABASE_URL primero" -ForegroundColor Red
    Write-Host ""
    Write-Host "1. Ve a: https://dashboard.render.com" -ForegroundColor Yellow
    Write-Host "2. PostgreSQL → Connections" -ForegroundColor Yellow
    Write-Host "3. Copia: External Database URL" -ForegroundColor Yellow
    Write-Host "4. Edita este script y pega la URL" -ForegroundColor Yellow
    exit 1
}

Write-Host "📋 Listando migraciones disponibles..." -ForegroundColor Yellow
dotnet ef migrations list --project GestionTime.Infrastructure

Write-Host "`n⏳ Aplicando migraciones a Render..." -ForegroundColor Yellow
dotnet ef database update --connection $DATABASE_URL --project GestionTime.Infrastructure

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ ¡MIGRACIONES APLICADAS EXITOSAMENTE!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Tablas creadas:" -ForegroundColor Cyan
    Write-Host "  • freshdesk_tags" -ForegroundColor White
    Write-Host "  • freshdesk_agent_maps" -ForegroundColor White
    Write-Host "  • parte_tags" -ForegroundColor White
} else {
    Write-Host "`n❌ Error al aplicar migraciones" -ForegroundColor Red
    Write-Host "Ver error arriba para más detalles" -ForegroundColor Yellow
}
