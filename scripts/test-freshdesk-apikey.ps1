# Test de API Key de Freshdesk
Write-Host "🔍 Verificando API Key de Freshdesk..." -ForegroundColor Cyan

# Configuración
$baseUrl = "https://localhost:2502/api/v1"

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

Write-Host "`n1️⃣ Test: Ping a Freshdesk (sin autenticación de GestionTime)" -ForegroundColor Yellow
try {
    $pingResponse = Invoke-RestMethod -Uri "$baseUrl/freshdesk/ping" -Method GET
    Write-Host "✅ Ping exitoso:" -ForegroundColor Green
    Write-Host ($pingResponse | ConvertTo-Json -Depth 5) -ForegroundColor White
} catch {
    Write-Host "❌ Ping falló: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response: $responseBody" -ForegroundColor Red
    }
}

Write-Host "`n2️⃣ Test directo a Freshdesk API con la API Key" -ForegroundColor Yellow
try {
    $apiKey = "9i1AtT08nkY1B1BmjtLk"
    $base64Auth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("${apiKey}:X"))
    
    $headers = @{
        "Authorization" = "Basic $base64Auth"
        "Content-Type" = "application/json"
    }
    
    Write-Host "Auth Header generado: Basic ${base64Auth}" -ForegroundColor Gray
    
    # Test 1: /api/v2/agents/me (debería funcionar)
    Write-Host "`n  a) GET /api/v2/agents/me" -ForegroundColor Cyan
    $agentResponse = Invoke-RestMethod -Uri "https://alterasoftware.freshdesk.com/api/v2/agents/me" -Method GET -Headers $headers
    Write-Host "  ✅ OK: $($agentResponse.email)" -ForegroundColor Green
    
    # Test 2: /api/v2/tickets (el que está fallando)
    Write-Host "`n  b) GET /api/v2/tickets?per_page=5&page=1" -ForegroundColor Cyan
    $ticketsResponse = Invoke-RestMethod -Uri "https://alterasoftware.freshdesk.com/api/v2/tickets?per_page=5&page=1" -Method GET -Headers $headers
    Write-Host "  ✅ OK: Obtenidos $($ticketsResponse.Count) tickets" -ForegroundColor Green
    Write-Host "  Primer ticket: ID=$($ticketsResponse[0].id), Subject=$($ticketsResponse[0].subject)" -ForegroundColor White
    
} catch {
    Write-Host "  ❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "  Status: $statusCode" -ForegroundColor Red
        Write-Host "  Response: $responseBody" -ForegroundColor Red
    }
}

Write-Host "`n✅ Test completado" -ForegroundColor Green
