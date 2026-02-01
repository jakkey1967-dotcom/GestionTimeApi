# Script para crear tablas de sincronización de Freshdesk
# Ejecuta: .\scripts\create-freshdesk-sync-tables.ps1

$ErrorActionPreference = "Stop"

Write-Host "🔧 Creando tablas de sincronización de Freshdesk..." -ForegroundColor Cyan

# Obtener connection string desde appsettings.Development.json
$appSettings = Get-Content "appsettings.Development.json" | ConvertFrom-Json
$connectionString = $appSettings.ConnectionStrings.DefaultConnection

if (-not $connectionString) {
    Write-Host "❌ No se pudo obtener ConnectionString de appsettings.Development.json" -ForegroundColor Red
    exit 1
}

Write-Host "📝 Connection String obtenido" -ForegroundColor Gray

# Leer el SQL
$sqlScript = Get-Content "scripts\create-freshdesk-ticket-header-tables.sql" -Raw

try {
    # Ejecutar con psql (si está instalado)
    if (Get-Command psql -ErrorAction SilentlyContinue) {
        Write-Host "✅ Usando psql..." -ForegroundColor Green
        
        # Parsear connection string para psql
        if ($connectionString -match "Host=([^;]+);.*Database=([^;]+);.*Username=([^;]+);.*Password=([^;]+)") {
            $host = $matches[1]
            $database = $matches[2]
            $username = $matches[3]
            $password = $matches[4]
            
            $env:PGPASSWORD = $password
            $sqlScript | & psql -h $host -U $username -d $database -f -
            Remove-Item Env:\PGPASSWORD
            
            Write-Host "✅ Tablas creadas exitosamente" -ForegroundColor Green
        }
        else {
            Write-Host "❌ No se pudo parsear el connection string" -ForegroundColor Red
            exit 1
        }
    }
    else {
        Write-Host "❌ psql no está instalado. Instálalo desde: https://www.postgresql.org/download/" -ForegroundColor Red
        Write-Host "   O ejecuta manualmente el script SQL:" -ForegroundColor Yellow
        Write-Host "   scripts\create-freshdesk-ticket-header-tables.sql" -ForegroundColor Yellow
        exit 1
    }
}
catch {
    Write-Host "❌ Error al crear tablas:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

# Verificar que las tablas existan
Write-Host "`n🔍 Verificando tablas creadas..." -ForegroundColor Cyan
try {
    if ($connectionString -match "Host=([^;]+);.*Database=([^;]+);.*Username=([^;]+);.*Password=([^;]+)") {
        $env:PGPASSWORD = $password
        $tables = & psql -h $host -U $username -d $database -t -c "SELECT tablename FROM pg_tables WHERE schemaname = 'pss_dvnx' AND tablename IN ('freshdesk_ticket_header', 'freshdesk_sync_state');"
        Remove-Item Env:\PGPASSWORD
        
        Write-Host "Tablas encontradas:" -ForegroundColor Green
        Write-Host $tables
    }
}
catch {
    Write-Host "⚠️  No se pudo verificar las tablas" -ForegroundColor Yellow
}

Write-Host "`n✅ Proceso completado" -ForegroundColor Green
