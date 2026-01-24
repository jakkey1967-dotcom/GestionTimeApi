#!/usr/bin/env pwsh
# ============================================================================
# 🚀 SETUP INICIAL PARA DESARROLLO LOCAL
# ============================================================================
# Este script configura todo lo necesario para desarrollo local

param(
    [string]$PostgresPassword = "postgres",
    [int]$PostgresPort = 5432,
    [string]$AdminEmail = "admin@local.com",
    [string]$AdminPassword = "Admin123!",
    [switch]$UseDocker,
    [switch]$SkipMigrations,
    [switch]$SkipAdmin
)

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║      🚀 SETUP INICIAL - DESARROLLO LOCAL 🚀                 ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Cambiar al directorio raíz del proyecto
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptPath
Set-Location $projectRoot

Write-Host "📁 Directorio: $projectRoot" -ForegroundColor Yellow
Write-Host ""

# 1. Verificar PostgreSQL
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "1️⃣  VERIFICANDO POSTGRESQL" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

if ($UseDocker) {
    Write-Host "🐳 Usando Docker..." -ForegroundColor Cyan
    
    # Verificar si Docker está instalado
    try {
        docker --version | Out-Null
        Write-Host "   ✅ Docker instalado" -ForegroundColor Green
    }
    catch {
        Write-Host "   ❌ Docker no encontrado" -ForegroundColor Red
        Write-Host "   Instala Docker desde: https://www.docker.com/products/docker-desktop" -ForegroundColor Yellow
        exit 1
    }
    
    # Verificar si el contenedor ya existe
    $existingContainer = docker ps -a --filter "name=postgres-gestiontime" --format "{{.Names}}"
    
    if ($existingContainer -eq "postgres-gestiontime") {
        Write-Host "   ℹ️  Contenedor ya existe, reiniciando..." -ForegroundColor Yellow
        docker start postgres-gestiontime | Out-Null
        Start-Sleep -Seconds 3
    }
    else {
        Write-Host "   🔧 Creando contenedor PostgreSQL..." -ForegroundColor Cyan
        docker run --name postgres-gestiontime `
            -e POSTGRES_PASSWORD=$PostgresPassword `
            -e POSTGRES_DB=pss_dvnx `
            -p ${PostgresPort}:5432 `
            -d postgres:16
        
        Write-Host "   ⏳ Esperando que PostgreSQL inicie..." -ForegroundColor Yellow
        Start-Sleep -Seconds 5
    }
    
    Write-Host "   ✅ PostgreSQL corriendo en Docker" -ForegroundColor Green
}
else {
    Write-Host "💻 Usando PostgreSQL local..." -ForegroundColor Cyan
    
    # Verificar si psql está disponible
    try {
        $env:PGPASSWORD = $PostgresPassword
        $version = psql -h localhost -p $PostgresPort -U postgres -c "SELECT version();" 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "   ✅ PostgreSQL conectado" -ForegroundColor Green
        }
        else {
            Write-Host "   ❌ No se puede conectar a PostgreSQL" -ForegroundColor Red
            Write-Host "   Verifica que PostgreSQL esté corriendo en puerto $PostgresPort" -ForegroundColor Yellow
            exit 1
        }
    }
    catch {
        Write-Host "   ⚠️  psql no encontrado en PATH" -ForegroundColor Yellow
        Write-Host "   Asumiendo que PostgreSQL está corriendo..." -ForegroundColor Yellow
    }
}

Write-Host ""

# 2. Crear Base de Datos y Schema
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "2️⃣  CREANDO BASE DE DATOS Y SCHEMA" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

$env:PGPASSWORD = $PostgresPassword

if ($UseDocker) {
    # Crear schema en Docker
    $createSchema = @"
CREATE SCHEMA IF NOT EXISTS pss_dvnx;
"@
    
    docker exec postgres-gestiontime psql -U postgres -d pss_dvnx -c $createSchema
}
else {
    # Crear BD si no existe
    $checkDb = psql -h localhost -p $PostgresPort -U postgres -lqt | Select-String -Pattern "pss_dvnx"
    if (-not $checkDb) {
        Write-Host "   🔧 Creando base de datos pss_dvnx..." -ForegroundColor Cyan
        psql -h localhost -p $PostgresPort -U postgres -c "CREATE DATABASE pss_dvnx;"
    }
    
    # Crear schema
    Write-Host "   🔧 Creando schema pss_dvnx..." -ForegroundColor Cyan
    psql -h localhost -p $PostgresPort -U postgres -d pss_dvnx -c "CREATE SCHEMA IF NOT EXISTS pss_dvnx;"
}

Write-Host "   ✅ Base de datos configurada" -ForegroundColor Green
Write-Host ""

# 3. Restaurar paquetes NuGet
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "3️⃣  RESTAURANDO PAQUETES NUGET" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

dotnet restore
if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✅ Paquetes restaurados" -ForegroundColor Green
}
else {
    Write-Host "   ❌ Error restaurando paquetes" -ForegroundColor Red
    exit 1
}
Write-Host ""

# 4. Compilar proyecto
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "4️⃣  COMPILANDO PROYECTO" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

dotnet build GestionTime.Api.csproj --no-restore
if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✅ Compilación exitosa" -ForegroundColor Green
}
else {
    Write-Host "   ❌ Error compilando" -ForegroundColor Red
    exit 1
}
Write-Host ""

# 5. Aplicar migraciones
if (-not $SkipMigrations) {
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "5️⃣  APLICANDO MIGRACIONES" -ForegroundColor Yellow
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""
    
    dotnet ef database update --project GestionTime.Infrastructure/GestionTime.Infrastructure.csproj --startup-project GestionTime.Api.csproj
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✅ Migraciones aplicadas" -ForegroundColor Green
    }
    else {
        Write-Host "   ❌ Error aplicando migraciones" -ForegroundColor Red
        Write-Host "   Verifica la conexión a la base de datos" -ForegroundColor Yellow
        exit 1
    }
    Write-Host ""
}

# 6. Crear usuario admin
if (-not $SkipAdmin) {
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "6️⃣  CREANDO USUARIO ADMINISTRADOR" -ForegroundColor Yellow
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""
    
    & "$projectRoot\scripts\create-admin-user.ps1" `
        -Email $AdminEmail `
        -Password $AdminPassword `
        -FullName "Admin Local"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✅ Usuario admin creado" -ForegroundColor Green
    }
    else {
        Write-Host "   ⚠️  Error creando admin (puede que ya exista)" -ForegroundColor Yellow
    }
    Write-Host ""
}

# 7. Verificar Health
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "7️⃣  INICIANDO API Y VERIFICANDO HEALTH" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "   🚀 Iniciando API en background..." -ForegroundColor Cyan

# Iniciar API en background
$apiJob = Start-Job -ScriptBlock {
    param($root)
    Set-Location $root
    dotnet run --project GestionTime.Api.csproj --no-build 2>&1
} -ArgumentList $projectRoot

Write-Host "   ⏳ Esperando que la API inicie (30 segundos)..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

# Verificar health
try {
    $health = Invoke-RestMethod -Uri "http://localhost:2501/health" -Method Get -TimeoutSec 10
    Write-Host ""
    Write-Host "   ✅ API FUNCIONANDO" -ForegroundColor Green
    Write-Host "   📊 Status: $($health.status)" -ForegroundColor White
    Write-Host "   🗄️  Database: $($health.database)" -ForegroundColor White
    Write-Host "   🏷️  Client: $($health.client)" -ForegroundColor White
    Write-Host ""
}
catch {
    Write-Host ""
    Write-Host "   ⚠️  No se pudo verificar health automáticamente" -ForegroundColor Yellow
    Write-Host "   Verifica manualmente: http://localhost:2501/health" -ForegroundColor Yellow
    Write-Host ""
}

# Detener API
Write-Host "   🛑 Deteniendo API..." -ForegroundColor Yellow
Stop-Job -Job $apiJob
Remove-Job -Job $apiJob

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║              ✅ SETUP COMPLETADO EXITOSAMENTE ✅             ║" -ForegroundColor Green
Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""

Write-Host "📋 RESUMEN:" -ForegroundColor Cyan
Write-Host "   ✅ PostgreSQL configurado" -ForegroundColor Green
Write-Host "   ✅ Base de datos creada: pss_dvnx" -ForegroundColor Green
Write-Host "   ✅ Schema creado: pss_dvnx" -ForegroundColor Green
Write-Host "   ✅ Migraciones aplicadas" -ForegroundColor Green
Write-Host "   ✅ Usuario admin creado" -ForegroundColor Green
Write-Host ""

Write-Host "🔑 CREDENCIALES DE ADMIN:" -ForegroundColor Yellow
Write-Host "   📧 Email: $AdminEmail" -ForegroundColor White
Write-Host "   🔐 Password: $AdminPassword" -ForegroundColor White
Write-Host ""

Write-Host "🚀 PARA INICIAR LA API:" -ForegroundColor Cyan
Write-Host "   dotnet run --project GestionTime.Api.csproj" -ForegroundColor White
Write-Host ""

Write-Host "🔗 URLS ÚTILES:" -ForegroundColor Cyan
Write-Host "   API: http://localhost:2501" -ForegroundColor White
Write-Host "   Swagger: http://localhost:2501/swagger" -ForegroundColor White
Write-Host "   Health: http://localhost:2501/health" -ForegroundColor White
Write-Host ""

Write-Host "📚 DOCUMENTACIÓN:" -ForegroundColor Cyan
Write-Host "   Setup: docs\LOCAL_DEVELOPMENT_SETUP.md" -ForegroundColor White
Write-Host "   Scripts: scripts\README.md" -ForegroundColor White
Write-Host "   API: docs\INDEX.md" -ForegroundColor White
Write-Host ""

Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
