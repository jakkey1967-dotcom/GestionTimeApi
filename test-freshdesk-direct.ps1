# Test directo de Freshdesk API
Write-Host "🧪 Probando API de Freshdesk directamente..." -ForegroundColor Cyan
Write-Host ""

$apiKey = "9i1AtT08nkY1BlBmjtLk"
$password = "X"
$base64Auth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("${apiKey}:${password}"))

$headers = @{
    "Authorization" = "Basic $base64Auth"
    "Content-Type" = "application/json"
}

try {
    Write-Host "📡 GET https://alterasoftware.freshdesk.com/api/v2/tickets/20" -ForegroundColor Yellow
    
    $response = Invoke-RestMethod -Uri "https://alterasoftware.freshdesk.com/api/v2/tickets/20" `
        -Headers $headers `
        -Method GET `
        -ErrorAction Stop
    
    Write-Host ""
    Write-Host "✅ CONEXIÓN EXITOSA!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📋 Ticket encontrado:" -ForegroundColor Cyan
    Write-Host "   ID: $($response.id)" -ForegroundColor White
    Write-Host "   Subject: $($response.subject)" -ForegroundColor White
    Write-Host "   Status: $($response.status)" -ForegroundColor White
    Write-Host ""
    Write-Host "🎉 La API Key funciona correctamente!" -ForegroundColor Green
    
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    
    Write-Host ""
    Write-Host "❌ ERROR: $statusCode" -ForegroundColor Red
    
    if ($statusCode -eq 401) {
        Write-Host "   La API Key es incorrecta o inválida" -ForegroundColor Yellow
    } elseif ($statusCode -eq 404) {
        Write-Host "   El ticket #20 no existe (pero la API Key funciona)" -ForegroundColor Yellow
        Write-Host "   ✅ Esto significa que la API Key SÍ es válida" -ForegroundColor Green
    } else {
        Write-Host "   $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

Write-Host ""
