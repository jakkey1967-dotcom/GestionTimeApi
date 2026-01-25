# 🔧 Script URGENTE para arreglar login
Write-Host ""
Write-Host "🔧 ARREGLANDO LOGIN AHORA MISMO..." -ForegroundColor Yellow
Write-Host ""

$baseUrl = "http://localhost:2501/api"

# 1. Verificar que la API esté corriendo
try {
    Invoke-WebRequest -Uri "http://localhost:2501/health" -Method GET -UseBasicParsing -TimeoutSec 3 | Out-Null
    Write-Host "✅ API corriendo" -ForegroundColor Green
} catch {
    Write-Host "❌ API NO está corriendo. Presiona F5 en Visual Studio" -ForegroundColor Red
    exit 1
}

# 2. Borrar y recrear el usuario usando psql DIRECTO
Write-Host "🗑️  Borrando usuario existente..." -ForegroundColor Yellow

$dbHost = "localhost"
$dbPort = "5434"
$dbName = "pss_dvnx"
$dbUser = "postgres"
$dbPassword = "postgres"
$psqlPath = "C:\Program Files\PostgreSQL\16\bin\psql.exe"
$email = "psantos@global-retail.com"

$env:PGPASSWORD = $dbPassword

# Borrar usuario
$deleteSql = "DELETE FROM pss_dvnx.users WHERE email = '$email';"
& $psqlPath -h $dbHost -p $dbPort -U $dbUser -d $dbName -c $deleteSql 2>&1 | Out-Null

Write-Host "✅ Usuario borrado" -ForegroundColor Green

# 3. Registrar usuario nuevo usando el endpoint de registro
Write-Host "👤 Creando usuario nuevo vía API..." -ForegroundColor Yellow

$registerBody = @{
    email = $email
    password = "12345678"
    fullName = "Francisco Santos"
} | ConvertTo-Json

try {
    $response = Invoke-WebRequest -Uri "$baseUrl/auth/register" `
        -Method POST `
        -Body $registerBody `
        -ContentType "application/json" `
        -UseBasicParsing `
        -ErrorAction Stop
    
    Write-Host "✅ Usuario creado correctamente" -ForegroundColor Green
} catch {
    Write-Host "⚠️  Error al registrar: $($_.Exception.Message)" -ForegroundColor Yellow
    
    if ($_.ErrorDetails.Message) {
        $errorObj = $_.ErrorDetails.Message | ConvertFrom-Json
        Write-Host "   Detalle: $($errorObj.message)" -ForegroundColor Yellow
    }
}

Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║              ✅ LISTO PARA PROBAR                         ║" -ForegroundColor Green
Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""
Write-Host "Credenciales:" -ForegroundColor Cyan
Write-Host "  Email:    $email" -ForegroundColor White
Write-Host "  Password: 12345678" -ForegroundColor White
Write-Host ""
Write-Host "Ejecuta ahora:" -ForegroundColor Yellow
Write-Host "  .\scripts\test-freshdesk.ps1" -ForegroundColor White
Write-Host ""
