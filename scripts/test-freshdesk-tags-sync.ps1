# Test de sincronización manual de tags de Freshdesk
# Endpoint: POST /api/v1/integrations/freshdesk/sync/tags

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

Write-Host "🔄 TEST: Sincronización Manual de Tags de Freshdesk" -ForegroundColor Cyan
Write-Host "=" * 70

# Credenciales de ADMIN
$adminEmail = "psantos@global-retail.com"
$adminPassword = "12345678"

Write-Host "`n📝 Paso 1: Login como ADMIN..." -ForegroundColor Yellow
try {
    $loginBody = @{
        email = $adminEmail
        password = $adminPassword
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
    
    Write-Host "✅ Login exitoso" -ForegroundColor Green
    Write-Host "   Email: $($loginResponse.user.email)" -ForegroundColor Gray
    Write-Host "   Role: $($loginResponse.user.role)" -ForegroundColor Gray
}
catch {
    Write-Host "❌ Error en login:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

Write-Host "`n🔄 Paso 2: Ejecutar sincronización de tags..." -ForegroundColor Yellow
try {
    $syncStartTime = Get-Date
    
    $syncResponse = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/integrations/freshdesk/sync/tags" `
        -Method POST `
        -Headers $headers
    
    $syncEndTime = Get-Date
    $duration = ($syncEndTime - $syncStartTime).TotalMilliseconds
    
    Write-Host "✅ Sincronización completada en $([math]::Round($duration, 2)) ms" -ForegroundColor Green
    Write-Host ""
    Write-Host "📊 RESULTADOS:" -ForegroundColor Cyan
    Write-Host "   Success: $($syncResponse.success)" -ForegroundColor $(if ($syncResponse.success) { "Green" } else { "Red" })
    Write-Host "   Message: $($syncResponse.message)" -ForegroundColor Gray
    Write-Host "   Rows Affected: $($syncResponse.rowsAffected)" -ForegroundColor Yellow
    Write-Host "   Total Tags: $($syncResponse.totalTags)" -ForegroundColor Yellow
    Write-Host "   Synced At: $($syncResponse.syncedAt)" -ForegroundColor Gray
}
catch {
    Write-Host "❌ Error en sincronización:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    
    if ($_.ErrorDetails.Message) {
        Write-Host "`nDetalles del error:" -ForegroundColor Red
        Write-Host $_.ErrorDetails.Message -ForegroundColor Red
    }
}

Write-Host "`n" + ("=" * 70)
Write-Host "✅ Test completado" -ForegroundColor Green

Write-Host "`n💡 NOTAS:" -ForegroundColor Cyan
Write-Host "   - Este endpoint requiere rol ADMIN" -ForegroundColor Gray
Write-Host "   - Ejecuta UPSERT en pss_dvnx.freshdesk_tags" -ForegroundColor Gray
Write-Host "   - Fuente de datos: pss_dvnx.v_freshdesk_ticket_full" -ForegroundColor Gray
Write-Host "   - Tags se normalizan (lower/trim, max 100 chars)" -ForegroundColor Gray
Write-Host "   - Evita duplicados por clave 'name'" -ForegroundColor Gray
Write-Host "   - Actualiza last_seen_at solo si es más reciente" -ForegroundColor Gray

Write-Host "`n📚 ENDPOINTS DEPRECADOS (retornan 410 Gone):" -ForegroundColor Yellow
Write-Host "   ❌ GET  /api/v1/freshdesk/tags/suggest" -ForegroundColor DarkGray
Write-Host "   ❌ POST /api/v1/freshdesk/tags/sync" -ForegroundColor DarkGray
Write-Host ""
Write-Host "📍 NUEVO ENDPOINT:" -ForegroundColor Green
Write-Host "   ✅ POST /api/v1/integrations/freshdesk/sync/tags" -ForegroundColor Green
