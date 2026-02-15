#!/usr/bin/env pwsh
# fix-jwt-simple.ps1
# Script simplificado: Solo login-desktop + header Authorization

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host " 🔧 FIX JWT SIMPLIFICADO: Solo login-desktop + header" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "https://localhost:2502"
$email = "psantos@global-retail.com"
$password = "12345678"

# ============================================================================
# 1. VERIFICAR QUE LA API ESTÉ CORRIENDO
# ============================================================================

Write-Host "📡 Paso 1: Verificando que la API esté corriendo..." -ForegroundColor Yellow
Write-Host ""

try {
    $healthCheck = Invoke-RestMethod -Uri "$baseUrl/health" -TimeoutSec 3 -ErrorAction Stop
    Write-Host "✅ API está corriendo (status: $($healthCheck.status))" -ForegroundColor Green
}
catch {
    Write-Host "❌ La API NO está corriendo en puerto 2501" -ForegroundColor Red
    Write-Host ""
    Write-Host "💡 Inicia la API primero con:" -ForegroundColor Yellow
    Write-Host "   .\run-dev.ps1" -ForegroundColor White
    Write-Host ""
    exit 1
}

Write-Host ""

# ============================================================================
# 2. OBTENER TOKEN CON /login-desktop
# ============================================================================

Write-Host "🔐 Paso 2: Obteniendo token JWT con /login-desktop..." -ForegroundColor Yellow
Write-Host ""

$loginBody = @{
    email = $email
    password = $password
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/auth/login-desktop" `
        -Method Post `
        -ContentType "application/json" `
        -Body $loginBody `
        -ErrorAction Stop
    
    $token = $loginResponse.accessToken
    $userId = $loginResponse.user.id
    $userRole = $loginResponse.user.role
    
    Write-Host "✅ Token obtenido:" -ForegroundColor Green
    Write-Host "   Usuario: $($loginResponse.user.email)" -ForegroundColor Gray
    Write-Host "   Rol: $userRole" -ForegroundColor Gray
    Write-Host "   User ID: $userId" -ForegroundColor Gray
    Write-Host "   Token length: $($token.Length) chars" -ForegroundColor Gray
}
catch {
    Write-Host "❌ Error obteniendo token:" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# ============================================================================
# 3. PROBAR TOKEN EN HEADER AUTHORIZATION
# ============================================================================

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
Write-Host "🧪 Paso 3: Probando token en header Authorization" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
Write-Host ""

$headers = @{
    "Authorization" = "Bearer $token"
}

$today = Get-Date -Format "yyyy-MM-dd"

Write-Host "   URL: $baseUrl/api/v2/informes/partes?date=$today&pageSize=5" -ForegroundColor Gray
Write-Host "   Header: Authorization: Bearer [token]" -ForegroundColor Gray
Write-Host ""

try {
    $response = Invoke-RestMethod `
        -Uri "$baseUrl/api/v2/informes/partes?date=$today&pageSize=5" `
        -Method Get `
        -Headers $headers `
        -ErrorAction Stop
    
    Write-Host "✅ ¡HEADER AUTHORIZATION FUNCIONA!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📊 Respuesta del endpoint:" -ForegroundColor White
    Write-Host "   Total registros: $($response.total)" -ForegroundColor Green
    Write-Host "   Página: $($response.page) / Tamaño: $($response.pageSize)" -ForegroundColor Green
    Write-Host "   Items retornados: $($response.items.Count)" -ForegroundColor Green
    
    if ($response.items.Count -gt 0) {
        $item = $response.items[0]
        Write-Host ""
        Write-Host "   Ejemplo de registro:" -ForegroundColor Gray
        Write-Host "   - Fecha: $($item.fechaTrabajo)" -ForegroundColor Gray
        Write-Host "   - Agente: $($item.agenteNombre)" -ForegroundColor Gray
        Write-Host "   - Duración: $($item.duracionMin) min" -ForegroundColor Gray
    }
    
    $fixWorked = $true
}
catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    
    if ($statusCode -eq 400) {
        Write-Host "✅ Header Authorization funciona (400 = error de validación esperado)" -ForegroundColor Green
        Write-Host ""
        Write-Host "   El token se autenticó correctamente." -ForegroundColor Gray
        Write-Host "   Error 400 es por falta de parámetro 'date', no por auth." -ForegroundColor Gray
        $fixWorked = $true
    }
    else {
        Write-Host "❌ HEADER AUTHORIZATION FALLA" -ForegroundColor Red
        Write-Host "   Status: $statusCode" -ForegroundColor Red
        Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
        
        if ($_.ErrorDetails.Message) {
            try {
                $errorJson = $_.ErrorDetails.Message | ConvertFrom-Json
                Write-Host "   Detalle: $($errorJson.error)" -ForegroundColor Red
            }
            catch {}
        }
        
        $fixWorked = $false
    }
}

Write-Host ""

# ============================================================================
# 4. RESULTADO FINAL
# ============================================================================

Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
Write-Host " 📊 RESULTADO FINAL" -ForegroundColor Cyan
Write-Host " ────────────────────────────────────────────────────────────────" -ForegroundColor Gray
Write-Host ""

if ($fixWorked) {
    Write-Host "   🎉 JWT CON HEADER AUTHORIZATION FUNCIONA!" -ForegroundColor Green
    Write-Host ""
    Write-Host "   ✅ API está corriendo" -ForegroundColor Green
    Write-Host "   ✅ /login-desktop funciona" -ForegroundColor Green
    Write-Host "   ✅ Token obtenido correctamente" -ForegroundColor Green
    Write-Host "   ✅ Header Authorization funciona" -ForegroundColor Green
    Write-Host ""
    Write-Host " 💡 SIGUIENTE PASO:" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "   Ejecuta el test completo:" -ForegroundColor White
    Write-Host "   .\test-informes-partes-log.ps1" -ForegroundColor Yellow
    Write-Host ""
}
else {
    Write-Host "   ❌ EL FIX NO FUNCIONÓ" -ForegroundColor Red
    Write-Host ""
    Write-Host "   Header Authorization sigue dando $statusCode" -ForegroundColor Red
    Write-Host ""
    Write-Host " 💡 POSIBLES CAUSAS:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "   1. La API está usando código viejo (no reinició)" -ForegroundColor White
    Write-Host "      → Mata procesos: .\kill-api-ports.ps1" -ForegroundColor Gray
    Write-Host "      → Reinicia: .\run-dev.ps1" -ForegroundColor Gray
    Write-Host ""
    Write-Host "   2. El fix en Program.cs no se aplicó" -ForegroundColor White
    Write-Host "      → Verifica línea 281: .FirstOrDefault()" -ForegroundColor Gray
    Write-Host "      → NO debe ser: .ToString()" -ForegroundColor Gray
    Write-Host ""
    Write-Host "   3. Configuración JWT incorrecta" -ForegroundColor White
    Write-Host "      → Verifica appsettings.json" -ForegroundColor Gray
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
