# Test crear parte con tags
$baseUrl = "https://localhost:2502"

# Ignorar errores SSL
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

Write-Host "=== TEST: CREAR PARTE CON TAGS ===" -ForegroundColor Cyan

# 1. Login
Write-Host "`n1. Login..." -ForegroundColor Yellow
try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/v1/auth/login" `
        -Method Post `
        -ContentType "application/json" `
        -Body '{"email":"psantos@global-retail.com","password":"12345678"}'
    
    $token = $loginResponse.accessToken
    Write-Host "   ✅ Login exitoso" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Error en login: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# 2. Crear parte con tags
Write-Host "`n2. Crear parte con tags 'tpv', 'hardware', 'urgente'..." -ForegroundColor Yellow

$parteData = @{
    fecha_trabajo = "2026-01-25"
    hora_inicio = "09:00"
    hora_fin = "11:30"
    id_cliente = 1
    tienda = "Madrid Centro"
    id_grupo = 1
    id_tipo = 1
    accion = "Reparación de TPV - Error al procesar cobros con tarjeta"
    ticket = "55950"
    tags = @("tpv", "hardware", "urgente")
}

$headers = @{
    Authorization = "Bearer $token"
    "Content-Type" = "application/json"
}

try {
    $createResponse = Invoke-RestMethod -Uri "$baseUrl/api/v1/partes" `
        -Method Post `
        -Headers $headers `
        -Body ($parteData | ConvertTo-Json)
    
    Write-Host "   ✅ Parte creado: ID=$($createResponse.id)" -ForegroundColor Green
    $parteId = $createResponse.id
    
    # 3. Verificar que tiene tags
    Write-Host "`n3. Verificando tags del parte..." -ForegroundColor Yellow
    $listResponse = Invoke-RestMethod -Uri "$baseUrl/api/v1/partes?fecha=2026-01-25" `
        -Method Get `
        -Headers $headers
    
    $parte = $listResponse | Where-Object { $_.id -eq $parteId } | Select-Object -First 1
    
    if ($parte) {
        Write-Host "   ✅ Parte encontrado en listado" -ForegroundColor Green
        Write-Host "   📋 Acción: $($parte.accion)" -ForegroundColor White
        Write-Host "   🏷️  Tags: $($parte.tags -join ', ')" -ForegroundColor Cyan
        
        if ($parte.tags -contains 'tpv') {
            Write-Host "`n🎉 ¡TODO FUNCIONANDO! El sistema de tags está operativo." -ForegroundColor Green
        } else {
            Write-Host "`n⚠️  Parte creado pero sin tags" -ForegroundColor Yellow
        }
    } else {
        Write-Host "   ⚠️  Parte no encontrado en el listado" -ForegroundColor Yellow
    }
    
    # 4. Ver tags en freshdesk_tags
    Write-Host "`n4. Verificando tabla freshdesk_tags..." -ForegroundColor Yellow
    $env:PGPASSWORD="postgres"
    $sqlQuery = "SELECT name, source, last_seen_at FROM pss_dvnx.freshdesk_tags WHERE name IN ('tpv', 'hardware', 'urgente') ORDER BY name;"
    $pgResult = psql -h localhost -p 5434 -U postgres -d pss_dvnx -t -c $sqlQuery 2>$null
    
    if ($pgResult) {
        Write-Host "   ✅ Tags en BD:" -ForegroundColor Green
        Write-Host $pgResult -ForegroundColor White
    }
    
} catch {
    Write-Host "   ❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails) {
        Write-Host "   Detalles: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
}
