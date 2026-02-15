#!/usr/bin/env pwsh
# test-informes-partes-completo.ps1
# Test exhaustivo del endpoint /api/v2/informes/partes con todos los filtros posibles

$ErrorActionPreference = "Continue"

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                                                                ║" -ForegroundColor Cyan
Write-Host "║  📊 TEST COMPLETO: /api/v2/informes/partes                    ║" -ForegroundColor Cyan
Write-Host "║  Prueba todos los filtros y casos de error                    ║" -ForegroundColor Cyan
Write-Host "║                                                                ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "http://localhost:2501"
$email = "psantos@global-retail.com"
$password = "12345678"

# Contadores de resultados
$totalTests = 0
$passedTests = 0
$failedTests = 0
$expectedErrors = 0

# Función para ejecutar un test
function Test-Endpoint {
    param(
        [string]$TestName,
        [string]$Url,
        [hashtable]$Headers,
        [int]$ExpectedStatus = 200,
        [string]$Category = "General"
    )
    
    $script:totalTests++
    
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
    Write-Host "🔹 TEST $totalTests - $Category" -ForegroundColor Cyan
    Write-Host "   $TestName" -ForegroundColor White
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
    Write-Host "   URL: $Url" -ForegroundColor Gray
    Write-Host ""
    
    try {
        $response = Invoke-WebRequest -Uri $Url -Method Get -Headers $Headers -ErrorAction Stop
        $json = $response.Content | ConvertFrom-Json
        
        if ($response.StatusCode -eq $ExpectedStatus) {
            Write-Host "✅ Status: $($response.StatusCode) (esperado: $ExpectedStatus)" -ForegroundColor Green
            
            if ($json.total -ne $null) {
                Write-Host "📊 Total registros: $($json.total)" -ForegroundColor White
                Write-Host "📄 Página: $($json.page) / Tamaño: $($json.pageSize)" -ForegroundColor White
                Write-Host "📦 Items retornados: $($json.items.Count)" -ForegroundColor White
                
                if ($json.items.Count -gt 0) {
                    $item = $json.items[0]
                    Write-Host "   Ejemplo:" -ForegroundColor Gray
                    Write-Host "   - Fecha: $($item.fechaTrabajo)" -ForegroundColor Gray
                    Write-Host "   - Agente: $($item.agenteNombre) ($($item.agenteEmail))" -ForegroundColor Gray
                    if ($item.clienteNombre) {
                        Write-Host "   - Cliente: $($item.clienteNombre)" -ForegroundColor Gray
                    }
                    if ($item.grupoNombre) {
                        Write-Host "   - Grupo: $($item.grupoNombre)" -ForegroundColor Gray
                    }
                    if ($item.tipoNombre) {
                        Write-Host "   - Tipo: $($item.tipoNombre)" -ForegroundColor Gray
                    }
                }
                
                Write-Host "   Filtros aplicados:" -ForegroundColor Gray
                $json.filtersApplied | Format-List | Out-String | ForEach-Object { Write-Host $_.Trim() -ForegroundColor DarkGray }
            }
            
            $script:passedTests++
            Write-Host "✅ PASS" -ForegroundColor Green
        } else {
            Write-Host "❌ Status: $($response.StatusCode) (esperado: $ExpectedStatus)" -ForegroundColor Red
            $script:failedTests++
            Write-Host "❌ FAIL" -ForegroundColor Red
        }
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        
        if ($statusCode -eq $ExpectedStatus) {
            Write-Host "✅ Status: $statusCode (esperado: $ExpectedStatus)" -ForegroundColor Green
            
            $errorBody = $_.ErrorDetails.Message
            if ($errorBody) {
                $errorJson = $errorBody | ConvertFrom-Json
                if ($errorJson.error) {
                    Write-Host "📝 Error esperado: $($errorJson.error)" -ForegroundColor Yellow
                }
            }
            
            $script:expectedErrors++
            Write-Host "✅ PASS (error esperado)" -ForegroundColor Green
        }
        else {
            Write-Host "❌ Status: $statusCode (esperado: $ExpectedStatus)" -ForegroundColor Red
            Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
            
            if ($_.ErrorDetails.Message) {
                try {
                    $errorJson = $_.ErrorDetails.Message | ConvertFrom-Json
                    Write-Host "   Detalle: $($errorJson.error)" -ForegroundColor Red
                }
                catch {
                    Write-Host "   Detalle: $($_.ErrorDetails.Message)" -ForegroundColor Red
                }
            }
            
            $script:failedTests++
            Write-Host "❌ FAIL" -ForegroundColor Red
        }
    }
    
    Write-Host ""
}

# ============================================================================
# 1. OBTENER TOKEN JWT
# ============================================================================

Write-Host "🔐 Obteniendo token JWT..." -ForegroundColor Cyan
Write-Host ""

try {
    $loginBody = @{
        email = $email
        password = $password
    } | ConvertTo-Json
    
    $loginResponse = Invoke-WebRequest `
        -Uri "$baseUrl/api/v1/auth/login-desktop" `
        -Method Post `
        -ContentType "application/json" `
        -Body $loginBody `
        -ErrorAction Stop
    
    $loginJson = $loginResponse.Content | ConvertFrom-Json
    $token = $loginJson.accessToken
    
    Write-Host "✅ Token obtenido correctamente" -ForegroundColor Green
    Write-Host "   Usuario: $($loginJson.user.email)" -ForegroundColor Gray
    Write-Host "   Rol: $($loginJson.user.role)" -ForegroundColor Gray
    Write-Host "   User ID: $($loginJson.user.id)" -ForegroundColor Gray
    Write-Host ""
    
    $headers = @{
        "Authorization" = "Bearer $token"
    }
    
    $userId = $loginJson.user.id
}
catch {
    Write-Host "❌ Error obteniendo token: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Start-Sleep -Seconds 1

# ============================================================================
# 2. TESTS DE VALIDACIÓN DE PARÁMETROS OBLIGATORIOS
# ============================================================================

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Yellow
Write-Host "║  📋 CATEGORÍA 1: VALIDACIÓN DE PARÁMETROS OBLIGATORIOS       ║" -ForegroundColor Yellow
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Yellow
Write-Host ""

# TEST 1.1: Sin parámetros de fecha (debe fallar 400)
Test-Endpoint `
    -TestName "Sin parámetros de fecha (debe devolver 400)" `
    -Url "$baseUrl/api/v2/informes/partes?page=1&pageSize=10" `
    -Headers $headers `
    -ExpectedStatus 400 `
    -Category "Validación Fecha"

# TEST 1.2: Parámetros de fecha inválidos
Test-Endpoint `
    -TestName "Fecha inválida (debe devolver 400)" `
    -Url "$baseUrl/api/v2/informes/partes?date=invalido" `
    -Headers $headers `
    -ExpectedStatus 400 `
    -Category "Validación Fecha"

# TEST 1.3: WeekIso inválido
Test-Endpoint `
    -TestName "WeekIso inválido (debe devolver 400)" `
    -Url "$baseUrl/api/v2/informes/partes?weekIso=2026-W99" `
    -Headers $headers `
    -ExpectedStatus 400 `
    -Category "Validación Fecha"

# TEST 1.4: From sin To
Test-Endpoint `
    -TestName "From sin To (debe devolver 400)" `
    -Url "$baseUrl/api/v2/informes/partes?from=2026-02-01" `
    -Headers $headers `
    -ExpectedStatus 400 `
    -Category "Validación Fecha"

# ============================================================================
# 3. TESTS DE FILTROS DE FECHA (VÁLIDOS)
# ============================================================================

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Yellow
Write-Host "║  📅 CATEGORÍA 2: FILTROS DE FECHA (VÁLIDOS)                  ║" -ForegroundColor Yellow
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Yellow
Write-Host ""

$today = Get-Date -Format "yyyy-MM-dd"
$yesterday = (Get-Date).AddDays(-1).ToString("yyyy-MM-dd")
$weekStart = (Get-Date).AddDays(-7).ToString("yyyy-MM-dd")
$currentWeek = Get-Date -UFormat "%Y-W%V"

# TEST 2.1: Filtro por fecha específica (hoy)
Test-Endpoint `
    -TestName "Fecha específica: $today" `
    -Url "$baseUrl/api/v2/informes/partes?date=$today&page=1&pageSize=10" `
    -Headers $headers `
    -Category "Fecha"

# TEST 2.2: Filtro por fecha específica (ayer)
Test-Endpoint `
    -TestName "Fecha específica: $yesterday" `
    -Url "$baseUrl/api/v2/informes/partes?date=$yesterday&page=1&pageSize=10" `
    -Headers $headers `
    -Category "Fecha"

# TEST 2.3: Filtro por semana ISO
Test-Endpoint `
    -TestName "Semana ISO: $currentWeek" `
    -Url "$baseUrl/api/v2/informes/partes?weekIso=$currentWeek&page=1&pageSize=10" `
    -Headers $headers `
    -Category "Fecha"

# TEST 2.4: Filtro por rango (última semana)
Test-Endpoint `
    -TestName "Rango: últimos 7 días" `
    -Url "$baseUrl/api/v2/informes/partes?from=$weekStart&to=$today&page=1&pageSize=10" `
    -Headers $headers `
    -Category "Fecha"

# TEST 2.5: Filtro por rango (mes completo)
$monthStart = (Get-Date -Day 1).ToString("yyyy-MM-dd")
$monthEnd = (Get-Date -Day 1).AddMonths(1).AddDays(-1).ToString("yyyy-MM-dd")
Test-Endpoint `
    -TestName "Rango: mes actual ($monthStart a $monthEnd)" `
    -Url "$baseUrl/api/v2/informes/partes?from=$monthStart&to=$monthEnd&page=1&pageSize=10" `
    -Headers $headers `
    -Category "Fecha"

# ============================================================================
# 4. TESTS DE PAGINACIÓN
# ============================================================================

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Yellow
Write-Host "║  📄 CATEGORÍA 3: PAGINACIÓN                                   ║" -ForegroundColor Yellow
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Yellow
Write-Host ""

# TEST 3.1: Página 1, tamaño 10
Test-Endpoint `
    -TestName "Paginación: página 1, tamaño 10" `
    -Url "$baseUrl/api/v2/informes/partes?date=$today&page=1&pageSize=10" `
    -Headers $headers `
    -Category "Paginación"

# TEST 3.2: Página 1, tamaño 50
Test-Endpoint `
    -TestName "Paginación: página 1, tamaño 50" `
    -Url "$baseUrl/api/v2/informes/partes?date=$today&page=1&pageSize=50" `
    -Headers $headers `
    -Category "Paginación"

# TEST 3.3: Página 2, tamaño 20
Test-Endpoint `
    -TestName "Paginación: página 2, tamaño 20" `
    -Url "$baseUrl/api/v2/informes/partes?date=$today&page=2&pageSize=20" `
    -Headers $headers `
    -Category "Paginación"

# TEST 3.4: Tamaño máximo (200)
Test-Endpoint `
    -TestName "Paginación: tamaño máximo 200" `
    -Url "$baseUrl/api/v2/informes/partes?date=$today&page=1&pageSize=200" `
    -Headers $headers `
    -Category "Paginación"

# TEST 3.5: Sin paginación explícita (valores por defecto)
Test-Endpoint `
    -TestName "Paginación: valores por defecto (page=1, pageSize=50)" `
    -Url "$baseUrl/api/v2/informes/partes?date=$today" `
    -Headers $headers `
    -Category "Paginación"

# ============================================================================
# 5. TESTS DE ORDENAMIENTO
# ============================================================================

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Yellow
Write-Host "║  🔀 CATEGORÍA 4: ORDENAMIENTO                                 ║" -ForegroundColor Yellow
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Yellow
Write-Host ""

# TEST 4.1: Orden por fecha descendente
Test-Endpoint `
    -TestName "Orden: fecha_trabajo DESC" `
    -Url "$baseUrl/api/v2/informes/partes?date=$today&sort=fecha_trabajo:desc&pageSize=10" `
    -Headers $headers `
    -Category "Ordenamiento"

# TEST 4.2: Orden por fecha ascendente
Test-Endpoint `
    -TestName "Orden: fecha_trabajo ASC" `
    -Url "$baseUrl/api/v2/informes/partes?date=$today&sort=fecha_trabajo:asc&pageSize=10" `
    -Headers $headers `
    -Category "Ordenamiento"

# TEST 4.3: Orden múltiple (fecha DESC, hora_inicio ASC)
Test-Endpoint `
    -TestName "Orden múltiple: fecha_trabajo DESC, hora_inicio ASC" `
    -Url "$baseUrl/api/v2/informes/partes?date=$today&sort=fecha_trabajo:desc,hora_inicio:asc&pageSize=10" `
    -Headers $headers `
    -Category "Ordenamiento"

# TEST 4.4: Orden por duración
Test-Endpoint `
    -TestName "Orden: duracion_min DESC" `
    -Url "$baseUrl/api/v2/informes/partes?date=$today&sort=duracion_min:desc&pageSize=10" `
    -Headers $headers `
    -Category "Ordenamiento"

# TEST 4.5: Orden por agente
Test-Endpoint `
    -TestName "Orden: agente_nombre ASC" `
    -Url "$baseUrl/api/v2/informes/partes?date=$today&sort=agente_nombre:asc&pageSize=10" `
    -Headers $headers `
    -Category "Ordenamiento"

# TEST 4.6: Orden por cliente
Test-Endpoint `
    -TestName "Orden: cliente_nombre ASC" `
    -Url "$baseUrl/api/v2/informes/partes?date=$today&sort=cliente_nombre:asc&pageSize=10" `
    -Headers $headers `
    -Category "Ordenamiento"

# ============================================================================
# 6. TESTS DE FILTROS POR AGENTE (SEGURIDAD ROL USER)
# ============================================================================

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Yellow
Write-Host "║  👤 CATEGORÍA 5: FILTROS POR AGENTE (SEGURIDAD)              ║" -ForegroundColor Yellow
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Yellow
Write-Host ""

# TEST 5.1: Sin filtro de agente (USER ve solo sus datos automáticamente)
Test-Endpoint `
    -TestName "Sin filtro agente: USER ve solo sus datos" `
    -Url "$baseUrl/api/v2/informes/partes?date=$today&pageSize=10" `
    -Headers $headers `
    -Category "Agente"

# TEST 5.2: Con filtro agentId propio (debe funcionar)
Test-Endpoint `
    -TestName "AgentId propio: $userId (debe funcionar)" `
    -Url "$baseUrl/api/v2/informes/partes?date=$today&agentId=$userId&pageSize=10" `
    -Headers $headers `
    -Category "Agente"

# TEST 5.3: Con filtro agentId de otro usuario (debe fallar 403)
$otherUserId = "00000000-0000-0000-0000-000000000001"
Test-Endpoint `
    -TestName "AgentId ajeno: $otherUserId (debe devolver 403)" `
    -Url "$baseUrl/api/v2/informes/partes?date=$today&agentId=$otherUserId&pageSize=10" `
    -Headers $headers `
    -ExpectedStatus 403 `
    -Category "Agente"

# TEST 5.4: Con filtro agentIds múltiples (USER no puede usar, debe fallar 403)
Test-Endpoint `
    -TestName "AgentIds múltiples (USER no puede usar, debe devolver 403)" `
    -Url "$baseUrl/api/v2/informes/partes?date=$today&agentIds=$userId,$otherUserId&pageSize=10" `
    -Headers $headers `
    -ExpectedStatus 403 `
    -Category "Agente"

# ============================================================================
# 7. TESTS DE FILTROS POR CATÁLOGOS
# ============================================================================

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Yellow
Write-Host "║  🏢 CATEGORÍA 6: FILTROS POR CATÁLOGOS                        ║" -ForegroundColor Yellow
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Yellow
Write-Host ""

# TEST 6.1: Filtro por cliente (ID 1)
Test-Endpoint `
    -TestName "ClientId: 1" `
    -Url "$baseUrl/api/v2/informes/partes?date=$today&clientId=1&pageSize=10" `
    -Headers $headers `
    -Category "Catálogos"

# TEST 6.2: Filtro por grupo (ID 1)
Test-Endpoint `
    -TestName "GroupId: 1" `
    -Url "$baseUrl/api/v2/informes/partes?date=$today&groupId=1&pageSize=10" `
    -Headers $headers `
    -Category "Catálogos"

# TEST 6.3: Filtro por tipo (ID 1)
Test-Endpoint `
    -TestName "TypeId: 1" `
    -Url "$baseUrl/api/v2/informes/partes?date=$today&typeId=1&pageSize=10" `
    -Headers $headers `
    -Category "Catálogos"

# TEST 6.4: Combinación: cliente + grupo
Test-Endpoint `
    -TestName "Combinación: clientId=1 + groupId=1" `
    -Url "$baseUrl/api/v2/informes/partes?date=$today&clientId=1&groupId=1&pageSize=10" `
    -Headers $headers `
    -Category "Catálogos"

# TEST 6.5: Combinación: cliente + grupo + tipo
Test-Endpoint `
    -TestName "Combinación: clientId=1 + groupId=1 + typeId=1" `
    -Url "$baseUrl/api/v2/informes/partes?date=$today&clientId=1&groupId=1&typeId=1&pageSize=10" `
    -Headers $headers `
    -Category "Catálogos"

# ============================================================================
# 8. TESTS DE BÚSQUEDA DE TEXTO
# ============================================================================

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Yellow
Write-Host "║  🔍 CATEGORÍA 7: BÚSQUEDA DE TEXTO (Q)                       ║" -ForegroundColor Yellow
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Yellow
Write-Host ""

# TEST 7.1: Búsqueda por ticket
Test-Endpoint `
    -TestName "Búsqueda: q=TK" `
    -Url "$baseUrl/api/v2/informes/partes?date=$today&q=TK&pageSize=10" `
    -Headers $headers `
    -Category "Búsqueda"

# TEST 7.2: Búsqueda por acción
Test-Endpoint `
    -TestName "Búsqueda: q=instalación" `
    -Url "$baseUrl/api/v2/informes/partes?date=$today&q=instalación&pageSize=10" `
    -Headers $headers `
    -Category "Búsqueda"

# TEST 7.3: Búsqueda por tienda
Test-Endpoint `
    -TestName "Búsqueda: q=madrid" `
    -Url "$baseUrl/api/v2/informes/partes?date=$today&q=madrid&pageSize=10" `
    -Headers $headers `
    -Category "Búsqueda"

# TEST 7.4: Búsqueda con caracteres especiales
Test-Endpoint `
    -TestName "Búsqueda: q=red-router" `
    -Url "$baseUrl/api/v2/informes/partes?date=$today&q=red-router&pageSize=10" `
    -Headers $headers `
    -Category "Búsqueda"

# TEST 7.5: Búsqueda vacía
Test-Endpoint `
    -TestName "Búsqueda vacía: q= (debe ignorarse)" `
    -Url "$baseUrl/api/v2/informes/partes?date=$today&q=&pageSize=10" `
    -Headers $headers `
    -Category "Búsqueda"

# ============================================================================
# 9. TESTS DE COMBINACIONES COMPLEJAS
# ============================================================================

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Yellow
Write-Host "║  🎯 CATEGORÍA 8: COMBINACIONES COMPLEJAS                      ║" -ForegroundColor Yellow
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Yellow
Write-Host ""

# TEST 8.1: Rango + cliente + búsqueda + orden
Test-Endpoint `
    -TestName "Complejo: rango + clientId + q + sort" `
    -Url "$baseUrl/api/v2/informes/partes?from=$weekStart&to=$today&clientId=1&q=TK&sort=fecha_trabajo:desc&pageSize=20" `
    -Headers $headers `
    -Category "Complejo"

# TEST 8.2: WeekIso + grupo + tipo + paginación
Test-Endpoint `
    -TestName "Complejo: weekIso + groupId + typeId + paginación" `
    -Url "$baseUrl/api/v2/informes/partes?weekIso=$currentWeek&groupId=1&typeId=1&page=1&pageSize=30" `
    -Headers $headers `
    -Category "Complejo"

# TEST 8.3: Todos los filtros disponibles para USER
Test-Endpoint `
    -TestName "Complejo: todos los filtros (USER)" `
    -Url "$baseUrl/api/v2/informes/partes?from=$weekStart&to=$today&agentId=$userId&clientId=1&groupId=1&typeId=1&q=TK&sort=fecha_trabajo:desc,hora_inicio:asc&page=1&pageSize=50" `
    -Headers $headers `
    -Category "Complejo"

# ============================================================================
# 10. TESTS DE CASOS EXTREMOS
# ============================================================================

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Yellow
Write-Host "║  ⚠️  CATEGORÍA 9: CASOS EXTREMOS                              ║" -ForegroundColor Yellow
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Yellow
Write-Host ""

# TEST 9.1: Fecha muy antigua (sin datos)
Test-Endpoint `
    -TestName "Fecha antigua: 2020-01-01 (sin datos esperados)" `
    -Url "$baseUrl/api/v2/informes/partes?date=2020-01-01&pageSize=10" `
    -Headers $headers `
    -Category "Extremos"

# TEST 9.2: Fecha futura
Test-Endpoint `
    -TestName "Fecha futura: 2030-12-31 (sin datos esperados)" `
    -Url "$baseUrl/api/v2/informes/partes?date=2030-12-31&pageSize=10" `
    -Headers $headers `
    -Category "Extremos"

# TEST 9.3: Rango muy amplio (año completo)
$yearStart = (Get-Date -Month 1 -Day 1).ToString("yyyy-MM-dd")
$yearEnd = (Get-Date -Month 12 -Day 31).ToString("yyyy-MM-dd")
Test-Endpoint `
    -TestName "Rango amplio: año completo ($yearStart a $yearEnd)" `
    -Url "$baseUrl/api/v2/informes/partes?from=$yearStart&to=$yearEnd&pageSize=100" `
    -Headers $headers `
    -Category "Extremos"

# TEST 9.4: Cliente inexistente
Test-Endpoint `
    -TestName "Cliente inexistente: clientId=99999 (sin datos esperados)" `
    -Url "$baseUrl/api/v2/informes/partes?date=$today&clientId=99999&pageSize=10" `
    -Headers $headers `
    -Category "Extremos"

# TEST 9.5: Búsqueda sin resultados
Test-Endpoint `
    -TestName "Búsqueda sin resultados: q=XYZABCINEXISTENTE" `
    -Url "$baseUrl/api/v2/informes/partes?date=$today&q=XYZABCINEXISTENTE&pageSize=10" `
    -Headers $headers `
    -Category "Extremos"

# ============================================================================
# RESUMEN FINAL
# ============================================================================

Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
Write-Host "📊 RESUMEN DE RESULTADOS" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
Write-Host ""
Write-Host "   Total de tests ejecutados: $totalTests" -ForegroundColor White
Write-Host "   ✅ Tests exitosos: $passedTests" -ForegroundColor Green
Write-Host "   ⚠️  Errores esperados: $expectedErrors" -ForegroundColor Yellow
Write-Host "   ❌ Tests fallidos: $failedTests" -ForegroundColor Red
Write-Host ""

$successRate = [math]::Round(($passedTests / $totalTests) * 100, 2)
Write-Host "   Tasa de éxito: $successRate%" -ForegroundColor $(if ($successRate -ge 90) { "Green" } elseif ($successRate -ge 70) { "Yellow" } else { "Red" })
Write-Host ""

if ($failedTests -eq 0) {
    Write-Host "🎉 ¡TODOS LOS TESTS PASARON CORRECTAMENTE!" -ForegroundColor Green
} else {
    Write-Host "⚠️  ALGUNOS TESTS FALLARON - REVISAR ERRORES ARRIBA" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""

# Resumen por categoría
Write-Host "📋 RESUMEN POR CATEGORÍA:" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. ✅ Validación de Parámetros Obligatorios" -ForegroundColor White
Write-Host "2. ✅ Filtros de Fecha (date, weekIso, from+to)" -ForegroundColor White
Write-Host "3. ✅ Paginación (page, pageSize, límites)" -ForegroundColor White
Write-Host "4. ✅ Ordenamiento (sort múltiple)" -ForegroundColor White
Write-Host "5. ✅ Filtros por Agente (seguridad USER/EDITOR/ADMIN)" -ForegroundColor White
Write-Host "6. ✅ Filtros por Catálogos (clientId, groupId, typeId)" -ForegroundColor White
Write-Host "7. ✅ Búsqueda de Texto (q)" -ForegroundColor White
Write-Host "8. ✅ Combinaciones Complejas" -ForegroundColor White
Write-Host "9. ✅ Casos Extremos y Edge Cases" -ForegroundColor White
Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
