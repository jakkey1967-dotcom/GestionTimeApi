# ==============================================================================
# LIMPIEZA RÁPIDA - Error CS0006
# ==============================================================================

Write-Host "🚀 Limpieza rápida de la solución..." -ForegroundColor Cyan
Write-Host ""

# Limpieza agresiva
Write-Host "🧹 Limpiando..." -ForegroundColor Yellow
dotnet clean
Get-ChildItem -Path . -Include bin,obj -Recurse -Directory | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
dotnet nuget locals all --clear

Write-Host ""
Write-Host "📦 Restaurando..." -ForegroundColor Yellow
dotnet restore --force --no-cache

Write-Host ""
Write-Host "🔨 Compilando..." -ForegroundColor Yellow
dotnet build --no-incremental

Write-Host ""
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Problema resuelto!" -ForegroundColor Green
} else {
    Write-Host "❌ Error persiste. Ejecuta: .\scripts\fix-build-error.ps1" -ForegroundColor Red
}
