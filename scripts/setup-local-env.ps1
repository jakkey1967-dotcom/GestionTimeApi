# =====================================================================
# GestionTime API v1.9.0 - Configuración Automática de Entorno Local
# =====================================================================

Write-Host "`n🔧 GestionTime API - Setup Local Environment`n" -ForegroundColor Cyan

# 1. Verificar PostgreSQL
Write-Host "📋 PASO 1: Verificando PostgreSQL..." -ForegroundColor Yellow
try {
    $pgTest = Test-NetConnection localhost -Port 5434 -WarningAction SilentlyContinue
    if ($pgTest.TcpTestSucceeded) {
        Write-Host "✅ PostgreSQL está corriendo en puerto 5434" -ForegroundColor Green
    } else {
        throw "PostgreSQL no está escuchando en puerto 5434"
    }
} catch {
    Write-Host "❌ Error: $_" -ForegroundColor Red
    Write-Host "   💡 Asegúrate de que PostgreSQL esté instalado y corriendo" -ForegroundColor Yellow
    exit 1
}

# 2. Crear base de datos y schema
Write-Host "`n📋 PASO 2: Creando base de datos y schema..." -ForegroundColor Yellow

$createDbScript = @"
-- Verificar si la BD existe
SELECT 'db_exists' FROM pg_database WHERE datname = 'pss_dvnx';

-- Si no existe, se creará manualmente
"@

Write-Host "   Conectando a PostgreSQL..." -ForegroundColor Cyan

$env:PGPASSWORD = "postgres"
$dbCheck = psql -h localhost -p 5434 -U postgres -d postgres -t -c "SELECT 1 FROM pg_database WHERE datname = 'pss_dvnx';" 2>$null

if ($dbCheck -match "1") {
    Write-Host "✅ Base de datos 'pss_dvnx' ya existe" -ForegroundColor Green
} else {
    Write-Host "   Creando base de datos 'pss_dvnx'..." -ForegroundColor Cyan
    psql -h localhost -p 5434 -U postgres -d postgres -c "CREATE DATABASE pss_dvnx;" 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Base de datos 'pss_dvnx' creada" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Error creando BD (puede que ya exista)" -ForegroundColor Yellow
    }
}

# Crear schema
Write-Host "   Creando schema 'pss_dvnx'..." -ForegroundColor Cyan
psql -h localhost -p 5434 -U postgres -d pss_dvnx -c "CREATE SCHEMA IF NOT EXISTS pss_dvnx;" 2>$null
psql -h localhost -p 5434 -U postgres -d pss_dvnx -c "CREATE EXTENSION IF NOT EXISTS pgcrypto;" 2>$null
Write-Host "✅ Schema y extensiones configuradas" -ForegroundColor Green

# 3. Mostrar configuración
Write-Host "`n📋 PASO 3: Configuración Actual" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan

Write-Host "`n🔑 JWT Secret Key:" -ForegroundColor White
Write-Host "   v7ZpQ9mL3H2kN8xR1aT6yW4cE0sB5dU9jF2hK7nP3qL8rM1tX6zA4gS9uV2bC5e" -ForegroundColor Gray

Write-Host "`n🗃️  Base de Datos PostgreSQL:" -ForegroundColor White
Write-Host "   Host:     localhost" -ForegroundColor Gray
Write-Host "   Port:     5434" -ForegroundColor Gray
Write-Host "   Database: pss_dvnx" -ForegroundColor Gray
Write-Host "   Username: postgres" -ForegroundColor Gray
Write-Host "   Password: postgres" -ForegroundColor Gray
Write-Host "   Schema:   pss_dvnx" -ForegroundColor Gray

Write-Host "`n📧 Email SMTP (Opcional):" -ForegroundColor White
Write-Host "   Host:     smtp.ionos.es" -ForegroundColor Gray
Write-Host "   Port:     587" -ForegroundColor Gray
Write-Host "   User:     envio_noreplica@tdkportal.com" -ForegroundColor Gray

Write-Host "`n🔗 Freshdesk (Deshabilitado por defecto):" -ForegroundColor White
Write-Host "   Domain:   alterasoftware" -ForegroundColor Gray
Write-Host "   Enabled:  false" -ForegroundColor Gray

Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━`n" -ForegroundColor Cyan

# 4. Verificar archivos de configuración
Write-Host "📋 PASO 4: Verificando archivos de configuración..." -ForegroundColor Yellow

$requiredFiles = @(
    "appsettings.json",
    "appsettings.Development.json",
    "GestionTime.Api.csproj"
)

foreach ($file in $requiredFiles) {
    if (Test-Path $file) {
        Write-Host "✅ $file" -ForegroundColor Green
    } else {
        Write-Host "❌ $file NO ENCONTRADO" -ForegroundColor Red
    }
}

# 5. Opciones de inicio
Write-Host "`n📋 PASO 5: ¿Qué deseas hacer?" -ForegroundColor Yellow
Write-Host "   1. Iniciar API ahora (dotnet run)" -ForegroundColor Cyan
Write-Host "   2. Verificar tablas en BD" -ForegroundColor Cyan
Write-Host "   3. Crear usuario admin" -ForegroundColor Cyan
Write-Host "   4. Ejecutar tests" -ForegroundColor Cyan
Write-Host "   5. Solo mostrar configuración (hecho)" -ForegroundColor Cyan
Write-Host ""

$choice = Read-Host "Selecciona opción (1-5)"

switch ($choice) {
    "1" {
        Write-Host "`n🚀 Iniciando GestionTime API..." -ForegroundColor Green
        Write-Host "   URL: http://localhost:2501" -ForegroundColor Cyan
        Write-Host "   Swagger: http://localhost:2501/swagger" -ForegroundColor Cyan
        Write-Host "   Health: http://localhost:2501/health" -ForegroundColor Cyan
        Write-Host ""
        dotnet run
    }
    "2" {
        Write-Host "`n🔍 Verificando tablas..." -ForegroundColor Green
        psql -h localhost -p 5434 -U postgres -d pss_dvnx -c "\dt pss_dvnx.*"
        Write-Host ""
        Read-Host "Presiona Enter para continuar"
    }
    "3" {
        Write-Host "`n👤 Creando usuario admin..." -ForegroundColor Green
        dotnet run seed-admin
    }
    "4" {
        Write-Host "`n🧪 Ejecutando tests básicos..." -ForegroundColor Green
        
        Write-Host "`n1️⃣ Test Health Endpoint..." -ForegroundColor Cyan
        $health = Invoke-RestMethod -Uri "http://localhost:2501/health" -ErrorAction SilentlyContinue
        if ($health) {
            Write-Host "✅ API respondiendo: Version $($health.version)" -ForegroundColor Green
        } else {
            Write-Host "⚠️  API no está corriendo. Ejecuta 'dotnet run' primero" -ForegroundColor Yellow
        }
        
        Write-Host "`n2️⃣ Test Login..." -ForegroundColor Cyan
        $body = @{
            Email = "admin@gestiontime.com"
            Password = "Admin123!"
        } | ConvertTo-Json
        
        try {
            $response = Invoke-RestMethod -Uri "http://localhost:2501/api/auth/login" `
                -Method POST `
                -ContentType "application/json" `
                -Body $body `
                -ErrorAction Stop
            
            Write-Host "✅ Login exitoso: $($response.user.email)" -ForegroundColor Green
            Write-Host "   Token: $($response.accessToken.Substring(0, 50))..." -ForegroundColor Gray
        } catch {
            Write-Host "❌ Error en login: $_" -ForegroundColor Red
        }
        
        Write-Host ""
        Read-Host "Presiona Enter para continuar"
    }
    "5" {
        Write-Host "`n✅ Configuración mostrada arriba" -ForegroundColor Green
    }
    default {
        Write-Host "`n✅ Setup completado" -ForegroundColor Green
    }
}

Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "📖 Documentación completa: LOCAL_SETUP_GUIDE.md" -ForegroundColor White
Write-Host "🚀 Iniciar API: dotnet run" -ForegroundColor White
Write-Host "📋 Ver logs: logs/log-$(Get-Date -Format 'yyyy-MM-dd').txt" -ForegroundColor White
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━`n" -ForegroundColor Cyan
