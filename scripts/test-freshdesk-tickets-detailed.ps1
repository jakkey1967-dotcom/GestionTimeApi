# Test completo de endpoints de Freshdesk con foco en TICKETS
Write-Host "╔══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "║ TEST DE FRESHDESK API - TICKETS" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════════" -ForegroundColor Cyan

# Configuración
$FRESHDESK_DOMAIN = "alterasoftware"
$FRESHDESK_APIKEY = "9i1AtT08nkY1B1BmjtLk"

# Generar Auth Header
$base64Auth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("${FRESHDESK_APIKEY}:X"))
$headers = @{
    "Authorization" = "Basic $base64Auth"
    "Content-Type" = "application/json"
}

Write-Host "`n📋 Configuración:" -ForegroundColor Yellow
Write-Host "   Domain: $FRESHDESK_DOMAIN" -ForegroundColor White
Write-Host "   API Key: $($FRESHDESK_APIKEY.Substring(0,4))...$($FRESHDESK_APIKEY.Substring($FRESHDESK_APIKEY.Length-4))" -ForegroundColor White
Write-Host "   Auth Header: Basic $($base64Auth.Substring(0,20))..." -ForegroundColor White

# Array de endpoints a probar
$endpoints = @(
    @{
        Name = "1. List Tickets (per_page=5)"
        Url = "https://${FRESHDESK_DOMAIN}.freshdesk.com/api/v2/tickets?per_page=5"
        Method = "GET"
    },
    @{
        Name = "2. List Tickets (page=1)"
        Url = "https://${FRESHDESK_DOMAIN}.freshdesk.com/api/v2/tickets?page=1"
        Method = "GET"
    },
    @{
        Name = "3. List Tickets (sin parámetros)"
        Url = "https://${FRESHDESK_DOMAIN}.freshdesk.com/api/v2/tickets"
        Method = "GET"
    },
    @{
        Name = "4. Filter Tickets (new_and_my_open)"
        Url = "https://${FRESHDESK_DOMAIN}.freshdesk.com/api/v2/tickets?filter=new_and_my_open"
        Method = "GET"
    },
    @{
        Name = "5. Search Tickets (query)"
        Url = "https://${FRESHDESK_DOMAIN}.freshdesk.com/api/v2/search/tickets?query=`"status:2`""
        Method = "GET"
    },
    @{
        Name = "6. Agents Me"
        Url = "https://${FRESHDESK_DOMAIN}.freshdesk.com/api/v2/agents/me"
        Method = "GET"
    },
    @{
        Name = "7. List Agents"
        Url = "https://${FRESHDESK_DOMAIN}.freshdesk.com/api/v2/agents"
        Method = "GET"
    }
)

Write-Host "`n🧪 Ejecutando tests..." -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════`n" -ForegroundColor Cyan

$successCount = 0
$failCount = 0
$results = @()

foreach ($endpoint in $endpoints) {
    Write-Host "📝 Test: $($endpoint.Name)" -ForegroundColor Yellow
    Write-Host "   URL: $($endpoint.Url)" -ForegroundColor Gray
    
    try {
        $response = Invoke-RestMethod -Uri $endpoint.Url -Method $endpoint.Method -Headers $headers -ErrorAction Stop
        
        Write-Host "   ✅ SUCCESS (200 OK)" -ForegroundColor Green
        
        # Mostrar información relevante según el endpoint
        if ($endpoint.Url -like "*tickets*") {
            if ($response -is [Array]) {
                Write-Host "   📊 Tickets obtenidos: $($response.Count)" -ForegroundColor Cyan
                if ($response.Count -gt 0) {
                    Write-Host "   Primer ticket:" -ForegroundColor Gray
                    Write-Host "      ID: $($response[0].id)" -ForegroundColor White
                    Write-Host "      Subject: $($response[0].subject)" -ForegroundColor White
                    Write-Host "      Status: $($response[0].status)" -ForegroundColor White
                    Write-Host "      Created: $($response[0].created_at)" -ForegroundColor White
                }
            } elseif ($response.results) {
                Write-Host "   📊 Results: $($response.results.Count) / Total: $($response.total)" -ForegroundColor Cyan
            }
        } elseif ($endpoint.Url -like "*agents*") {
            if ($response.email) {
                Write-Host "   👤 Agent: $($response.email)" -ForegroundColor Cyan
            } elseif ($response -is [Array]) {
                Write-Host "   👥 Agents: $($response.Count)" -ForegroundColor Cyan
            }
        }
        
        $successCount++
        $results += @{
            Test = $endpoint.Name
            Status = "✅ SUCCESS"
            StatusCode = 200
        }
        
    } catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        $statusText = $_.Exception.Response.StatusCode
        
        Write-Host "   ❌ FAILED ($statusCode - $statusText)" -ForegroundColor Red
        
        try {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $responseBody = $reader.ReadToEnd()
            Write-Host "   Error: $responseBody" -ForegroundColor Red
        } catch {
            Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
        }
        
        $failCount++
        $results += @{
            Test = $endpoint.Name
            Status = "❌ FAILED"
            StatusCode = $statusCode
        }
    }
    
    Write-Host ""
}

Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "📊 RESUMEN DE TESTS" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "   ✅ Exitosos: $successCount" -ForegroundColor Green
Write-Host "   ❌ Fallidos: $failCount" -ForegroundColor Red
Write-Host "   📊 Total: $($endpoints.Count)" -ForegroundColor Cyan
Write-Host ""

# Mostrar tabla de resultados
Write-Host "Resultados detallados:" -ForegroundColor Yellow
foreach ($result in $results) {
    $statusColor = if ($result.Status -like "*SUCCESS*") { "Green" } else { "Red" }
    Write-Host "   $($result.Status) - $($result.Test)" -ForegroundColor $statusColor
}

Write-Host "`n═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan

# Diagnóstico final
if ($failCount -eq $endpoints.Count) {
    Write-Host "`n❌ DIAGNÓSTICO: API Key INVÁLIDA o SIN PERMISOS" -ForegroundColor Red
    Write-Host ""
    Write-Host "Todos los tests fallaron. Posibles causas:" -ForegroundColor Yellow
    Write-Host "   1. La API Key '$FRESHDESK_APIKEY' es incorrecta" -ForegroundColor White
    Write-Host "   2. La API Key ha expirado" -ForegroundColor White
    Write-Host "   3. La API Key no tiene permisos suficientes" -ForegroundColor White
    Write-Host ""
    Write-Host "Solución:" -ForegroundColor Green
    Write-Host "   1. Ve a: https://${FRESHDESK_DOMAIN}.freshdesk.com" -ForegroundColor White
    Write-Host "   2. Profile → Profile Settings → Your API Key" -ForegroundColor White
    Write-Host "   3. Copia la API Key completa" -ForegroundColor White
    Write-Host "   4. Ejecuta: dotnet user-secrets set `"Freshdesk:ApiKey`" `"TU_NUEVA_API_KEY`"" -ForegroundColor White
} elseif ($failCount -gt 0) {
    Write-Host "`n⚠️  DIAGNÓSTICO: API Key VÁLIDA pero con PERMISOS LIMITADOS" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Algunos tests pasaron, otros fallaron. Verifica permisos de la API Key." -ForegroundColor White
} else {
    Write-Host "`n✅ DIAGNÓSTICO: API Key VÁLIDA y FUNCIONANDO CORRECTAMENTE" -ForegroundColor Green
    Write-Host ""
    Write-Host "Todos los tests pasaron. La integración con Freshdesk está lista." -ForegroundColor White
}

Write-Host ""
