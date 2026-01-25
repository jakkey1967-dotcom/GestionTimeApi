# 🧪 Script de Pruebas para Freshdesk Integration
# Este script verifica la integración de Freshdesk sin afectar datos

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║     🧪 Test de Integración Freshdesk - Modo Seguro      ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Configuración
$baseUrl = "https://localhost:2502"
$apiUrl = "$baseUrl/api/v1"
$email = "psantos@global-retail.com"
$password = "12345678"

# ✅ Ignorar errores de certificado SSL (para desarrollo local)
add-type @"
    using System.Net;
    using System.Security.Cryptography.X509Certificates;
    public class TrustAllCertsPolicy : ICertificatePolicy {
        public bool CheckValidationResult(
            ServicePoint srvPoint, X509Certificate certificate,
            WebRequest request, int certificateProblem) {
            return true;
        }
    }
"@
[System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12

Write-Host "🔍 Verificando API..." -ForegroundColor Yellow

# Probar HTTPS primero
try {
    $healthCheck = Invoke-WebRequest -Uri "$baseUrl/health" -Method GET -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
    Write-Host "✅ API está corriendo en HTTPS: $baseUrl" -ForegroundColor Green
} catch {
    # Si HTTPS falla, probar HTTP
    Write-Host "⚠️  HTTPS no disponible, probando HTTP..." -ForegroundColor Yellow
    $baseUrl = "http://localhost:2501"
    $apiUrl = "$baseUrl/api/v1"
    
    try {
        $healthCheck = Invoke-WebRequest -Uri "$baseUrl/health" -Method GET -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
        Write-Host "✅ API está corriendo en HTTP: $baseUrl" -ForegroundColor Green
    } catch {
        Write-Host "❌ La API NO está corriendo" -ForegroundColor Red
        Write-Host ""
        Write-Host "💡 Soluciones:" -ForegroundColor Yellow
        Write-Host "   1. Presiona F5 en Visual Studio para iniciar la API" -ForegroundColor White
        Write-Host "   2. Verifica que la API esté escuchando en:" -ForegroundColor White
        Write-Host "      - https://localhost:2502" -ForegroundColor Gray
        Write-Host "      - http://localhost:2501" -ForegroundColor Gray
        Write-Host ""
        exit 1
    }
}

Write-Host ""
Write-Host "🔐 Autenticando con: $email" -ForegroundColor Yellow

try {
    $loginBody = @{
        email = $email
        password = $password
    } | ConvertTo-Json
    
    Write-Host "📤 POST: $apiUrl/auth/login" -ForegroundColor Gray
    
    # IMPORTANTE: SessionVariable captura las cookies automáticamente
    $loginResponse = Invoke-WebRequest -Uri "$apiUrl/auth/login" `
        -Method POST `
        -Body $loginBody `
        -ContentType "application/json; charset=utf-8" `
        -UseBasicParsing `
        -SessionVariable webSession `
        -ErrorAction Stop
    
    Write-Host "✅ Login exitoso - Status: $($loginResponse.StatusCode)" -ForegroundColor Green
    Write-Host "🍪 Usando sesión con cookies (access_token)" -ForegroundColor Green
    
} catch {
    Write-Host "❌ Error en login - StatusCode: $($_.Exception.Response.StatusCode.Value__)" -ForegroundColor Red
    
    if ($_.ErrorDetails.Message) {
        try {
            $errorObj = $_.ErrorDetails.Message | ConvertFrom-Json
            Write-Host "   Detalle: $($errorObj.message)" -ForegroundColor Yellow
        } catch {
            Write-Host "   Detalle: $($_.ErrorDetails.Message)" -ForegroundColor Yellow
        }
    }
    
    # Mostrar ayuda para diagnosticar
    Write-Host ""
    Write-Host "🔍 Para diagnosticar:" -ForegroundColor Cyan
    Write-Host "   1. .\scripts\fix-login.ps1 (recrear usuario)" -ForegroundColor White
    Write-Host "   2. Prueba en Swagger: https://localhost:2502/swagger" -ForegroundColor White
    Write-Host ""
    exit 1
}

Write-Host ""
Write-Host "📡 Probando conexión con Freshdesk..." -ForegroundColor Yellow

try {
    $response = Invoke-WebRequest -Uri "$apiUrl/freshdesk/test-connection" `
        -Method GET `
        -WebSession $webSession `
        -UseBasicParsing `
        -ErrorAction Stop
    
    $result = $response.Content | ConvertFrom-Json
    
    if ($result.success) {
        Write-Host "✅ $($result.message)" -ForegroundColor Green
        if ($result.agentId) {
            Write-Host "   🆔 Agent ID: $($result.agentId)" -ForegroundColor Cyan
            Write-Host "   📧 Email: $($result.email)" -ForegroundColor Cyan
        }
    } else {
        Write-Host "⚠️  $($result.message)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ Error conectando con Freshdesk" -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        $errorObj = $_.ErrorDetails.Message | ConvertFrom-Json
        Write-Host "   Detalle: $($errorObj.message)" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "🎫 Probando búsqueda de tickets..." -ForegroundColor Yellow

try {
    $response = Invoke-WebRequest -Uri "$apiUrl/freshdesk/tickets/suggest?limit=5" `
        -Method GET `
        -WebSession $webSession `
        -UseBasicParsing `
        -ErrorAction Stop
    
    $result = $response.Content | ConvertFrom-Json
    
    Write-Host "✅ Búsqueda exitosa: $($result.count) tickets encontrados" -ForegroundColor Green
    
    if ($result.count -gt 0) {
        Write-Host ""
        Write-Host "   📋 Primeros tickets:" -ForegroundColor Cyan
        foreach ($ticket in $result.tickets | Select-Object -First 3) {
            Write-Host "      • ID: $($ticket.id) - $($ticket.subject)" -ForegroundColor White
        }
    }
} catch {
    Write-Host "⚠️  Error al buscar tickets" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "🏷️  Probando búsqueda de tags..." -ForegroundColor Yellow

try {
    $response = Invoke-WebRequest -Uri "$apiUrl/freshdesk/tags/suggest?limit=10" `
        -Method GET `
        -WebSession $webSession `
        -UseBasicParsing `
        -ErrorAction Stop
    
    $result = $response.Content | ConvertFrom-Json
    
    Write-Host "✅ Búsqueda de tags exitosa: $($result.count) tags encontrados" -ForegroundColor Green
    
    if ($result.count -gt 0) {
        Write-Host ""
        Write-Host "   🏷️  Tags disponibles:" -ForegroundColor Cyan
        foreach ($tag in $result.tags | Select-Object -First 5) {
            Write-Host "      • $tag" -ForegroundColor White
        }
    } else {
        Write-Host "   ℹ️  No hay tags en caché. Ejecuta la sincronización primero." -ForegroundColor Yellow
    }
} catch {
    Write-Host "⚠️  Error al buscar tags" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║            🎉 PRUEBAS COMPLETADAS 🎉                     ║" -ForegroundColor Green
Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""
