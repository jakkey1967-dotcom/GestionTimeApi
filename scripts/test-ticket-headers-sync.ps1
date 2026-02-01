# Test de sincronización de ticket headers con diagnóstico completo
# Para: psantos@global-retail.com

$ErrorActionPreference = "Continue"
$baseUrl = "https://localhost:2502"

# Ignorar certificados SSL (compatible con PowerShell 5.1 y 7+)
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

Write-Host "🔐 TEST: Sincronización Ticket Headers" -ForegroundColor Cyan
Write-Host "=" * 60

# 1. LOGIN
Write-Host "`n📝 Paso 1: Login..." -ForegroundColor Yellow
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
    Write-Host "✅ Login exitoso" -ForegroundColor Green
    Write-Host "   Email: $($loginResponse.user.email)" -ForegroundColor Gray
    Write-Host "   Role: $($loginResponse.user.role)" -ForegroundColor Gray
    Write-Host "   Token: $($token.Substring(0, 20))..." -ForegroundColor Gray
    
    # Verificar si es Admin
    if ($loginResponse.user.role -ne "Admin" -and $loginResponse.user.role -ne "ADMIN") {
        Write-Host "⚠️  ADVERTENCIA: Usuario NO es Admin (role: $($loginResponse.user.role))" -ForegroundColor Yellow
        Write-Host "   Este endpoint requiere rol Admin" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "❌ Error en login:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host "Detalles:" -ForegroundColor Red
        Write-Host $_.ErrorDetails.Message -ForegroundColor Red
    }
    exit 1
}

# 2. SYNC TICKET HEADERS (INCREMENTAL)
Write-Host "`n📊 Paso 2: Sincronizar ticket headers (modo incremental)..." -ForegroundColor Yellow
try {
    $headers = @{
        "Authorization" = "Bearer $token"
        "Accept" = "*/*"
    }
    
    Write-Host "URL: $baseUrl/api/v1/integrations/freshdesk/sync/ticket-headers?full=false" -ForegroundColor Gray
    
    $syncResponse = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/integrations/freshdesk/sync/ticket-headers?full=false" `
        -Method POST `
        -Headers $headers `
        -TimeoutSec 120
    
    Write-Host "✅ Sincronización completada" -ForegroundColor Green
    Write-Host ($syncResponse | ConvertTo-Json -Depth 5) -ForegroundColor Gray
}
catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Host "❌ Error en sincronización (HTTP $statusCode):" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    
    if ($_.ErrorDetails.Message) {
        Write-Host "`nDetalles del error:" -ForegroundColor Red
        try {
            $errorJson = $_.ErrorDetails.Message | ConvertFrom-Json
            Write-Host ($errorJson | ConvertTo-Json -Depth 5) -ForegroundColor Red
        }
        catch {
            Write-Host $_.ErrorDetails.Message -ForegroundColor Red
        }
    }
    
    # Diagnóstico según código de error
    switch ($statusCode) {
        401 { 
            Write-Host "`n💡 DIAGNÓSTICO: Token inválido o expirado" -ForegroundColor Yellow
            Write-Host "   - Verificar que el token sea correcto" -ForegroundColor Yellow
        }
        403 { 
            Write-Host "`n💡 DIAGNÓSTICO: Usuario sin permisos (no es Admin)" -ForegroundColor Yellow
            Write-Host "   - Verificar rol del usuario en BD" -ForegroundColor Yellow
            Write-Host "   - Rol actual: $userRole" -ForegroundColor Yellow
        }
        404 {
            Write-Host "`n💡 DIAGNÓSTICO: Endpoint no encontrado" -ForegroundColor Yellow
            Write-Host "   - Verificar que la API esté corriendo" -ForegroundColor Yellow
            Write-Host "   - Verificar la ruta del endpoint" -ForegroundColor Yellow
        }
        500 {
            Write-Host "`n💡 DIAGNÓSTICO: Error interno del servidor" -ForegroundColor Yellow
            Write-Host "   - Revisar logs de la API" -ForegroundColor Yellow
            Write-Host "   - Verificar configuración de Freshdesk" -ForegroundColor Yellow
            Write-Host "   - Verificar conexión a base de datos" -ForegroundColor Yellow
        }
        default {
            Write-Host "`n💡 DIAGNÓSTICO: Error HTTP $statusCode" -ForegroundColor Yellow
        }
    }
    
    exit 1
}

# 3. VERIFICAR ESTADO
Write-Host "`n📈 Paso 3: Verificar estado de sincronización..." -ForegroundColor Yellow
try {
    $statusResponse = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/integrations/freshdesk/sync/status" `
        -Method GET `
        -Headers $headers
    
    Write-Host "✅ Estado obtenido:" -ForegroundColor Green
    Write-Host ($statusResponse | ConvertTo-Json -Depth 5) -ForegroundColor Gray
}
catch {
    Write-Host "⚠️  No se pudo obtener el estado" -ForegroundColor Yellow
    Write-Host $_.Exception.Message -ForegroundColor Yellow
}

Write-Host "`n" + ("=" * 60)
Write-Host "✅ Test completado" -ForegroundColor Green
