#!/usr/bin/env pwsh
# diagnose-auth-headers.ps1
# Diagnostica por qué el token JWT no funciona en los tests

$ErrorActionPreference = "Stop"
$baseUrl = "http://localhost:2501"
$email = "psantos@global-retail.com"
$password = "12345678"

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host " 🔍 DIAGNÓSTICO: Por qué falla la autenticación en tests" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# ============================================================================
# 1. OBTENER TOKEN
# ============================================================================

Write-Host "🔐 Paso 1: Obteniendo token JWT..." -ForegroundColor Yellow
Write-Host ""

$loginBody = @{
    email = $email
    password = $password
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod `
    -Uri "$baseUrl/api/v1/auth/login-desktop" `
    -Method Post `
    -ContentType "application/json" `
    -Body $loginBody

$token = $loginResponse.accessToken
$userId = $loginResponse.user.id
$userRole = $loginResponse.user.role

Write-Host "✅ Token obtenido:" -ForegroundColor Green
Write-Host "   Usuario: $($loginResponse.user.email)" -ForegroundColor Gray
Write-Host "   Rol: $userRole" -ForegroundColor Gray
Write-Host "   User ID: $userId" -ForegroundColor Gray
Write-Host "   Token (primeros 50 chars): $($token.Substring(0, [Math]::Min(50, $token.Length)))..." -ForegroundColor Gray
Write-Host "   Token length: $($token.Length) caracteres" -ForegroundColor Gray
Write-Host ""

# ============================================================================
# 2. PROBAR CON INVOKE-WEBREQUEST (COMO EN EL TEST)
# ============================================================================

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
Write-Host "🧪 Paso 2: Probando con Invoke-WebRequest (método del test)" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
Write-Host ""

$headers1 = @{
    "Authorization" = "Bearer $token"
}

Write-Host "Headers enviados:" -ForegroundColor Gray
$headers1 | Format-Table -AutoSize | Out-String | ForEach-Object { Write-Host $_.Trim() -ForegroundColor DarkGray }
Write-Host ""

try {
    $testUrl = "$baseUrl/api/v2/informes/partes?date=2026-02-14&pageSize=5"
    Write-Host "URL: $testUrl" -ForegroundColor Gray
    Write-Host ""
    
    $response1 = Invoke-WebRequest `
        -Uri $testUrl `
        -Method Get `
        -Headers $headers1 `
        -UseBasicParsing `
        -ErrorAction Stop
    
    Write-Host "✅ Status: $($response1.StatusCode)" -ForegroundColor Green
    $json1 = $response1.Content | ConvertFrom-Json
    Write-Host "📊 Total registros: $($json1.total)" -ForegroundColor Green
}
catch {
    Write-Host "❌ FALLÓ con Invoke-WebRequest" -ForegroundColor Red
    Write-Host "   Status: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# ============================================================================
# 3. PROBAR CON INVOKE-RESTMETHOD
# ============================================================================

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
Write-Host "🧪 Paso 3: Probando con Invoke-RestMethod (alternativo)" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
Write-Host ""

$headers2 = @{
    "Authorization" = "Bearer $token"
}

try {
    $testUrl = "$baseUrl/api/v2/informes/partes?date=2026-02-14&pageSize=5"
    Write-Host "URL: $testUrl" -ForegroundColor Gray
    Write-Host ""
    
    $response2 = Invoke-RestMethod `
        -Uri $testUrl `
        -Method Get `
        -Headers $headers2 `
        -ErrorAction Stop
    
    Write-Host "✅ FUNCIONA con Invoke-RestMethod!" -ForegroundColor Green
    Write-Host "📊 Total registros: $($response2.total)" -ForegroundColor Green
    Write-Host "📄 Página: $($response2.page) / Tamaño: $($response2.pageSize)" -ForegroundColor Green
    Write-Host "📦 Items retornados: $($response2.items.Count)" -ForegroundColor Green
    
    if ($response2.items.Count -gt 0) {
        Write-Host ""
        Write-Host "   Ejemplo de registro:" -ForegroundColor Gray
        $item = $response2.items[0]
        Write-Host "   - Fecha: $($item.fechaTrabajo)" -ForegroundColor Gray
        Write-Host "   - Agente: $($item.agenteNombre)" -ForegroundColor Gray
        Write-Host "   - Duración: $($item.duracionMin) min" -ForegroundColor Gray
    }
}
catch {
    Write-Host "❌ También falló con Invoke-RestMethod" -ForegroundColor Red
    Write-Host "   Status: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# ============================================================================
# 4. VERIFICAR ENDPOINT V1 (PARA COMPARAR)
# ============================================================================

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
Write-Host "🧪 Paso 4: Probando endpoint v1 (para comparar)" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
Write-Host ""

try {
    $testUrl = "$baseUrl/api/v1/catalog/clientes?limit=3"
    Write-Host "URL: $testUrl" -ForegroundColor Gray
    Write-Host ""
    
    $response3 = Invoke-RestMethod `
        -Uri $testUrl `
        -Method Get `
        -Headers $headers2 `
        -ErrorAction Stop
    
    Write-Host "✅ Endpoint v1 funciona!" -ForegroundColor Green
    Write-Host "📊 Total clientes: $($response3.total)" -ForegroundColor Green
}
catch {
    Write-Host "❌ Endpoint v1 también falla" -ForegroundColor Red
    Write-Host "   Status: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
}

Write-Host ""

# ============================================================================
# 5. CONCLUSIÓN
# ============================================================================

Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
Write-Host " 📊 CONCLUSIÓN DEL DIAGNÓSTICO" -ForegroundColor Cyan
Write-Host " ────────────────────────────────────────────────────────────────" -ForegroundColor Gray
Write-Host ""
Write-Host "   ✅ Token JWT obtenido correctamente" -ForegroundColor Green
Write-Host "   ❓ Invoke-WebRequest: Ver resultado arriba" -ForegroundColor Yellow
Write-Host "   ❓ Invoke-RestMethod: Ver resultado arriba" -ForegroundColor Yellow
Write-Host ""
Write-Host " 💡 RECOMENDACIONES:" -ForegroundColor Cyan
Write-Host ""
Write-Host "   Si Invoke-RestMethod funciona pero Invoke-WebRequest NO:" -ForegroundColor White
Write-Host "   → Cambiar los scripts de test para usar Invoke-RestMethod" -ForegroundColor Yellow
Write-Host ""
Write-Host "   Si ambos fallan:" -ForegroundColor White
Write-Host "   → Verificar logs del servidor (búscar 'Authorization')" -ForegroundColor Yellow
Write-Host "   → Verificar que la API esté escuchando en puerto 2501" -ForegroundColor Yellow
Write-Host "   → Verificar configuración JWT en Program.cs" -ForegroundColor Yellow
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
