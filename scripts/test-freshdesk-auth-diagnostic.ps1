# Diagnóstico completo de autenticación Freshdesk
Write-Host "╔══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "║ DIAGNÓSTICO DE AUTENTICACIÓN FRESHDESK" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════════" -ForegroundColor Cyan

# 1. Verificar User Secrets
Write-Host "`n📋 [1/5] Verificando User Secrets..." -ForegroundColor Yellow
try {
    $secrets = dotnet user-secrets list 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ User Secrets encontrados:" -ForegroundColor Green
        Write-Host $secrets -ForegroundColor White
    } else {
        Write-Host "❌ Error al leer User Secrets" -ForegroundColor Red
        Write-Host $secrets -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}

# 2. Probar diferentes formatos de autenticación
Write-Host "`n🔐 [2/5] Probando diferentes formatos de Auth..." -ForegroundColor Yellow

$apiKey = "9i1AtT08nkY1B1BmjtLk"
Write-Host "API Key original: '$apiKey'" -ForegroundColor Gray
Write-Host "Longitud: $($apiKey.Length) caracteres" -ForegroundColor Gray

# Verificar si hay espacios o caracteres ocultos
$trimmedKey = $apiKey.Trim()
if ($trimmedKey -ne $apiKey) {
    Write-Host "⚠️  ADVERTENCIA: La API Key tiene espacios!" -ForegroundColor Yellow
    Write-Host "   Original length: $($apiKey.Length)" -ForegroundColor Red
    Write-Host "   Trimmed length: $($trimmedKey.Length)" -ForegroundColor Green
    $apiKey = $trimmedKey
}

# Formato 1: API_KEY:X (estándar Freshdesk)
$auth1 = "${apiKey}:X"
$base64_1 = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($auth1))
Write-Host "`nFormato 1 (API_KEY:X):" -ForegroundColor Cyan
Write-Host "   String: '$auth1'" -ForegroundColor White
Write-Host "   Base64: $base64_1" -ForegroundColor White

# Formato 2: API_KEY:x (minúscula)
$auth2 = "${apiKey}:x"
$base64_2 = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($auth2))
Write-Host "`nFormato 2 (API_KEY:x):" -ForegroundColor Cyan
Write-Host "   String: '$auth2'" -ForegroundColor White
Write-Host "   Base64: $base64_2" -ForegroundColor White

# Formato 3: Solo API_KEY (sin :X)
$auth3 = $apiKey
$base64_3 = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($auth3))
Write-Host "`nFormato 3 (solo API_KEY):" -ForegroundColor Cyan
Write-Host "   String: '$auth3'" -ForegroundColor White
Write-Host "   Base64: $base64_3" -ForegroundColor White

# 3. Probar cada formato contra Freshdesk
Write-Host "`n🧪 [3/5] Probando cada formato contra Freshdesk..." -ForegroundColor Yellow

$formats = @(
    @{ Name = "Formato 1 (API_KEY:X)"; Auth = $base64_1 },
    @{ Name = "Formato 2 (API_KEY:x)"; Auth = $base64_2 },
    @{ Name = "Formato 3 (solo API_KEY)"; Auth = $base64_3 }
)

$testUrl = "https://alterasoftware.freshdesk.com/api/v2/tickets?per_page=1"

foreach ($format in $formats) {
    Write-Host "`nProbando: $($format.Name)" -ForegroundColor Cyan
    
    $headers = @{
        "Authorization" = "Basic $($format.Auth)"
        "Content-Type" = "application/json"
    }
    
    try {
        $response = Invoke-WebRequest -Uri $testUrl -Method GET -Headers $headers -ErrorAction Stop
        Write-Host "   ✅ SUCCESS ($($response.StatusCode))" -ForegroundColor Green
        
        # Parsear y mostrar primer ticket
        $tickets = $response.Content | ConvertFrom-Json
        if ($tickets.Count -gt 0) {
            Write-Host "   Primer ticket:" -ForegroundColor Gray
            Write-Host "      ID: $($tickets[0].id)" -ForegroundColor White
            Write-Host "      Subject: $($tickets[0].subject)" -ForegroundColor White
        }
        
        Write-Host "`n   ✨ Este formato FUNCIONA! Usar este." -ForegroundColor Green
        break
        
    } catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        Write-Host "   ❌ FAILED ($statusCode)" -ForegroundColor Red
        
        if ($_.Exception.Response) {
            try {
                $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
                $errorBody = $reader.ReadToEnd()
                Write-Host "   Error: $errorBody" -ForegroundColor Red
            } catch {}
        }
    }
}

# 4. Verificar configuración de la aplicación
Write-Host "`n⚙️  [4/5] Verificando configuración de la aplicación..." -ForegroundColor Yellow

# Leer appsettings.json
if (Test-Path "appsettings.json") {
    $appsettings = Get-Content "appsettings.json" | ConvertFrom-Json
    Write-Host "Freshdesk en appsettings.json:" -ForegroundColor Cyan
    Write-Host "   Domain: $($appsettings.Freshdesk.Domain)" -ForegroundColor White
    Write-Host "   ApiKey: $($appsettings.Freshdesk.ApiKey)" -ForegroundColor White
    Write-Host "   SyncEnabled: $($appsettings.Freshdesk.SyncEnabled)" -ForegroundColor White
    Write-Host "   PerPage: $($appsettings.Freshdesk.PerPage)" -ForegroundColor White
}

# 5. Probar endpoint local
Write-Host "`n🌐 [5/5] Probando endpoint local /ping..." -ForegroundColor Yellow

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

try {
    $pingResponse = Invoke-RestMethod -Uri "https://localhost:2502/api/v1/freshdesk/ping" -Method GET
    Write-Host "Response del ping local:" -ForegroundColor Cyan
    Write-Host ($pingResponse | ConvertTo-Json -Depth 3) -ForegroundColor White
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n╔══════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "║ DIAGNÓSTICO COMPLETADO" -ForegroundColor Green
Write-Host "╚══════════════════════════════════════════════════════════════" -ForegroundColor Green

Write-Host "`n💡 Recomendaciones:" -ForegroundColor Yellow
Write-Host "   1. Si algún formato funcionó, actualiza el código para usarlo" -ForegroundColor White
Write-Host "   2. Verifica que la API Key en User Secrets coincida con la probada" -ForegroundColor White
Write-Host "   3. Asegúrate de que no hay espacios o caracteres ocultos" -ForegroundColor White
Write-Host "   4. Reinicia la aplicación después de cambiar User Secrets" -ForegroundColor White
