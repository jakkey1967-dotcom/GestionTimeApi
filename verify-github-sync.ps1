#!/usr/bin/env pwsh
# ============================================================================
# VERIFICACIÓN RÁPIDA DEL ESTADO DEL REPOSITORIO
# ============================================================================

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║     🔍 VERIFICACIÓN DE RESPALDO - GitHub Sync Status       ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Cambiar al directorio del proyecto
Set-Location $PSScriptRoot

# 1. Estado del Working Tree
Write-Host "📋 Estado del Working Tree:" -ForegroundColor Yellow
$status = git status --porcelain
if ([string]::IsNullOrEmpty($status)) {
    Write-Host "   ✅ Limpio (nada por commitear)" -ForegroundColor Green
} else {
    Write-Host "   ⚠️  Archivos sin commitear:" -ForegroundColor Red
    git status --short
}
Write-Host ""

# 2. Último Commit Local
Write-Host "💾 Último Commit Local:" -ForegroundColor Yellow
$lastCommit = git log -1 --oneline
Write-Host "   $lastCommit" -ForegroundColor White
Write-Host ""

# 3. Verificar Sincronización con GitHub
Write-Host "🌐 Sincronización con GitHub:" -ForegroundColor Yellow
git fetch origin --quiet

$localCommit = git rev-parse HEAD
$remoteCommit = git rev-parse origin/main

if ($localCommit -eq $remoteCommit) {
    Write-Host "   ✅ Sincronizado con origin/main" -ForegroundColor Green
} else {
    Write-Host "   ⚠️  Desincronizado:" -ForegroundColor Red
    Write-Host "   Local:  $localCommit" -ForegroundColor White
    Write-Host "   Remote: $remoteCommit" -ForegroundColor White
    
    $behind = git rev-list --count HEAD..origin/main
    $ahead = git rev-list --count origin/main..HEAD
    
    if ($ahead -gt 0) {
        Write-Host "   → Tienes $ahead commit(s) por pushear" -ForegroundColor Yellow
    }
    if ($behind -gt 0) {
        Write-Host "   → Estás $behind commit(s) atrás de origin/main" -ForegroundColor Yellow
    }
}
Write-Host ""

# 4. Tags
Write-Host "🏷️  Tags Disponibles:" -ForegroundColor Yellow
$tags = git tag -l
if ($tags) {
    foreach ($tag in $tags) {
        $tagInfo = git log -1 --format="%h %s" $tag
        Write-Host "   📌 $tag → $tagInfo" -ForegroundColor Cyan
    }
} else {
    Write-Host "   (sin tags)" -ForegroundColor Gray
}
Write-Host ""

# 5. Últimos 3 Commits
Write-Host "📜 Últimos 3 Commits:" -ForegroundColor Yellow
git log --oneline -3 --decorate | ForEach-Object {
    Write-Host "   $_" -ForegroundColor White
}
Write-Host ""

# 6. Remotes
Write-Host "🔗 Remotes Configurados:" -ForegroundColor Yellow
git remote -v | ForEach-Object {
    Write-Host "   $_" -ForegroundColor White
}
Write-Host ""

# 7. Estado de la Compilación
Write-Host "🔨 Verificando Compilación:" -ForegroundColor Yellow
try {
    $buildOutput = dotnet build GestionTime.Api.csproj --no-restore --verbosity quiet 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✅ Compilación exitosa" -ForegroundColor Green
    } else {
        Write-Host "   ❌ Errores de compilación" -ForegroundColor Red
        Write-Host $buildOutput
    }
} catch {
    Write-Host "   ⚠️  No se pudo verificar compilación" -ForegroundColor Yellow
}
Write-Host ""

# Resumen Final
Write-Host "╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                    📊 RESUMEN FINAL                         ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

if ([string]::IsNullOrEmpty($status) -and $localCommit -eq $remoteCommit) {
    Write-Host "✅ TODO OK: Repositorio limpio y sincronizado con GitHub" -ForegroundColor Green
    Write-Host "✅ Seguro para hacer cambios en la aplicación cliente" -ForegroundColor Green
    Write-Host ""
    Write-Host "🔄 Para restaurar este punto en el futuro:" -ForegroundColor Cyan
    Write-Host "   git checkout $localCommit" -ForegroundColor White
    if ($tags) {
        Write-Host "   git checkout $($tags[-1])" -ForegroundColor White
    }
} else {
    Write-Host "⚠️  ATENCIÓN: Estado inconsistente" -ForegroundColor Yellow
    Write-Host ""
    if (-not [string]::IsNullOrEmpty($status)) {
        Write-Host "→ Ejecuta: git add . && git commit -m 'descripción'" -ForegroundColor Yellow
    }
    if ($localCommit -ne $remoteCommit -and $ahead -gt 0) {
        Write-Host "→ Ejecuta: git push origin main" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
