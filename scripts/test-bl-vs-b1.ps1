# Script para probar AMBAS variantes de la API Key
Write-Host "╔══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "║ TEST: ¿Bl (letra l) o B1 (número 1)?" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════════" -ForegroundColor Cyan

# Variante 1: Con "Bl" (letra l) - LO QUE ESTÁ EN USER SECRETS
$apiKey1 = "9i1AtT08nkY1BlBmjtLk"
Write-Host "`n🔑 Variante 1 (Bl con letra l): '$apiKey1'" -ForegroundColor Yellow

# Variante 2: Con "B1" (número 1) - LO QUE ESTAMOS PROBANDO
$apiKey2 = "9i1AtT08nkY1B1BmjtLk"
Write-Host "🔑 Variante 2 (B1 con número 1): '$apiKey2'" -ForegroundColor Yellow

$testUrl = "https://alterasoftware.freshdesk.com/api/v2/tickets?per_page=1"

# Probar variante 1
Write-Host "`n📝 Probando Variante 1 (Bl - letra l)..." -ForegroundColor Cyan
$auth1 = "${apiKey1}:X"
$base64_1 = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($auth1))
$headers1 = @{ "Authorization" = "Basic $base64_1"; "Content-Type" = "application/json" }

try {
    $response1 = Invoke-WebRequest -Uri $testUrl -Method GET -Headers $headers1 -ErrorAction Stop
    Write-Host "   ✅ SUCCESS - Status: $($response1.StatusCode)" -ForegroundColor Green
    $tickets1 = $response1.Content | ConvertFrom-Json
    Write-Host "   Tickets obtenidos: $($tickets1.Count)" -ForegroundColor Green
    if ($tickets1.Count -gt 0) {
        Write-Host "   Primer ticket: ID=$($tickets1[0].id), Subject=$($tickets1[0].subject)" -ForegroundColor White
    }
    Write-Host "`n   ✨✨✨ ESTA ES LA CORRECTA! (Bl con letra l) ✨✨✨" -ForegroundColor Green
    Write-Host "   Comando para actualizar User Secrets:" -ForegroundColor Yellow
    Write-Host "   dotnet user-secrets set `"Freshdesk:ApiKey`" `"$apiKey1`"" -ForegroundColor White
} catch {
    $status1 = $_.Exception.Response.StatusCode.value__
    Write-Host "   ❌ FAILED - Status: $status1" -ForegroundColor Red
    try {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $errorBody = $reader.ReadToEnd()
        Write-Host "   Error: $errorBody" -ForegroundColor Red
    } catch {}
}

# Probar variante 2
Write-Host "`n📝 Probando Variante 2 (B1 - número 1)..." -ForegroundColor Cyan
$auth2 = "${apiKey2}:X"
$base64_2 = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($auth2))
$headers2 = @{ "Authorization" = "Basic $base64_2"; "Content-Type" = "application/json" }

try {
    $response2 = Invoke-WebRequest -Uri $testUrl -Method GET -Headers $headers2 -ErrorAction Stop
    Write-Host "   ✅ SUCCESS - Status: $($response2.StatusCode)" -ForegroundColor Green
    $tickets2 = $response2.Content | ConvertFrom-Json
    Write-Host "   Tickets obtenidos: $($tickets2.Count)" -ForegroundColor Green
    if ($tickets2.Count -gt 0) {
        Write-Host "   Primer ticket: ID=$($tickets2[0].id), Subject=$($tickets2[0].subject)" -ForegroundColor White
    }
    Write-Host "`n   ✨✨✨ ESTA ES LA CORRECTA! (B1 con número 1) ✨✨✨" -ForegroundColor Green
    Write-Host "   Comando para actualizar User Secrets:" -ForegroundColor Yellow
    Write-Host "   dotnet user-secrets set `"Freshdesk:ApiKey`" `"$apiKey2`"" -ForegroundColor White
} catch {
    $status2 = $_.Exception.Response.StatusCode.value__
    Write-Host "   ❌ FAILED - Status: $status2" -ForegroundColor Red
    try {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $errorBody = $reader.ReadToEnd()
        Write-Host "   Error: $errorBody" -ForegroundColor Red
    } catch {}
}

Write-Host "`n╔══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "║ RESUMEN" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════════" -ForegroundColor Cyan

Write-Host "`nPor favor, verifica en Freshdesk cuál es la correcta:" -ForegroundColor Yellow
Write-Host "   https://alterasoftware.freshdesk.com/a/admin/profile" -ForegroundColor White
Write-Host "`nBusca el carácter entre 'Y1' y 'Bm':" -ForegroundColor Yellow
Write-Host "   ¿Es 'Bl' (letra l minúscula)?" -ForegroundColor White
Write-Host "   ¿Es 'B1' (número uno)?" -ForegroundColor White
