# Script para verificar usuario en la base de datos LOCAL
param(
    [string]$email = ""
)

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║        🔍 Verificar Usuario en Base de Datos Local       ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Si no se proporcionó email, pedir al usuario
if ([string]::IsNullOrEmpty($email)) {
    $email = Read-Host "📧 Ingresa el email del usuario a verificar"
}

Write-Host "🔍 Buscando usuario: $email" -ForegroundColor Yellow
Write-Host ""

# Configuración de conexión (desde appsettings.json)
$dbHost = "localhost"
$dbPort = "5434"
$dbName = "pss_dvnx"
$dbUser = "postgres"
$dbPassword = "postgres"

# Buscar psql de PostgreSQL 16
$psqlPath = "C:\Program Files\PostgreSQL\16\bin\psql.exe"

# Si no existe en esa ruta, buscar en otras ubicaciones comunes
if (-not (Test-Path $psqlPath)) {
    $psqlPath = "C:\PostgreSQL\16\bin\psql.exe"
}
if (-not (Test-Path $psqlPath)) {
    $psqlPath = "C:\Program Files\PostgreSQL\16\bin\psql.exe"
}
if (-not (Test-Path $psqlPath)) {
    # Intentar usar el que esté en el PATH (aunque sea viejo)
    $psqlCmd = Get-Command psql -ErrorAction SilentlyContinue
    if ($psqlCmd) {
        $psqlPath = $psqlCmd.Source
    }
}

if (-not (Test-Path $psqlPath)) {
    Write-Host "❌ Error: No se encontró psql de PostgreSQL 16" -ForegroundColor Red
    Write-Host ""
    Write-Host "💡 Alternativa: Usar herramienta de base de datos (pgAdmin, DBeaver, etc.)" -ForegroundColor Yellow
    Write-Host "   Ejecuta esta query manualmente:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "   SELECT * FROM pss_dvnx.users WHERE email = '$email';" -ForegroundColor White
    Write-Host ""
    Write-Host "   Connection string:" -ForegroundColor Yellow
    Write-Host "   Host=$dbHost Port=$dbPort Database=$dbName User=$dbUser Password=$dbPassword" -ForegroundColor White
    Write-Host ""
    exit 1
}

# Ejecutar consulta
$env:PGPASSWORD = $dbPassword
$query = "SELECT * FROM pss_dvnx.users WHERE email = '$email';"

Write-Host "🔗 Conectando a PostgreSQL..." -ForegroundColor Yellow
Write-Host "📍 Usando: $psqlPath" -ForegroundColor Gray

try {
    $result = & $psqlPath -h $dbHost -p $dbPort -U $dbUser -d $dbName -c $query 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Consulta ejecutada:" -ForegroundColor Green
        Write-Host ""
        Write-Host $result -ForegroundColor White
        Write-Host ""
        
        # Verificar si se encontró el usuario
        if ($result -match "0 rows") {
            Write-Host "⚠️  Usuario NO encontrado en la base de datos" -ForegroundColor Yellow
            Write-Host ""
            Write-Host "💡 Opciones:" -ForegroundColor Cyan
            Write-Host "   1. Crear usuario desde Swagger: POST /api/auth/register" -ForegroundColor White
            Write-Host "   2. Verificar que la API haya ejecutado el seed correctamente" -ForegroundColor White
            Write-Host "   3. Listar todos los usuarios:" -ForegroundColor White
            Write-Host "      psql -h $dbHost -p $dbPort -U $dbUser -d $dbName -c ""SELECT email FROM pss_dvnx.users;""" -ForegroundColor Gray
            Write-Host ""
        }
        else {
            Write-Host "✅ Usuario encontrado!" -ForegroundColor Green
            Write-Host ""
        }
    }
    else {
        Write-Host "❌ Error al ejecutar consulta:" -ForegroundColor Red
        Write-Host $result -ForegroundColor Yellow
    }
}
catch {
    Write-Host "❌ Error de conexión: $($_.Exception.Message)" -ForegroundColor Red
}
finally {
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
}
