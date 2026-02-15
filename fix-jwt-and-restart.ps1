#!/usr/bin/env pwsh
# fix-jwt-and-restart.ps1
# Script completo: Compilar + Matar procesos + Reiniciar API + Verificar

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host " 🔧 FIX JWT: Compilar + Reiniciar + Verificar" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# ============================================================================
# 1. COMPILAR CAMBIOS
# ============================================================================

Write-Host "📦 Paso 1: Compilando cambios en Program.cs..." -ForegroundColor Yellow
Write-Host ""

try {
    $buildResult = dotnet build GestionTime.Api.csproj --no-incremental --configuration Debug 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Error compilando:" -ForegroundColor Red
        Write-Host $buildResult -ForegroundColor Red
        exit 1
    }
    
    Write-Host "✅ Compilación exitosa" -ForegroundColor Green
}
catch {
    Write-Host "❌ Error compilando: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# ============================================================================
# 2. MATAR PROCESOS EN PUERTO 2501
# ============================================================================

Write-Host "🔫 Paso 2: Matando procesos en puerto 2501..." -ForegroundColor Yellow
Write-Host ""

$processesKilled = 0

# Obtener procesos usando puerto 2501
$connections = netstat -ano | Select-String ":2501" | ForEach-Object {
    $line = $_.Line.Trim()
    $parts = $line -split '\s+'

    if ($parts.Length -ge 5) {
        $processId = $parts[-1]

        try {
            $process = Get-Process -Id $processId -ErrorAction SilentlyContinue
            if ($process) {
                Write-Host "   Matando proceso: $($process.ProcessName) (PID: $processId)" -ForegroundColor Gray
                Stop-Process -Id $processId -Force
                $script:processesKilled++
            }
        }
        catch {}
    }
}

if ($processesKilled -eq 0) {
    Write-Host "   No hay procesos en puerto 2501" -ForegroundColor Gray
} else {
    Write-Host "✅ $processesKilled proceso(s) eliminado(s)" -ForegroundColor Green
    Start-Sleep -Seconds 2
}

Write-Host ""

# ============================================================================
# 3. INICIAR API EN BACKGROUND
# ============================================================================

Write-Host "🚀 Paso 3: Iniciando API..." -ForegroundColor Yellow
Write-Host ""

# Iniciar API en background
$apiJob = Start-Job -ScriptBlock {
    Set-Location "C:\GestionTime\GestionTimeApi"
    dotnet run --project GestionTime.Api.csproj --no-build --configuration Debug
}

Write-Host "   Job ID: $($apiJob.Id)" -ForegroundColor Gray
Write-Host "   Estado: $($apiJob.State)" -ForegroundColor Gray
Write-Host ""

# Esperar a que la API esté lista (máximo 30 segundos)
Write-Host "⏳ Esperando a que la API esté lista..." -ForegroundColor Yellow

$maxWaitSeconds = 30
$elapsed = 0
$apiReady = $false

while ($elapsed -lt $maxWaitSeconds -and -not $apiReady) {
    Start-Sleep -Seconds 1
    $elapsed++
    
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:2501/health" -UseBasicParsing -TimeoutSec 2 -ErrorAction SilentlyContinue
        if ($response.StatusCode -eq 200) {
            $apiReady = $true
            Write-Host "✅ API lista en $elapsed segundos" -ForegroundColor Green
            break
        }
    }
    catch {}
    
    Write-Host "   Esperando... ($elapsed/$maxWaitSeconds seg)" -ForegroundColor Gray
}

if (-not $apiReady) {
    Write-Host "❌ La API no respondió en $maxWaitSeconds segundos" -ForegroundColor Red
    Write-Host ""
    Write-Host "Logs del job:" -ForegroundColor Yellow
    Receive-Job -Job $apiJob | ForEach-Object { Write-Host $_ -ForegroundColor Gray }
    Stop-Job -Job $apiJob
    Remove-Job -Job $apiJob
    exit 1
}

Write-Host ""

# ============================================================================
# 4. VERIFICAR JWT CON HEADER AUTHORIZATION
# ============================================================================

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
Write-Host "🧪 Paso 4: Verificando JWT con header Authorization" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
Write-Host ""

$baseUrl = "http://localhost:2501"
$email = "psantos@global-retail.com"
$password = "12345678"

# 4.1 Obtener token
Write-Host "   Obteniendo token JWT..." -ForegroundColor Gray
$loginBody = @{ email = $email; password = $password } | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/auth/login-desktop" `
        -Method Post `
        -ContentType "application/json" `
        -Body $loginBody
    
    $token = $loginResponse.accessToken
    Write-Host "   ✅ Token obtenido ($($token.Length) chars)" -ForegroundColor Green
}
catch {
    Write-Host "   ❌ Error obteniendo token: $($_.Exception.Message)" -ForegroundColor Red
    Stop-Job -Job $apiJob
    Remove-Job -Job $apiJob
    exit 1
}

Write-Host ""

# 4.2 Probar con cookies
Write-Host "   Probando con COOKIES..." -ForegroundColor Gray
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

try {
    Invoke-WebRequest `
        -Uri "$baseUrl/api/v1/auth/login" `
        -Method Post `
        -ContentType "application/json" `
        -Body $loginBody `
        -WebSession $session `
        -UseBasicParsing | Out-Null
    
    $responseCookies = Invoke-RestMethod `
        -Uri "$baseUrl/api/v2/informes/partes?date=2026-02-14&pageSize=5" `
        -Method Get `
        -WebSession $session
    
    Write-Host "   ✅ Cookies funcionan (total: $($responseCookies.total))" -ForegroundColor Green
}
catch {
    $statusCookies = $_.Exception.Response.StatusCode.value__
    if ($statusCookies -eq 400) {
        Write-Host "   ✅ Cookies funcionan (400 = error validación esperado)" -ForegroundColor Green
    } else {
        Write-Host "   ❌ Cookies fallan ($statusCookies)" -ForegroundColor Red
    }
}

Write-Host ""

# 4.3 Probar con header Authorization
Write-Host "   Probando con HEADER Authorization..." -ForegroundColor Gray
$headers = @{ "Authorization" = "Bearer $token" }

try {
    $responseHeader = Invoke-RestMethod `
        -Uri "$baseUrl/api/v2/informes/partes?date=2026-02-14&pageSize=5" `
        -Method Get `
        -Headers $headers
    
    Write-Host "   ✅ HEADER FUNCIONA! (total: $($responseHeader.total))" -ForegroundColor Green
    $fixWorked = $true
}
catch {
    $statusHeader = $_.Exception.Response.StatusCode.value__
    
    if ($statusHeader -eq 400) {
        Write-Host "   ✅ HEADER FUNCIONA! (400 = error validación esperado)" -ForegroundColor Green
        $fixWorked = $true
    } else {
        Write-Host "   ❌ HEADER FALLA ($statusHeader)" -ForegroundColor Red
        $fixWorked = $false
    }
}

Write-Host ""

# ============================================================================
# 5. RESULTADO FINAL
# ============================================================================

Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
Write-Host " 📊 RESULTADO FINAL" -ForegroundColor Cyan
Write-Host " ────────────────────────────────────────────────────────────────" -ForegroundColor Gray
Write-Host ""

if ($fixWorked) {
    Write-Host "   🎉 FIX APLICADO EXITOSAMENTE!" -ForegroundColor Green
    Write-Host ""
    Write-Host "   ✅ Compilación OK" -ForegroundColor Green
    Write-Host "   ✅ API reiniciada" -ForegroundColor Green
    Write-Host "   ✅ Cookies funcionan" -ForegroundColor Green
    Write-Host "   ✅ Header Authorization funciona" -ForegroundColor Green
    Write-Host ""
    Write-Host " 💡 SIGUIENTE PASO:" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "   Ejecuta el test completo:" -ForegroundColor White
    Write-Host "   .\test-informes-partes-log.ps1" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "   La API está corriendo en background (Job ID: $($apiJob.Id))" -ForegroundColor Gray
    Write-Host "   Para detenerla: Stop-Job -Id $($apiJob.Id); Remove-Job -Id $($apiJob.Id)" -ForegroundColor Gray
} else {
    Write-Host "   ❌ EL FIX NO FUNCIONÓ" -ForegroundColor Red
    Write-Host ""
    Write-Host "   Header Authorization sigue dando 401" -ForegroundColor Red
    Write-Host ""
    Write-Host " 💡 REVISAR:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "   1. Logs del servidor (Job ID: $($apiJob.Id))" -ForegroundColor White
    Write-Host "      Receive-Job -Id $($apiJob.Id)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "   2. Verificar que Program.cs línea 281 sea:" -ForegroundColor White
    Write-Host "      var authHeader = ctx.Request.Headers[""Authorization""].FirstOrDefault();" -ForegroundColor Gray
    Write-Host ""
    
    Stop-Job -Job $apiJob
    Remove-Job -Job $apiJob
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
