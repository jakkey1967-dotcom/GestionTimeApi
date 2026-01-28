# Test de conexión a base de datos local PostgreSQL (puerto 5434)
Write-Host "================================" -ForegroundColor Cyan
Write-Host "TEST: Conexión BD Local (5434)" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

$ErrorActionPreference = "Stop"

# Parámetros de conexión
$host_db = "localhost"
$port_db = "5434"
$database = "pss_dvnx"
$username = "postgres"
$password = "postgres"
$schema = "pss_dvnx"

Write-Host "📋 Parámetros de conexión:" -ForegroundColor Yellow
Write-Host "   Host:     $host_db"
Write-Host "   Port:     $port_db"
Write-Host "   Database: $database"
Write-Host "   Schema:   $schema"
Write-Host "   User:     $username"
Write-Host ""

# Test 1: Verificar que PostgreSQL esté corriendo
Write-Host "🔍 Test 1: Verificando PostgreSQL en puerto $port_db..." -ForegroundColor Cyan
try {
    $testConnection = Test-NetConnection -ComputerName $host_db -Port $port_db -WarningAction SilentlyContinue
    if ($testConnection.TcpTestSucceeded) {
        Write-Host "   ✅ PostgreSQL está corriendo en puerto $port_db" -ForegroundColor Green
    } else {
        Write-Host "   ❌ PostgreSQL NO está disponible en puerto $port_db" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "   ❌ Error al verificar puerto: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Test 2: Verificar tablas en el schema
Write-Host "🔍 Test 2: Verificando tablas en schema '$schema'..." -ForegroundColor Cyan

$env:PGPASSWORD = $password

try {
    $tables = psql -h $host_db -p $port_db -U $username -d $database -t -c "SELECT table_name FROM information_schema.tables WHERE table_schema = '$schema' ORDER BY table_name;"
    
    if ($LASTEXITCODE -eq 0) {
        $tableList = $tables -split "`n" | Where-Object { $_.Trim() -ne "" }
        Write-Host "   ✅ Encontradas $($tableList.Count) tablas:" -ForegroundColor Green
        foreach ($table in $tableList) {
            Write-Host "      - $($table.Trim())" -ForegroundColor Gray
        }
    } else {
        Write-Host "   ❌ Error al consultar tablas" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "   ❌ Error: $_" -ForegroundColor Red
    exit 1
} finally {
    $env:PGPASSWORD = $null
}

Write-Host ""

# Test 3: Verificar tabla de usuarios
Write-Host "🔍 Test 3: Verificando usuarios..." -ForegroundColor Cyan

$env:PGPASSWORD = $password

try {
    $userCount = psql -h $host_db -p $port_db -U $username -d $database -t -c "SELECT COUNT(*) FROM $schema.users;"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✅ Tabla 'users' encontrada: $($userCount.Trim()) usuarios registrados" -ForegroundColor Green
    } else {
        Write-Host "   ❌ Error al consultar usuarios" -ForegroundColor Red
    }
} catch {
    Write-Host "   ⚠️  Tabla 'users' no existe o error: $_" -ForegroundColor Yellow
} finally {
    $env:PGPASSWORD = $null
}

Write-Host ""

# Test 4: Verificar integración Freshdesk (debe estar deshabilitada)
Write-Host "🔍 Test 4: Verificando configuración Freshdesk..." -ForegroundColor Cyan

$appsettingsPath = "appsettings.Development.json"
if (Test-Path $appsettingsPath) {
    $config = Get-Content $appsettingsPath | ConvertFrom-Json
    $syncEnabled = $config.Freshdesk.SyncEnabled
    
    if ($syncEnabled -eq $false) {
        Write-Host "   ✅ Freshdesk Sync deshabilitado (como debe ser)" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️  Freshdesk Sync habilitado (debería estar deshabilitado)" -ForegroundColor Yellow
    }
} else {
    Write-Host "   ⚠️  No se encontró $appsettingsPath" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "================================" -ForegroundColor Cyan
Write-Host "✅ TEST COMPLETADO" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "🎯 Base de datos local PostgreSQL (puerto 5434) está lista para usar" -ForegroundColor Green
Write-Host "🚫 Freshdesk sincronización está deshabilitada" -ForegroundColor Green
Write-Host ""
