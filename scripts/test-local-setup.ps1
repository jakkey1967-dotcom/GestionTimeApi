# =====================================================================
# Test Rápido - Verificar Configuración Local v1.9.0
# =====================================================================

Write-Host "`n🧪 GestionTime API - Test Rápido de Configuración`n" -ForegroundColor Cyan

$baseUrl = "http://localhost:2501"
$testsPassed = 0
$testsFailed = 0

# Test 1: API está corriendo
Write-Host "Test 1: Verificando que la API está corriendo..." -ForegroundColor Yellow
try {
    $health = Invoke-RestMethod -Uri "$baseUrl/health" -TimeoutSec 5 -ErrorAction Stop
    if ($health.status -eq "OK") {
        Write-Host "✅ API corriendo - Versión: $($health.version)" -ForegroundColor Green
        Write-Host "   Cliente: $($health.client)" -ForegroundColor Gray
        Write-Host "   Schema: $($health.schema)" -ForegroundColor Gray
        Write-Host "   BD: $($health.database)" -ForegroundColor Gray
        $testsPassed++
    } else {
        Write-Host "❌ API no está saludable: $($health.status)" -ForegroundColor Red
        $testsFailed++
    }
} catch {
    Write-Host "❌ API no responde. ¿Está corriendo? (dotnet run)" -ForegroundColor Red
    Write-Host "   Error: $_" -ForegroundColor DarkRed
    $testsFailed++
    Write-Host "`n💡 Ejecuta primero: dotnet run" -ForegroundColor Yellow
    exit 1
}

# Test 2: Swagger disponible
Write-Host "`nTest 2: Verificando Swagger..." -ForegroundColor Yellow
try {
    $swagger = Invoke-WebRequest -Uri "$baseUrl/swagger/index.html" -TimeoutSec 5 -ErrorAction Stop
    if ($swagger.StatusCode -eq 200) {
        Write-Host "✅ Swagger disponible en: $baseUrl/swagger" -ForegroundColor Green
        $testsPassed++
    }
} catch {
    Write-Host "❌ Swagger no disponible" -ForegroundColor Red
    $testsFailed++
}

# Test 3: Login con admin
Write-Host "`nTest 3: Probando login con usuario admin..." -ForegroundColor Yellow
$loginBody = @{
    Email = "admin@gestiontime.com"
    Password = "Admin123!"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" `
        -Method POST `
        -ContentType "application/json" `
        -Body $loginBody `
        -TimeoutSec 5 `
        -ErrorAction Stop
    
    if ($loginResponse.accessToken) {
        Write-Host "✅ Login exitoso" -ForegroundColor Green
        Write-Host "   Usuario: $($loginResponse.user.email)" -ForegroundColor Gray
        Write-Host "   Nombre: $($loginResponse.user.nombre)" -ForegroundColor Gray
        Write-Host "   Rol: $($loginResponse.user.rol)" -ForegroundColor Gray
        Write-Host "   Token: $($loginResponse.accessToken.Substring(0, 50))..." -ForegroundColor Gray
        $token = $loginResponse.accessToken
        $testsPassed++
    } else {
        Write-Host "❌ Login falló: No se recibió token" -ForegroundColor Red
        $testsFailed++
    }
} catch {
    Write-Host "❌ Error en login" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor DarkRed
    $testsFailed++
}

# Test 4: Endpoint protegido /me
if ($token) {
    Write-Host "`nTest 4: Probando endpoint protegido /me..." -ForegroundColor Yellow
    $headers = @{
        Authorization = "Bearer $token"
    }
    
    try {
        $meResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/me" `
            -Method GET `
            -Headers $headers `
            -TimeoutSec 5 `
            -ErrorAction Stop
        
        Write-Host "✅ Autenticación JWT funcionando" -ForegroundColor Green
        Write-Host "   Email: $($meResponse.email)" -ForegroundColor Gray
        Write-Host "   Rol: $($meResponse.rol)" -ForegroundColor Gray
        $testsPassed++
    } catch {
        Write-Host "❌ Error en endpoint protegido" -ForegroundColor Red
        Write-Host "   $($_.Exception.Message)" -ForegroundColor DarkRed
        $testsFailed++
    }
}

# Test 5: Verificar conexión a BD
Write-Host "`nTest 5: Verificando conexión a PostgreSQL..." -ForegroundColor Yellow
try {
    $pgTest = Test-NetConnection localhost -Port 5434 -WarningAction SilentlyContinue
    if ($pgTest.TcpTestSucceeded) {
        Write-Host "✅ PostgreSQL está corriendo en puerto 5434" -ForegroundColor Green
        $testsPassed++
    } else {
        Write-Host "❌ PostgreSQL no responde en puerto 5434" -ForegroundColor Red
        $testsFailed++
    }
} catch {
    Write-Host "❌ Error verificando PostgreSQL" -ForegroundColor Red
    $testsFailed++
}

# Test 6: Endpoint de catálogo (clientes)
if ($token) {
    Write-Host "`nTest 6: Probando endpoint de catálogo..." -ForegroundColor Yellow
    $headers = @{
        Authorization = "Bearer $token"
    }
    
    try {
        $clientes = Invoke-RestMethod -Uri "$baseUrl/api/clientes?pageNumber=1&pageSize=10" `
            -Method GET `
            -Headers $headers `
            -TimeoutSec 5 `
            -ErrorAction Stop
        
        Write-Host "✅ Endpoint de catálogos funcionando" -ForegroundColor Green
        Write-Host "   Total clientes: $($clientes.totalCount)" -ForegroundColor Gray
        $testsPassed++
    } catch {
        Write-Host "⚠️  Endpoint de catálogos no disponible" -ForegroundColor Yellow
        Write-Host "   (Puede ser normal si no hay datos)" -ForegroundColor DarkGray
    }
}

# Resumen
Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "📊 RESUMEN DE TESTS" -ForegroundColor White
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "✅ Tests exitosos: $testsPassed" -ForegroundColor Green
Write-Host "❌ Tests fallidos: $testsFailed" -ForegroundColor Red

if ($testsFailed -eq 0) {
    Write-Host "`n🎉 ¡TODO FUNCIONANDO CORRECTAMENTE!" -ForegroundColor Green
    Write-Host "`n📋 Información útil:" -ForegroundColor White
    Write-Host "   • API URL: $baseUrl" -ForegroundColor Gray
    Write-Host "   • Swagger: $baseUrl/swagger" -ForegroundColor Gray
    Write-Host "   • Health: $baseUrl/health" -ForegroundColor Gray
    Write-Host "   • Usuario: admin@gestiontime.com" -ForegroundColor Gray
    Write-Host "   • Password: Admin123!" -ForegroundColor Gray
} else {
    Write-Host "`n⚠️  Algunos tests fallaron. Revisa la configuración." -ForegroundColor Yellow
    Write-Host "`n📖 Ver guía completa: LOCAL_SETUP_GUIDE.md" -ForegroundColor White
}

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━`n" -ForegroundColor Cyan

# Guardar token en archivo temporal para otros scripts
if ($token) {
    $token | Out-File -FilePath "temp_token.txt" -NoNewline
    Write-Host "💾 Token guardado en temp_token.txt para uso en otros scripts`n" -ForegroundColor DarkGray
}
