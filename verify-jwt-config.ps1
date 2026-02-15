#!/usr/bin/env pwsh
# verify-jwt-config.ps1
# Verifica la configuración JWT y prueba con cookies

$ErrorActionPreference = "Stop"
$baseUrl = "http://localhost:2501"
$email = "psantos@global-retail.com"
$password = "12345678"

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host " 🔧 VERIFICACIÓN JWT: Configuración del servidor" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# ============================================================================
# 1. PROBAR CON COOKIES (COMO NAVEGADOR)
# ============================================================================

Write-Host "🍪 Paso 1: Probando con COOKIES (método /login)" -ForegroundColor Yellow
Write-Host ""

$loginBody = @{
    email = $email
    password = $password
} | ConvertTo-Json

$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

$loginResponse = Invoke-WebRequest `
    -Uri "$baseUrl/api/v1/auth/login" `
    -Method Post `
    -ContentType "application/json" `
    -Body $loginBody `
    -WebSession $session

Write-Host "✅ Login con cookies exitoso" -ForegroundColor Green
Write-Host "   Cookies establecidas: $($loginResponse.Headers['Set-Cookie'].Count)" -ForegroundColor Gray
Write-Host ""

Write-Host "Probando endpoint v2 CON COOKIES..." -ForegroundColor Gray
try {
    $response1 = Invoke-RestMethod `
        -Uri "$baseUrl/api/v2/informes/partes?date=2026-02-14&pageSize=5" `
        -Method Get `
        -WebSession $session
    
    Write-Host "✅ FUNCIONA con cookies!" -ForegroundColor Green
    Write-Host "   Total registros: $($response1.total)" -ForegroundColor Green
    Write-Host "   Items: $($response1.items.Count)" -ForegroundColor Green
}
catch {
    Write-Host "❌ También falló con cookies" -ForegroundColor Red
    Write-Host "   Status: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
}

Write-Host ""

# ============================================================================
# 2. PROBAR CON TOKEN EN HEADER (COMO APP DESKTOP)
# ============================================================================

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
Write-Host "🔑 Paso 2: Probando con TOKEN en header (método /login-desktop)" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
Write-Host ""

$loginResponse2 = Invoke-RestMethod `
    -Uri "$baseUrl/api/v1/auth/login-desktop" `
    -Method Post `
    -ContentType "application/json" `
    -Body $loginBody

$token = $loginResponse2.accessToken

Write-Host "✅ Token obtenido" -ForegroundColor Green
Write-Host "   Token length: $($token.Length) chars" -ForegroundColor Gray
Write-Host ""

Write-Host "Probando endpoint v2 CON HEADER Authorization..." -ForegroundColor Gray
$headers = @{
    "Authorization" = "Bearer $token"
}

try {
    $response2 = Invoke-RestMethod `
        -Uri "$baseUrl/api/v2/informes/partes?date=2026-02-14&pageSize=5" `
        -Method Get `
        -Headers $headers
    
    Write-Host "✅ FUNCIONA con header Authorization!" -ForegroundColor Green
    Write-Host "   Total registros: $($response2.total)" -ForegroundColor Green
}
catch {
    Write-Host "❌ FALLA con header Authorization" -ForegroundColor Red
    Write-Host "   Status: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
}

Write-Host ""

# ============================================================================
# 3. CONCLUSIÓN
# ============================================================================

Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
Write-Host " 📊 DIAGNÓSTICO FINAL" -ForegroundColor Cyan
Write-Host " ────────────────────────────────────────────────────────────────" -ForegroundColor Gray
Write-Host ""
Write-Host "   ✅ Cookies (navegador): Ver resultado arriba" -ForegroundColor Yellow
Write-Host "   ❌ Header Authorization (apps): Ver resultado arriba" -ForegroundColor Yellow
Write-Host ""
Write-Host " 🔧 PROBLEMA IDENTIFICADO:" -ForegroundColor Red
Write-Host ""
Write-Host "   La API NO está configurada para leer JWT desde el header" -ForegroundColor White
Write-Host "   'Authorization: Bearer {token}'" -ForegroundColor White
Write-Host ""
Write-Host "   Posible causa en Program.cs:" -ForegroundColor Yellow
Write-Host "   - OnMessageReceived solo lee desde cookies" -ForegroundColor Yellow
Write-Host "   - Falta leer desde Request.Headers['Authorization']" -ForegroundColor Yellow
Write-Host ""
Write-Host " 💡 SOLUCIÓN:" -ForegroundColor Cyan
Write-Host ""
Write-Host "   Modificar Program.cs para leer JWT desde:" -ForegroundColor White
Write-Host "   1. Header Authorization: Bearer {token} (PRIORIDAD)" -ForegroundColor Green
Write-Host "   2. Cookie access_token (FALLBACK)" -ForegroundColor Yellow
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
