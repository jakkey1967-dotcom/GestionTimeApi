# ========================================
# 🚀 DEPLOY RÁPIDO A RENDER
# ========================================
# Pushea los cambios y monitorea el deploy en Render

param(
    [string]$CommitMessage = "fix: Actualizar filtro de tags por nombre"
)

$ErrorActionPreference = "Continue"

Write-Host "🚀 DEPLOY RÁPIDO A RENDER" -ForegroundColor Cyan
Write-Host "=" * 60

# 1. Verificar estado de Git
Write-Host "`n📊 Paso 1: Verificar estado de Git..." -ForegroundColor Yellow
git status --short

# 2. Agregar cambios
Write-Host "`n📦 Paso 2: Agregando cambios..." -ForegroundColor Yellow
git add Controllers/TagsController.cs
git add scripts/quick-deploy-render.ps1
git status --short

# 3. Commit
Write-Host "`n💾 Paso 3: Haciendo commit..." -ForegroundColor Yellow
git commit -m "$CommitMessage"

if ($LASTEXITCODE -ne 0) {
    Write-Host "⚠️  No hay cambios para commitear o hubo un error" -ForegroundColor Yellow
}

# 4. Push
Write-Host "`n🌐 Paso 4: Pusheando a GitHub..." -ForegroundColor Yellow
git push origin main

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Push exitoso" -ForegroundColor Green
} else {
    Write-Host "❌ Error en push" -ForegroundColor Red
    exit 1
}

# 5. Instrucciones para monitorear
Write-Host "`n📡 Paso 5: Monitorear el deploy..." -ForegroundColor Yellow
Write-Host ""
Write-Host "🔗 Abrir en el navegador:" -ForegroundColor Cyan
Write-Host "   Dashboard: https://dashboard.render.com/" -ForegroundColor Gray
Write-Host "   Logs:      https://dashboard.render.com/web/srv-YOUR_SERVICE_ID" -ForegroundColor Gray
Write-Host ""
Write-Host "⏳ El deploy tardará ~3-5 minutos" -ForegroundColor Yellow
Write-Host ""
Write-Host "🧪 Después del deploy, probar el endpoint:" -ForegroundColor Cyan
Write-Host "   curl https://gestiontimeapi.onrender.com/health" -ForegroundColor Gray
Write-Host "   curl https://gestiontimeapi.onrender.com/api/v1/tags?source=a&limit=10" -ForegroundColor Gray
Write-Host ""

# 6. Esperar y hacer ping cada 30s
Write-Host "⏰ Esperando 3 minutos antes de hacer ping..." -ForegroundColor Yellow
Start-Sleep -Seconds 180

Write-Host "`n🏓 Haciendo ping al servicio..." -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "https://gestiontimeapi.onrender.com/health" -Method GET -TimeoutSec 10
    Write-Host "✅ Servicio respondió:" -ForegroundColor Green
    Write-Host ($response | ConvertTo-Json -Depth 3) -ForegroundColor Gray
}
catch {
    Write-Host "⚠️  Servicio aún no responde (normal durante deploy)" -ForegroundColor Yellow
    Write-Host "   Seguir monitoreando en: https://dashboard.render.com/" -ForegroundColor Gray
}

Write-Host "`n✅ DEPLOY COMPLETADO" -ForegroundColor Green
Write-Host "   Revisar logs en Render para confirmar" -ForegroundColor Gray
