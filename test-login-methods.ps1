#!/usr/bin/env pwsh
# test-login-methods.ps1
# Compara /login vs /login-desktop

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                                                                ║" -ForegroundColor Cyan
Write-Host "║  🔐 TEST: /login vs /login-desktop                            ║" -ForegroundColor Cyan
Write-Host "║                                                                ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "http://localhost:2501"
$email = "psantos@global-retail.com"
$password = "12345678"

$body = @{
    email = $email
    password = $password
} | ConvertTo-Json

# TEST 1: /login-desktop (retorna token en body)
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
Write-Host "🔹 TEST 1: /api/v1/auth/login-desktop" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
Write-Host ""

try {
    $response1 = Invoke-WebRequest `
        -Uri "$baseUrl/api/v1/auth/login-desktop" `
        -Method Post `
        -ContentType "application/json" `
        -Body $body `
        -ErrorAction Stop
    
    $json1 = $response1.Content | ConvertFrom-Json
    
    Write-Host "✅ Status: $($response1.StatusCode)" -ForegroundColor Green
    Write-Host "📦 Retorna token en body: $(if ($json1.accessToken) { 'SÍ ✅' } else { 'NO ❌' })" -ForegroundColor White
    Write-Host "🍪 Establece cookies: $(if ($response1.Headers['Set-Cookie']) { 'SÍ' } else { 'NO' })" -ForegroundColor White
    
    if ($json1.accessToken) {
        $tokenLength = $json1.accessToken.Length
        Write-Host "   Token length: $tokenLength chars" -ForegroundColor Gray
        Write-Host "   Token (primeros 50): $($json1.accessToken.Substring(0, [Math]::Min(50, $tokenLength)))..." -ForegroundColor Gray
    }
    
    Write-Host ""
    Write-Host "💡 Uso: Apps Desktop/Móvil (copian el token manualmente)" -ForegroundColor Yellow
}
catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# TEST 2: /login (establece cookie)
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
Write-Host "🔹 TEST 2: /api/v1/auth/login" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
Write-Host ""

try {
    $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    
    $response2 = Invoke-WebRequest `
        -Uri "$baseUrl/api/v1/auth/login" `
        -Method Post `
        -ContentType "application/json" `
        -Body $body `
        -WebSession $session `
        -ErrorAction Stop
    
    $json2 = $response2.Content | ConvertFrom-Json
    
    Write-Host "✅ Status: $($response2.StatusCode)" -ForegroundColor Green
    Write-Host "📦 Retorna token en body: $(if ($json2.accessToken) { 'SÍ' } else { 'NO ❌' })" -ForegroundColor White
    Write-Host "🍪 Establece cookies: $(if ($response2.Headers['Set-Cookie']) { 'SÍ ✅' } else { 'NO' })" -ForegroundColor White
    
    if ($response2.Headers['Set-Cookie']) {
        $cookies = $response2.Headers['Set-Cookie']
        Write-Host "   Cookies establecidas:" -ForegroundColor Gray
        foreach ($cookie in $cookies) {
            if ($cookie -match 'access_token') {
                Write-Host "   ✅ access_token (HttpOnly)" -ForegroundColor Green
            }
            if ($cookie -match 'refresh_token') {
                Write-Host "   ✅ refresh_token (HttpOnly)" -ForegroundColor Green
            }
        }
    }
    
    Write-Host ""
    Write-Host "💡 Uso: Navegadores web (Swagger) - cookies automáticas" -ForegroundColor Yellow
    Write-Host ""
    
    # TEST 3: Verificar que la cookie funciona
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
    Write-Host "🔹 TEST 3: Probar cookie con endpoint protegido" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
    Write-Host ""
    
    try {
        $testResponse = Invoke-WebRequest `
            -Uri "$baseUrl/api/v1/catalog/clientes?limit=3&offset=0" `
            -Method Get `
            -WebSession $session `
            -ErrorAction Stop
        
        $testJson = $testResponse.Content | ConvertFrom-Json
        
        Write-Host "✅ Endpoint protegido funciona con cookie!" -ForegroundColor Green
        Write-Host "   Status: $($testResponse.StatusCode)" -ForegroundColor Gray
        Write-Host "   Clientes obtenidos: $($testJson.items.Count)" -ForegroundColor Gray
        
        if ($testJson.items.Count -gt 0) {
            Write-Host "   Ejemplo: $($testJson.items[0].nombre)" -ForegroundColor Gray
        }
    }
    catch {
        Write-Host "❌ Cookie no funcionó: $($_.Exception.Message)" -ForegroundColor Red
    }
}
catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
Write-Host "📋 RESUMEN:" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
Write-Host ""
Write-Host "✅ Para Swagger (navegador):" -ForegroundColor White
Write-Host "   1. Usa POST /api/v1/auth/login" -ForegroundColor Cyan
Write-Host "   2. Las cookies se establecen automáticamente" -ForegroundColor Gray
Write-Host "   3. No necesitas 'Authorize'" -ForegroundColor Gray
Write-Host ""
Write-Host "✅ Para Apps Desktop/Móvil:" -ForegroundColor White
Write-Host "   1. Usa POST /api/v1/auth/login-desktop" -ForegroundColor Cyan
Write-Host "   2. Copia el accessToken del body" -ForegroundColor Gray
Write-Host "   3. Envíalo en header: Authorization: Bearer {token}" -ForegroundColor Gray
Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
