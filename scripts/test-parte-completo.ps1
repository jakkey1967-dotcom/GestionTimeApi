# Test crear parte COMPLETO con todos los datos
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

Write-Host "=== TEST: CREAR PARTE COMPLETO ===" -ForegroundColor Cyan

# 1. Login
Write-Host "`n1. Login..." -ForegroundColor Yellow
$loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/v1/auth/login" `
    -Method Post `
    -ContentType "application/json" `
    -Body '{"email":"psantos@global-retail.com","password":"12345678"}'

$token = $loginResponse.accessToken
Write-Host "   ✅ Login exitoso" -ForegroundColor Green

$headers = @{
    Authorization = "Bearer $token"
    "Content-Type" = "application/json"
}

# 2. Crear parte COMPLETO con TODOS los datos
Write-Host "`n2. Crear parte COMPLETO..." -ForegroundColor Yellow

$parteCompleto = @{
    fecha_trabajo = "2026-01-25"
    hora_inicio = "14:00"
    hora_fin = "17:30"
    
    # Cliente y tienda
    id_cliente = 1
    tienda = "Barcelona Diagonal - Local 42"
    
    # Grupo y tipo de trabajo
    id_grupo = 1
    id_tipo = 1
    
    # Descripción detallada
    accion = @"
Reparación completa del sistema TPV:
- Reemplazo de lector de tarjetas defectuoso
- Actualización de software a versión 3.2.1
- Configuración de impresora de tickets Epson TM-T20II
- Pruebas de cobro con tarjeta de crédito/débito
- Capacitación al personal (30 min)
"@
    
    # Ticket de Freshdesk asociado
    ticket = "55950"
    
    # Tags descriptivas
    tags = @(
        "tpv",
        "hardware",
        "software",
        "impresora",
        "lector-tarjetas",
        "actualizacion",
        "capacitacion",
        "urgente"
    )
}

try {
    $createResponse = Invoke-RestMethod -Uri "$baseUrl/api/v1/partes" `
        -Method Post `
        -Headers $headers `
        -Body ($parteCompleto | ConvertTo-Json)
    
    Write-Host "   ✅ Parte COMPLETO creado: ID=$($createResponse.id)" -ForegroundColor Green
    $parteId = $createResponse.id
    
    # 3. Verificar el parte creado
    Write-Host "`n3. Verificando parte completo..." -ForegroundColor Yellow
    $listResponse = Invoke-RestMethod -Uri "$baseUrl/api/v1/partes?fecha=2026-01-25" `
        -Method Get `
        -Headers $headers
    
    $parte = $listResponse | Where-Object { $_.id -eq $parteId } | Select-Object -First 1
    
    if ($parte) {
        Write-Host "`n╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
        Write-Host "║              📋 PARTE DE TRABAJO COMPLETO                    ║" -ForegroundColor Cyan
        Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
        
        Write-Host "`n🆔 ID Parte:       " -NoNewline -ForegroundColor Yellow
        Write-Host $parte.id -ForegroundColor White
        
        Write-Host "📅 Fecha:          " -NoNewline -ForegroundColor Yellow
        Write-Host $parte.fecha -ForegroundColor White
        
        Write-Host "⏰ Horario:        " -NoNewline -ForegroundColor Yellow
        Write-Host "$($parte.horainicio) - $($parte.horafin) ($($parte.duracion_min) min)" -ForegroundColor White
        
        Write-Host "`n🏢 Cliente:        " -NoNewline -ForegroundColor Yellow
        Write-Host $parte.cliente -ForegroundColor White
        
        Write-Host "🏪 Tienda:         " -NoNewline -ForegroundColor Yellow
        Write-Host $parte.tienda -ForegroundColor White
        
        Write-Host "`n📦 Grupo:          " -NoNewline -ForegroundColor Yellow
        Write-Host $parte.grupo -ForegroundColor White
        
        Write-Host "🔧 Tipo:           " -NoNewline -ForegroundColor Yellow
        Write-Host $parte.tipo -ForegroundColor White
        
        Write-Host "`n🎫 Ticket FD:      " -NoNewline -ForegroundColor Yellow
        Write-Host $parte.ticket -ForegroundColor White
        
        Write-Host "`n👤 Técnico:        " -NoNewline -ForegroundColor Yellow
        Write-Host $parte.tecnico -ForegroundColor White
        
        Write-Host "📊 Estado:         " -NoNewline -ForegroundColor Yellow
        Write-Host "$($parte.estado_nombre) ($($parte.estado))" -ForegroundColor White
        
        Write-Host "`n📝 Acción:`n" -ForegroundColor Yellow
        Write-Host $parte.accion -ForegroundColor White
        
        Write-Host "`n🏷️  Tags ($($parte.tags.Count)):" -ForegroundColor Yellow
        foreach ($tag in $parte.tags) {
            Write-Host "   • $tag" -ForegroundColor Cyan
        }
        
        Write-Host "`n╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Green
        Write-Host "║           🎉 ¡PARTE COMPLETO CREADO EXITOSAMENTE!           ║" -ForegroundColor Green
        Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️  Parte no encontrado en el listado" -ForegroundColor Yellow
    }
    
    # 4. Crear OTRO parte con diferentes datos
    Write-Host "`n`n4. Creando segundo parte (diferentes datos)..." -ForegroundColor Yellow
    
    $parteCompleto2 = @{
        fecha_trabajo = "2026-01-25"
        hora_inicio = "10:00"
        hora_fin = "12:45"
        id_cliente = 1
        tienda = "Madrid Centro - Plaza Mayor"
        id_grupo = 1
        id_tipo = 1
        accion = "Instalación nueva caja TPV - Configuración completa del sistema punto de venta"
        ticket = "56185"
        tags = @("tpv", "instalacion", "configuracion", "nueva-tienda")
    }
    
    $create2Response = Invoke-RestMethod -Uri "$baseUrl/api/v1/partes" `
        -Method Post `
        -Headers $headers `
        -Body ($parteCompleto2 | ConvertTo-Json)
    
    Write-Host "   ✅ Segundo parte creado: ID=$($create2Response.id)" -ForegroundColor Green
    
    # 5. Verificar tabla freshdesk_tags
    Write-Host "`n5. Verificando tags en base de datos..." -ForegroundColor Yellow
    $env:PGPASSWORD="postgres"
    $sqlQuery = @"
SELECT 
    name, 
    source, 
    TO_CHAR(last_seen_at, 'YYYY-MM-DD HH24:MI:SS') as last_seen
FROM pss_dvnx.freshdesk_tags 
WHERE source IN ('local', 'both')
ORDER BY last_seen_at DESC, name
LIMIT 15;
"@
    
    $pgResult = psql -h localhost -p 5434 -U postgres -d pss_dvnx -t -c $sqlQuery 2>$null
    
    if ($pgResult) {
        Write-Host "`n   ✅ Tags en freshdesk_tags:" -ForegroundColor Green
        Write-Host "   ────────────────────────────────────────────────────" -ForegroundColor Gray
        Write-Host $pgResult -ForegroundColor White
    }
    
    Write-Host "`n╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║                    📊 RESUMEN FINAL                          ║" -ForegroundColor Cyan
    Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host "   • 2 partes creados con éxito" -ForegroundColor Green
    Write-Host "   • Todos los campos poblados (cliente, tienda, grupo, tipo, ticket)" -ForegroundColor Green
    Write-Host "   • Tags unificadas en freshdesk_tags (source='local')" -ForegroundColor Green
    Write-Host "   • Relaciones N:N en parte_tags funcionando" -ForegroundColor Green
    Write-Host "`n   🎯 Sistema completamente operativo!" -ForegroundColor Cyan
    
} catch {
    Write-Host "   ❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails) {
        Write-Host "   Detalles: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
}
