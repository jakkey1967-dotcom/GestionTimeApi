#!/usr/bin/env pwsh
# test-informes-partes-log.ps1
# Test compacto con formato de logs para identificar problemas rápidamente

$ErrorActionPreference = "Continue"
$baseUrl = "https://localhost:2502"
$email = "psantos@global-retail.com"
$password = "12345678"

# Contadores
$script:totalTests = 0
$script:passedTests = 0
$script:failedTests = 0
$script:expectedErrors = 0
$script:failedTestsList = @()

# Función para log timestamp
function Get-LogTimestamp {
    return (Get-Date -Format "HH:mm:ss.fff")
}

# Función para log con nivel
function Write-Log {
    param(
        [string]$Level,
        [string]$Message,
        [string]$Color = "White"
    )
    $timestamp = Get-LogTimestamp
    Write-Host "[$timestamp] [$Level] $Message" -ForegroundColor $Color
}

# Función de test compacta
function Test-Endpoint {
    param(
        [string]$TestName,
        [string]$Url,
        [hashtable]$Headers,
        [int]$ExpectedStatus = 200,
        [string]$Category = "General"
    )
    
    $script:totalTests++
    $testNum = $script:totalTests.ToString().PadLeft(2, '0')
    
    try {
        $response = Invoke-WebRequest -Uri $Url -Method Get -Headers $Headers -ErrorAction Stop
        $json = $response.Content | ConvertFrom-Json
        
        if ($response.StatusCode -eq $ExpectedStatus) {
            $script:passedTests++
            Write-Log "INFO" "[$testNum/$Category] ✅ $TestName → 200 (total: $($json.total), items: $($json.items.Count))" "Green"
        } else {
            $script:failedTests++
            $script:failedTestsList += @{
                Test = "$testNum - $TestName"
                Expected = $ExpectedStatus
                Actual = $response.StatusCode
                Category = $Category
                Url = $Url
            }
            Write-Log "ERROR" "[$testNum/$Category] ❌ $TestName → Expected $ExpectedStatus, got $($response.StatusCode)" "Red"
        }
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        
        if ($statusCode -eq $ExpectedStatus) {
            $script:expectedErrors++
            $errorMsg = "Sin detalles"
            if ($_.ErrorDetails.Message) {
                try {
                    $errorJson = $_.ErrorDetails.Message | ConvertFrom-Json
                    $errorMsg = $errorJson.error
                } catch {}
            }
            Write-Log "WARN" "[$testNum/$Category] ⚠️  $TestName → $statusCode (esperado: $errorMsg)" "Yellow"
        }
        else {
            $script:failedTests++
            $errorMsg = $_.Exception.Message
            if ($_.ErrorDetails.Message) {
                try {
                    $errorJson = $_.ErrorDetails.Message | ConvertFrom-Json
                    $errorMsg = $errorJson.error
                } catch {}
            }
            $script:failedTestsList += @{
                Test = "$testNum - $TestName"
                Expected = $ExpectedStatus
                Actual = $statusCode
                Category = $Category
                Url = $Url
                Error = $errorMsg
            }
            Write-Log "ERROR" "[$testNum/$Category] ❌ $TestName → Expected $ExpectedStatus, got $statusCode | $errorMsg" "Red"
        }
    }
}

# ============================================================================
# INICIO
# ============================================================================

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host " 📊 TEST LOG: /api/v2/informes/partes" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Obtener token
Write-Log "INFO" "🔐 Obteniendo token JWT..." "Cyan"

try {
    $loginBody = @{ email = $email; password = $password } | ConvertTo-Json
    $loginResponse = Invoke-WebRequest -Uri "$baseUrl/api/v1/auth/login-desktop" -Method Post -ContentType "application/json" -Body $loginBody -ErrorAction Stop
    $loginJson = $loginResponse.Content | ConvertFrom-Json
    $token = $loginJson.accessToken
    $userId = $loginJson.user.id
    $userRole = $loginJson.user.role
    
    Write-Log "INFO" "✅ Token obtenido: $($loginJson.user.email) | Rol: $userRole | ID: $userId" "Green"
    
    $headers = @{ "Authorization" = "Bearer $token" }
}
catch {
    Write-Log "ERROR" "❌ Error obteniendo token: $($_.Exception.Message)" "Red"
    exit 1
}

Write-Host ""

# Variables de fecha
$today = Get-Date -Format "yyyy-MM-dd"
$yesterday = (Get-Date).AddDays(-1).ToString("yyyy-MM-dd")
$weekStart = (Get-Date).AddDays(-7).ToString("yyyy-MM-dd")
$currentWeek = Get-Date -UFormat "%Y-W%V"
$monthStart = (Get-Date -Day 1).ToString("yyyy-MM-dd")
$monthEnd = (Get-Date -Day 1).AddMonths(1).AddDays(-1).ToString("yyyy-MM-dd")
$yearStart = (Get-Date -Month 1 -Day 1).ToString("yyyy-MM-dd")
$yearEnd = (Get-Date -Month 12 -Day 31).ToString("yyyy-MM-dd")
$otherUserId = "00000000-0000-0000-0000-000000000001"

# ============================================================================
# TESTS
# ============================================================================

Write-Log "INFO" "═══ [1] VALIDACIÓN PARÁMETROS OBLIGATORIOS ===" "Yellow"
Test-Endpoint "Sin fecha" "$baseUrl/api/v2/informes/partes?page=1" $headers 400 "Validación"
Test-Endpoint "Fecha inválida" "$baseUrl/api/v2/informes/partes?date=invalido" $headers 400 "Validación"
Test-Endpoint "WeekIso inválido" "$baseUrl/api/v2/informes/partes?weekIso=2026-W99" $headers 400 "Validación"
Test-Endpoint "From sin To" "$baseUrl/api/v2/informes/partes?from=2026-02-01" $headers 400 "Validación"
Write-Host ""

Write-Log "INFO" "═══ [2] FILTROS DE FECHA ===" "Yellow"
Test-Endpoint "Hoy ($today)" "$baseUrl/api/v2/informes/partes?date=$today&pageSize=10" $headers 200 "Fecha"
Test-Endpoint "Ayer ($yesterday)" "$baseUrl/api/v2/informes/partes?date=$yesterday&pageSize=10" $headers 200 "Fecha"
Test-Endpoint "Semana ISO ($currentWeek)" "$baseUrl/api/v2/informes/partes?weekIso=$currentWeek&pageSize=10" $headers 200 "Fecha"
Test-Endpoint "Rango 7 días" "$baseUrl/api/v2/informes/partes?from=$weekStart&to=$today&pageSize=10" $headers 200 "Fecha"
Test-Endpoint "Mes completo" "$baseUrl/api/v2/informes/partes?from=$monthStart&to=$monthEnd&pageSize=10" $headers 200 "Fecha"
Write-Host ""

Write-Log "INFO" "═══ [3] PAGINACIÓN ===" "Yellow"
Test-Endpoint "page=1 size=10" "$baseUrl/api/v2/informes/partes?date=$today&page=1&pageSize=10" $headers 200 "Paginación"
Test-Endpoint "page=1 size=50" "$baseUrl/api/v2/informes/partes?date=$today&page=1&pageSize=50" $headers 200 "Paginación"
Test-Endpoint "page=2 size=20" "$baseUrl/api/v2/informes/partes?date=$today&page=2&pageSize=20" $headers 200 "Paginación"
Test-Endpoint "size=200 (max)" "$baseUrl/api/v2/informes/partes?date=$today&pageSize=200" $headers 200 "Paginación"
Test-Endpoint "Valores default" "$baseUrl/api/v2/informes/partes?date=$today" $headers 200 "Paginación"
Write-Host ""

Write-Log "INFO" "═══ [4] ORDENAMIENTO ===" "Yellow"
Test-Endpoint "fecha:desc" "$baseUrl/api/v2/informes/partes?date=$today&sort=fecha_trabajo:desc&pageSize=10" $headers 200 "Sort"
Test-Endpoint "fecha:asc" "$baseUrl/api/v2/informes/partes?date=$today&sort=fecha_trabajo:asc&pageSize=10" $headers 200 "Sort"
Test-Endpoint "fecha:desc,hora:asc" "$baseUrl/api/v2/informes/partes?date=$today&sort=fecha_trabajo:desc,hora_inicio:asc&pageSize=10" $headers 200 "Sort"
Test-Endpoint "duracion:desc" "$baseUrl/api/v2/informes/partes?date=$today&sort=duracion_min:desc&pageSize=10" $headers 200 "Sort"
Test-Endpoint "agente:asc" "$baseUrl/api/v2/informes/partes?date=$today&sort=agente_nombre:asc&pageSize=10" $headers 200 "Sort"
Test-Endpoint "cliente:asc" "$baseUrl/api/v2/informes/partes?date=$today&sort=cliente_nombre:asc&pageSize=10" $headers 200 "Sort"
Write-Host ""

Write-Log "INFO" "═══ [5] FILTROS AGENTE (SEGURIDAD) ===" "Yellow"
Test-Endpoint "Sin filtro (auto USER)" "$baseUrl/api/v2/informes/partes?date=$today&pageSize=10" $headers 200 "Agente"
Test-Endpoint "agentId propio" "$baseUrl/api/v2/informes/partes?date=$today&agentId=$userId&pageSize=10" $headers 200 "Agente"
Test-Endpoint "agentId ajeno (403)" "$baseUrl/api/v2/informes/partes?date=$today&agentId=$otherUserId&pageSize=10" $headers 403 "Agente"
Test-Endpoint "agentIds múltiples (403)" "$baseUrl/api/v2/informes/partes?date=$today&agentIds=$userId,$otherUserId&pageSize=10" $headers 403 "Agente"
Write-Host ""

Write-Log "INFO" "═══ [6] FILTROS CATÁLOGOS ===" "Yellow"
Test-Endpoint "clientId=1" "$baseUrl/api/v2/informes/partes?date=$today&clientId=1&pageSize=10" $headers 200 "Catálogo"
Test-Endpoint "groupId=1" "$baseUrl/api/v2/informes/partes?date=$today&groupId=1&pageSize=10" $headers 200 "Catálogo"
Test-Endpoint "typeId=1" "$baseUrl/api/v2/informes/partes?date=$today&typeId=1&pageSize=10" $headers 200 "Catálogo"
Test-Endpoint "cliente+grupo" "$baseUrl/api/v2/informes/partes?date=$today&clientId=1&groupId=1&pageSize=10" $headers 200 "Catálogo"
Test-Endpoint "cliente+grupo+tipo" "$baseUrl/api/v2/informes/partes?date=$today&clientId=1&groupId=1&typeId=1&pageSize=10" $headers 200 "Catálogo"
Write-Host ""

Write-Log "INFO" "═══ [7] BÚSQUEDA TEXTO ===" "Yellow"
Test-Endpoint "q=TK" "$baseUrl/api/v2/informes/partes?date=$today&q=TK&pageSize=10" $headers 200 "Búsqueda"
Test-Endpoint "q=instalación" "$baseUrl/api/v2/informes/partes?date=$today&q=instalación&pageSize=10" $headers 200 "Búsqueda"
Test-Endpoint "q=madrid" "$baseUrl/api/v2/informes/partes?date=$today&q=madrid&pageSize=10" $headers 200 "Búsqueda"
Test-Endpoint "q=red-router" "$baseUrl/api/v2/informes/partes?date=$today&q=red-router&pageSize=10" $headers 200 "Búsqueda"
Test-Endpoint "q= (vacío)" "$baseUrl/api/v2/informes/partes?date=$today&q=&pageSize=10" $headers 200 "Búsqueda"
Write-Host ""

Write-Log "INFO" "═══ [8] COMBINACIONES COMPLEJAS ===" "Yellow"
Test-Endpoint "rango+cliente+q+sort" "$baseUrl/api/v2/informes/partes?from=$weekStart&to=$today&clientId=1&q=TK&sort=fecha_trabajo:desc&pageSize=20" $headers 200 "Complejo"
Test-Endpoint "week+grupo+tipo+page" "$baseUrl/api/v2/informes/partes?weekIso=$currentWeek&groupId=1&typeId=1&page=1&pageSize=30" $headers 200 "Complejo"
Test-Endpoint "Todos filtros USER" "$baseUrl/api/v2/informes/partes?from=$weekStart&to=$today&agentId=$userId&clientId=1&groupId=1&typeId=1&q=TK&sort=fecha_trabajo:desc,hora_inicio:asc&page=1&pageSize=50" $headers 200 "Complejo"
Write-Host ""

Write-Log "INFO" "═══ [9] CASOS EXTREMOS ===" "Yellow"
Test-Endpoint "Fecha antigua (2020)" "$baseUrl/api/v2/informes/partes?date=2020-01-01&pageSize=10" $headers 200 "Extremo"
Test-Endpoint "Fecha futura (2030)" "$baseUrl/api/v2/informes/partes?date=2030-12-31&pageSize=10" $headers 200 "Extremo"
Test-Endpoint "Rango año completo" "$baseUrl/api/v2/informes/partes?from=$yearStart&to=$yearEnd&pageSize=100" $headers 200 "Extremo"
Test-Endpoint "Cliente inexistente" "$baseUrl/api/v2/informes/partes?date=$today&clientId=99999&pageSize=10" $headers 200 "Extremo"
Test-Endpoint "Búsqueda sin resultados" "$baseUrl/api/v2/informes/partes?date=$today&q=XYZABCINEXISTENTE&pageSize=10" $headers 200 "Extremo"
Write-Host ""

# ============================================================================
# RESUMEN
# ============================================================================

Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
Write-Host " 📊 RESUMEN FINAL" -ForegroundColor Cyan
Write-Host " ────────────────────────────────────────────────────────────────" -ForegroundColor Gray
Write-Host ""
Write-Host "   Total:      $totalTests tests" -ForegroundColor White
Write-Host "   ✅ Passed:  $passedTests" -ForegroundColor Green
Write-Host "   ⚠️  Expected: $expectedErrors (errores esperados 400/403)" -ForegroundColor Yellow
Write-Host "   ❌ Failed:  $failedTests" -ForegroundColor $(if ($failedTests -eq 0) { "Green" } else { "Red" })
Write-Host ""

$successRate = [math]::Round((($passedTests + $expectedErrors) / $totalTests) * 100, 2)
$statusColor = if ($failedTests -eq 0) { "Green" } elseif ($successRate -ge 90) { "Yellow" } else { "Red" }
Write-Host "   Success Rate: $successRate%" -ForegroundColor $statusColor
Write-Host ""

if ($failedTests -gt 0) {
    Write-Host " ❌ TESTS FALLIDOS:" -ForegroundColor Red
    Write-Host " ────────────────────────────────────────────────────────────────" -ForegroundColor Gray
    Write-Host ""
    
    foreach ($failed in $failedTestsList) {
        Write-Host "   [$($failed.Category)] $($failed.Test)" -ForegroundColor Red
        Write-Host "      Expected: $($failed.Expected) | Actual: $($failed.Actual)" -ForegroundColor Gray
        if ($failed.Error) {
            Write-Host "      Error: $($failed.Error)" -ForegroundColor DarkGray
        }
        Write-Host "      URL: $($failed.Url)" -ForegroundColor DarkGray
        Write-Host ""
    }
} else {
    Write-Host " 🎉 TODOS LOS TESTS PASARON!" -ForegroundColor Green
}

Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""

# Log final
$timestamp = Get-LogTimestamp
Write-Host "[$timestamp] [INFO] Test completado - Success: $successRate% | Passed: $passedTests | Expected: $expectedErrors | Failed: $failedTests" -ForegroundColor $(if ($failedTests -eq 0) { "Green" } else { "Yellow" })
Write-Host ""
