# Test Tags API
# Verifica los nuevos endpoints GET /api/v1/tags

$ErrorActionPreference = "Continue"
$baseUrl = "https://localhost:2502"

# Ignorar certificados SSL
if (-not ([System.Management.Automation.PSTypeName]'ServerCertificateValidationCallback').Type) {
    $certCallback = @"
    using System;
    using System.Net;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;
    public class ServerCertificateValidationCallback {
        public static void Ignore() {
            if(ServicePointManager.ServerCertificateValidationCallback == null) {
                ServicePointManager.ServerCertificateValidationCallback += 
                    delegate(Object obj, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors) {
                        return true;
                    };
            }
        }
    }
"@
    Add-Type $certCallback
}
[ServerCertificateValidationCallback]::Ignore()

Write-Host "================================" -ForegroundColor Cyan
Write-Host "TEST: Tags API Endpoints" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# 1. Login
Write-Host "[1] Login para obtener token..." -ForegroundColor Yellow
try {
    $loginBody = @{
        email = "psantos@global-retail.com"
        password = "12345678"
    } | ConvertTo-Json

    $loginResponse = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/auth/login-desktop" `
        -Method POST `
        -ContentType "application/json" `
        -Body $loginBody

    $token = $loginResponse.accessToken
    $headers = @{
        "Authorization" = "Bearer $token"
        "Content-Type" = "application/json"
    }

    Write-Host "✅ Token obtenido" -ForegroundColor Green
    Write-Host "   Email: $($loginResponse.user.email)" -ForegroundColor Gray
    Write-Host ""
}
catch {
    Write-Host "❌ Error en login: $_" -ForegroundColor Red
    Write-Host "   URI: $baseUrl/api/v1/auth/login-desktop" -ForegroundColor Gray
    exit 1
}

# 2. GET /api/v1/tags (Todos los tags)
Write-Host "[2] GET /api/v1/tags (Todos los tags)" -ForegroundColor Yellow
try {
    $allTags = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/tags" `
        -Method GET `
        -Headers $headers
    
    Write-Host "✅ Tags disponibles: $($allTags.Count)" -ForegroundColor Green
    
    if ($allTags.Count -gt 0) {
        Write-Host "   Primeros 10 tags:" -ForegroundColor White
        $allTags | Select-Object -First 10 | ForEach-Object { Write-Host "     - $_" -ForegroundColor Gray }
    }
    Write-Host ""
} catch {
    Write-Host "❌ Error: $_" -ForegroundColor Red
    Write-Host ""
}

# 3. GET /api/v1/tags?source=freshdesk_api (Filtrar por fuente)
Write-Host "[3] GET /api/v1/tags?source=freshdesk_api" -ForegroundColor Yellow
try {
    $freshdeskTags = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/tags?source=freshdesk_api" `
        -Method GET `
        -Headers $headers
    
    Write-Host "✅ Tags de Freshdesk: $($freshdeskTags.Count)" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "❌ Error: $_" -ForegroundColor Red
    Write-Host ""
}

# 4. GET /api/v1/tags?limit=10 (Limitar resultados)
Write-Host "[4] GET /api/v1/tags?limit=10" -ForegroundColor Yellow
try {
    $limitedTags = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/tags?limit=10" `
        -Method GET `
        -Headers $headers
    
    Write-Host "✅ Tags (primeros 10): $($limitedTags.Count)" -ForegroundColor Green
    $limitedTags | ForEach-Object { Write-Host "   - $_" -ForegroundColor Gray }
    Write-Host ""
} catch {
    Write-Host "❌ Error: $_" -ForegroundColor Red
    Write-Host ""
}

# 5. GET /api/v1/tags/stats (Estadísticas)
Write-Host "[5] GET /api/v1/tags/stats" -ForegroundColor Yellow
try {
    $stats = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/tags/stats" `
        -Method GET `
        -Headers $headers
    
    Write-Host "✅ Estadísticas:" -ForegroundColor Green
    Write-Host "   Total tags: $($stats.total_tags)" -ForegroundColor White
    Write-Host "   Partes con tags: $($stats.partes_con_tags)" -ForegroundColor White
    Write-Host "   Por fuente:" -ForegroundColor White
    $stats.por_fuente | ForEach-Object {
        Write-Host "     - $($_.source): $($_.count)" -ForegroundColor Gray
    }
    Write-Host ""
} catch {
    Write-Host "❌ Error: $_" -ForegroundColor Red
    Write-Host ""
}

Write-Host "================================" -ForegroundColor Cyan
Write-Host "✅ Tests completados" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Cyan

