# Test de sincronización de Freshdesk Companies
Write-Host "╔══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "║ TEST: SINCRONIZACIÓN DE FRESHDESK COMPANIES" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════════" -ForegroundColor Cyan

# Configuración
$baseUrl = "https://localhost:2502/api/v1"
$EMAIL = "psantos@global-retail.com"
$PASSWORD = "12345678"

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

Write-Host "`n🔐 [1/4] Login..." -ForegroundColor Cyan
$loginBody = @{
    Email = $EMAIL
    Password = $PASSWORD
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod `
        -Uri "$baseUrl/auth/login-desktop" `
        -Method POST `
        -ContentType "application/json" `
        -Body $loginBody
    
    $accessToken = $loginResponse.accessToken
    $headers = @{ "Authorization" = "Bearer $accessToken" }
    
    Write-Host "✅ Login exitoso" -ForegroundColor Green
} catch {
    Write-Host "❌ Login falló: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "`n📊 [2/4] Consultar estado ANTES de sincronización..." -ForegroundColor Cyan
try {
    $statusBefore = Invoke-RestMethod `
        -Uri "$baseUrl/integrations/freshdesk/sync/companies/status" `
        -Method GET `
        -Headers $headers
    
    Write-Host "Estado actual:" -ForegroundColor Yellow
    Write-Host "   Total companies: $($statusBefore.totalCompanies)" -ForegroundColor White
    Write-Host "   Max updated_at: $($statusBefore.maxUpdatedAt)" -ForegroundColor White
    Write-Host "   Max synced_at: $($statusBefore.maxSyncedAt)" -ForegroundColor White
} catch {
    Write-Host "⚠️  Error al obtener estado (probablemente tabla no existe aún)" -ForegroundColor Yellow
}

Write-Host "`n🔄 [3/4] Ejecutar sincronización de companies..." -ForegroundColor Cyan

$syncStart = Get-Date

try {
    $syncResponse = Invoke-RestMethod `
        -Uri "$baseUrl/integrations/freshdesk/sync/companies" `
        -Method POST `
        -Headers $headers
    
    $syncEnd = Get-Date
    $syncDuration = ($syncEnd - $syncStart).TotalSeconds
    
    Write-Host "`n✅ Sincronización completada en $([math]::Round($syncDuration, 2))s" -ForegroundColor Green
    Write-Host ""
    Write-Host "Resultados:" -ForegroundColor Cyan
    Write-Host "   Success: $($syncResponse.success)" -ForegroundColor $(if ($syncResponse.success) { "Green" } else { "Red" })
    Write-Host "   Message: $($syncResponse.message)" -ForegroundColor White
    Write-Host ""
    Write-Host "Métricas:" -ForegroundColor Yellow
    Write-Host "   Páginas obtenidas: $($syncResponse.pagesFetched)" -ForegroundColor White
    Write-Host "   Companies upserted: $($syncResponse.companiesUpserted)" -ForegroundColor White
    Write-Host "   Duración (ms): $($syncResponse.durationMs)" -ForegroundColor White
    Write-Host ""
    
    if ($syncResponse.sampleFirst3 -and $syncResponse.sampleFirst3.Count -gt 0) {
        Write-Host "Primeras 3 companies sincronizadas:" -ForegroundColor Yellow
        foreach ($sample in $syncResponse.sampleFirst3) {
            Write-Host "   - ID: $($sample.company_id) | Nombre: $($sample.name)" -ForegroundColor White
        }
        Write-Host ""
    }
    
    Write-Host "Timestamps:" -ForegroundColor Yellow
    Write-Host "   Inicio: $($syncResponse.startedAt)" -ForegroundColor White
    Write-Host "   Fin: $($syncResponse.completedAt)" -ForegroundColor White
    
} catch {
    Write-Host "❌ Error en sincronización: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response: $responseBody" -ForegroundColor Red
    }
    exit 1
}

Write-Host "`n📊 [4/4] Consultar estado DESPUÉS de sincronización..." -ForegroundColor Cyan
try {
    $statusAfter = Invoke-RestMethod `
        -Uri "$baseUrl/integrations/freshdesk/sync/companies/status" `
        -Method GET `
        -Headers $headers
    
    Write-Host "Estado actualizado:" -ForegroundColor Yellow
    Write-Host "   Total companies: $($statusAfter.totalCompanies)" -ForegroundColor White
    Write-Host "   Max updated_at: $($statusAfter.maxUpdatedAt)" -ForegroundColor White
    Write-Host "   Max synced_at: $($statusAfter.maxSyncedAt)" -ForegroundColor White
} catch {
    Write-Host "❌ Error al obtener estado: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n╔══════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "║ COMPARACIÓN ANTES/DESPUÉS" -ForegroundColor Green
Write-Host "╚══════════════════════════════════════════════════════════════" -ForegroundColor Green

if ($statusBefore -and $statusAfter) {
    Write-Host "`nCompanies sincronizadas:" -ForegroundColor Cyan
    Write-Host "   Antes:  $($statusBefore.totalCompanies)" -ForegroundColor White
    Write-Host "   Ahora:  $($statusAfter.totalCompanies)" -ForegroundColor White
    Write-Host "   Nuevas: $($statusAfter.totalCompanies - $statusBefore.totalCompanies)" -ForegroundColor Yellow
}

Write-Host "`n💡 Consulta SQL para verificar datos:" -ForegroundColor Yellow
Write-Host "   SELECT company_id, name, industry, account_tier FROM pss_dvnx.freshdesk_companies_cache LIMIT 10;" -ForegroundColor White
Write-Host "   SELECT COUNT(*) FROM pss_dvnx.freshdesk_companies_cache;" -ForegroundColor White

Write-Host "`n✅ Test completado exitosamente" -ForegroundColor Green
