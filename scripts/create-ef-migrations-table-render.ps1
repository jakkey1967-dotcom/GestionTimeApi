# ========================================
# 🔧 CREAR TABLA __EFMigrationsHistory EN RENDER
# ========================================
# Script para crear la tabla de migraciones de EF Core

$ErrorActionPreference = "Stop"

Write-Host "🔧 CREAR TABLA __EFMigrationsHistory EN RENDER" -ForegroundColor Cyan
Write-Host "=" * 60

# Variables de conexión (REEMPLAZAR CON TUS DATOS DE RENDER)
$DB_HOST = "dpg-xxxxxx-a.oregon-postgres.render.com"  # Reemplazar
$DB_PORT = "5432"
$DB_NAME = "pss_dvnx"
$DB_USER = "pss_dvnx_user"  # Reemplazar
$DB_PASSWORD = "xxxxxxxxxxxxxxxxxxxxxxx"  # Reemplazar

Write-Host "`n📝 INSTRUCCIONES:" -ForegroundColor Yellow
Write-Host "1. Ir a Render Dashboard → PostgreSQL → Connection" -ForegroundColor Gray
Write-Host "2. Copiar los valores y reemplazar en este script:" -ForegroundColor Gray
Write-Host "   - Host (Internal Database URL)" -ForegroundColor Gray
Write-Host "   - Database Name" -ForegroundColor Gray
Write-Host "   - Username" -ForegroundColor Gray
Write-Host "   - Password" -ForegroundColor Gray
Write-Host ""

# Verificar si psql está instalado
Write-Host "`n🔍 Verificando psql..." -ForegroundColor Yellow
try {
    $psqlVersion = psql --version
    Write-Host "✅ psql encontrado: $psqlVersion" -ForegroundColor Green
}
catch {
    Write-Host "❌ ERROR: psql no está instalado" -ForegroundColor Red
    Write-Host ""
    Write-Host "Instalar PostgreSQL Client:" -ForegroundColor Yellow
    Write-Host "   Windows: https://www.postgresql.org/download/windows/" -ForegroundColor Gray
    Write-Host "   O usar Chocolatey: choco install postgresql" -ForegroundColor Gray
    exit 1
}

# Construir connection string
$PGPASSWORD = $DB_PASSWORD
$env:PGPASSWORD = $PGPASSWORD

Write-Host "`n📊 Conectando a PostgreSQL en Render..." -ForegroundColor Yellow
Write-Host "   Host: $DB_HOST" -ForegroundColor Gray
Write-Host "   Database: $DB_NAME" -ForegroundColor Gray
Write-Host "   User: $DB_USER" -ForegroundColor Gray

# Leer el archivo SQL
$sqlFile = Join-Path $PSScriptRoot "create-ef-migrations-table.sql"

if (-not (Test-Path $sqlFile)) {
    Write-Host "❌ Archivo SQL no encontrado: $sqlFile" -ForegroundColor Red
    exit 1
}

Write-Host "`n🚀 Ejecutando SQL..." -ForegroundColor Yellow

try {
    # Ejecutar el archivo SQL
    psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -f $sqlFile
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n✅ TABLA CREADA EXITOSAMENTE" -ForegroundColor Green
        Write-Host ""
        Write-Host "Próximos pasos:" -ForegroundColor Cyan
        Write-Host "1. Reiniciar el servicio en Render (Manual Deploy)" -ForegroundColor Gray
        Write-Host "2. Probar el endpoint: https://gestiontimeapi.onrender.com/health" -ForegroundColor Gray
        Write-Host "3. Ejecutar: .\scripts\test-tags-render.ps1" -ForegroundColor Gray
    }
    else {
        Write-Host "`n❌ ERROR al ejecutar SQL (código: $LASTEXITCODE)" -ForegroundColor Red
    }
}
catch {
    Write-Host "`n❌ ERROR:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}
finally {
    # Limpiar variable de entorno
    Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
}

Write-Host ""
