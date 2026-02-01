# Test de sincronización del Agente Actual de Freshdesk (Agent Me)
Write-Host "╔══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "║ TEST: SINCRONIZACIÓN DE AGENT ME" -ForegroundColor Cyan
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

Write-Host "`n🔐 [1/3] Login..." -ForegroundColor Cyan
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

Write-Host "`n🔄 [2/3] Ejecutar sincronización del agente actual (me)..." -ForegroundColor Cyan

$syncStart = Get-Date

try {
    $syncResponse = Invoke-RestMethod `
        -Uri "$baseUrl/integrations/freshdesk/agent-me/sync" `
        -Method POST `
        -Headers $headers
    
    $syncEnd = Get-Date
    $syncDuration = ($syncEnd - $syncStart).TotalSeconds
    
    Write-Host "`n✅ Sincronización completada en $([math]::Round($syncDuration, 2))s" -ForegroundColor Green
    Write-Host ""
    Write-Host "Resultados:" -ForegroundColor Cyan
    Write-Host "   Success: $($syncResponse.success)" -ForegroundColor $(if ($syncResponse.success) { "Green" } else { "Red" })
    Write-Host "   Agent ID: $($syncResponse.agent_id)" -ForegroundColor White
    Write-Host "   Agent Email: $($syncResponse.agent_email)" -ForegroundColor White
    Write-Host "   Freshdesk Updated: $($syncResponse.freshdesk_updated_at)" -ForegroundColor White
    Write-Host "   Synced At: $($syncResponse.synced_at)" -ForegroundColor White
    Write-Host "   Duración (ms): $($syncResponse.durationMs)" -ForegroundColor White
    
} catch {
    Write-Host "❌ Error en sincronización: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response: $responseBody" -ForegroundColor Red
    }
    exit 1
}

Write-Host "`n👤 [3/3] Obtener agente actual desde cache..." -ForegroundColor Cyan
try {
    $agentResponse = Invoke-RestMethod `
        -Uri "$baseUrl/integrations/freshdesk/agent-me" `
        -Method GET `
        -Headers $headers
    
    Write-Host "✅ Agente obtenido desde cache" -ForegroundColor Green
    Write-Host ""
    Write-Host "Información del agente:" -ForegroundColor Yellow
    Write-Host "   Agent ID: $($agentResponse.agent.agent_id)" -ForegroundColor White
    Write-Host "   Email: $($agentResponse.agent.agent_email)" -ForegroundColor White
    Write-Host "   Nombre: $($agentResponse.agent.agent_name)" -ForegroundColor White
    Write-Host "   Tipo: $($agentResponse.agent.agent_type)" -ForegroundColor White
    Write-Host "   Activo: $($agentResponse.agent.is_active)" -ForegroundColor White
    Write-Host "   Idioma: $($agentResponse.agent.language)" -ForegroundColor White
    Write-Host "   Zona horaria: $($agentResponse.agent.time_zone)" -ForegroundColor White
    Write-Host "   Móvil: $($agentResponse.agent.mobile)" -ForegroundColor White
    Write-Host "   Teléfono: $($agentResponse.agent.phone)" -ForegroundColor White
    Write-Host "   Último login: $($agentResponse.agent.last_login_at)" -ForegroundColor White
    Write-Host "   Creado en Freshdesk: $($agentResponse.agent.freshdesk_created_at)" -ForegroundColor White
    Write-Host "   Actualizado en Freshdesk: $($agentResponse.agent.freshdesk_updated_at)" -ForegroundColor White
    Write-Host "   Sincronizado: $($agentResponse.agent.synced_at)" -ForegroundColor White
    
} catch {
    Write-Host "❌ Error al obtener agente desde cache: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n╔══════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "║ TEST COMPLETADO" -ForegroundColor Green
Write-Host "╚══════════════════════════════════════════════════════════════" -ForegroundColor Green

Write-Host "`n💡 Consulta SQL para verificar datos:" -ForegroundColor Yellow
Write-Host "   SELECT * FROM pss_dvnx.freshdesk_agent_me_cache;" -ForegroundColor White
Write-Host "   SELECT agent_id, agent_email, agent_name, synced_at FROM pss_dvnx.freshdesk_agent_me_cache;" -ForegroundColor White

Write-Host "`n✅ Test completado exitosamente" -ForegroundColor Green
