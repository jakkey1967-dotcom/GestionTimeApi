# cleanup.ps1
# Script para limpiar artefactos y organizar el repositorio del API

Write-Host "=== Limpieza del Repositorio GestionTime API ===" -ForegroundColor Cyan
Write-Host ""

# 1. Crear carpetas de archivo si no existen
Write-Host "[1/6] Creando carpetas de archivo..." -ForegroundColor Yellow
$archiveApi = "_ARCHIVE_APIS"
$archiveLogs = "_logs_archive"

if (-not (Test-Path $archiveApi)) {
    New-Item -Path $archiveApi -ItemType Directory | Out-Null
    Write-Host "  ✓ Carpeta $archiveApi creada" -ForegroundColor Green
} else {
    Write-Host "  → Carpeta $archiveApi ya existe" -ForegroundColor Gray
}

if (-not (Test-Path $archiveLogs)) {
    New-Item -Path $archiveLogs -ItemType Directory | Out-Null
    Write-Host "  ✓ Carpeta $archiveLogs creada" -ForegroundColor Green
} else {
    Write-Host "  → Carpeta $archiveLogs ya existe" -ForegroundColor Gray
}

# 2. Mover logs existentes a archivo
Write-Host ""
Write-Host "[2/6] Moviendo archivos .log a $archiveLogs..." -ForegroundColor Yellow
$logs = Get-ChildItem -Path . -Filter "*.log" -File -Depth 0
$logsDir = Get-ChildItem -Path "logs" -Filter "*.log" -File -ErrorAction SilentlyContinue

$logsMoved = 0
foreach ($log in $logs) {
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $newName = "$($log.BaseName)_$timestamp$($log.Extension)"
    Move-Item -Path $log.FullName -Destination "$archiveLogs\$newName" -Force
    Write-Host "  ✓ $($log.Name) → $archiveLogs\$newName" -ForegroundColor Green
    $logsMoved++
}

if ($logsMoved -eq 0) {
    Write-Host "  → No hay archivos .log en la raíz para mover" -ForegroundColor Gray
}

# 3. Mover carpeta duplicada GestionTime.Api (si existe contenido duplicado)
Write-Host ""
Write-Host "[3/6] Verificando carpetas duplicadas del API..." -ForegroundColor Yellow

# Verificar si GestionTime.Api es un duplicado
if (Test-Path "GestionTime.Api") {
    $apiRootCsproj = Test-Path "GestionTime.Api.csproj"
    if ($apiRootCsproj) {
        $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
        $targetPath = "$archiveApi\GestionTime.Api_$timestamp"
        Move-Item -Path "GestionTime.Api" -Destination $targetPath -Force
        Write-Host "  ✓ GestionTime.Api/ movido a $targetPath" -ForegroundColor Green
    } else {
        Write-Host "  ⚠ No se encontró GestionTime.Api.csproj en raíz. Revisar manualmente." -ForegroundColor Yellow
    }
} else {
    Write-Host "  → No se encontró carpeta duplicada GestionTime.Api/" -ForegroundColor Gray
}

# 4. Limpiar artefactos de build (.vs, bin, obj, backups existentes)
Write-Host ""
Write-Host "[4/6] Limpiando artefactos de build..." -ForegroundColor Yellow

# Limpiar .vs
if (Test-Path ".vs") {
    Remove-Item -Path ".vs" -Recurse -Force
    Write-Host "  ✓ .vs/ eliminado" -ForegroundColor Green
}

# Limpiar bin y obj en raíz
foreach ($folder in @("bin", "obj")) {
    if (Test-Path $folder) {
        Remove-Item -Path $folder -Recurse -Force
        Write-Host "  ✓ $folder/ eliminado" -ForegroundColor Green
    }
}

# Limpiar bin/obj en todos los proyectos
$projects = Get-ChildItem -Recurse -Directory -Filter "bin" | Where-Object { $_.FullName -notlike "*$archiveApi*" }
$projects += Get-ChildItem -Recurse -Directory -Filter "obj" | Where-Object { $_.FullName -notlike "*$archiveApi*" }

foreach ($proj in $projects) {
    Remove-Item -Path $proj.FullName -Recurse -Force
    Write-Host "  ✓ $($proj.FullName) eliminado" -ForegroundColor Green
}

# Mover carpeta backups existente
if (Test-Path "backups") {
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    Move-Item -Path "backups" -Destination "$archiveApi\backups_$timestamp" -Force
    Write-Host "  ✓ backups/ movido a archivo" -ForegroundColor Green
}

# 5. Remover archivos del tracking de git (sin borrarlos localmente)
Write-Host ""
Write-Host "[5/6] Removiendo artefactos del tracking de Git..." -ForegroundColor Yellow

$gitRemoveItems = @(
    "*.log",
    ".vs/",
    "bin/",
    "obj/",
    "backups/",
    "_ARCHIVE_APIS/",
    "_logs_archive/"
)

foreach ($item in $gitRemoveItems) {
    git rm -r --cached $item 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✓ $item removido del tracking" -ForegroundColor Green
    }
}

# 6. Verificar estructura final
Write-Host ""
Write-Host "[6/6] Verificando estructura del proyecto..." -ForegroundColor Yellow

$csprojPath = "GestionTime.Api.csproj"
if (Test-Path $csprojPath) {
    Write-Host "  ✓ API principal encontrado: $csprojPath" -ForegroundColor Green
    
    # Listar proyectos relacionados
    $projects = Get-ChildItem -Filter "*.csproj" -Recurse -Depth 1 | Select-Object -ExpandProperty Name
    Write-Host "  → Proyectos en el workspace:" -ForegroundColor Cyan
    foreach ($proj in $projects) {
        Write-Host "    • $proj" -ForegroundColor White
    }
} else {
    Write-Host "  ✗ ERROR: No se encontró GestionTime.Api.csproj en la raíz" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Limpieza completada ===" -ForegroundColor Green
Write-Host ""
Write-Host "Siguiente paso: Verificar que el proyecto compila" -ForegroundColor Cyan
Write-Host "  dotnet build GestionTime.Api.csproj" -ForegroundColor White
Write-Host ""
Write-Host "Para ejecutar el API:" -ForegroundColor Cyan
Write-Host "  dotnet run --project GestionTime.Api.csproj" -ForegroundColor White
Write-Host ""
