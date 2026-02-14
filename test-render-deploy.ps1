#!/usr/bin/env pwsh
#
# Test script para Render deployment de GestionTime API
# Uso: .\test-render-deploy.ps1 -BaseUrl "https://tu-servicio.onrender.com"
#

param(
    [Parameter(Mandatory=$false)]
    [string]$BaseUrl = "https://gestiontime-api.onrender.com",
    
    [Parameter(Mandatory=$false)]
    [string]$Email = "admin@test.com",
    
    [Parameter(Mandatory=$false)]
    [string]$Password = "Admin123!"
)

$ErrorActionPreference = "Stop"

Write-Host @"
═══════════════════════════════════════════════════════════════════
 🚀 RENDER DEPLOYMENT TEST
═══════════════════════════════════════════════════════════════════
 Base URL: $BaseUrl
 Email:    $Email
═══════════════════════════════════════════════════════════════════
"@ -ForegroundColor Cyan

$testsPassed = 0
$testsFailed = 0

function Test-Endpoint {
    param(
        [string]$Name,
        [scriptblock]$Test
    )
    
    Write-Host "`n[$Name]" -ForegroundColor Yellow -NoNewline
    try {
        & $Test
        Write-Host " ✅ PASS" -ForegroundColor Green
        $script:testsPassed++
        return $true
    } catch {
        Write-Host " ❌ FAIL" -ForegroundColor Red
        Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
        $script:testsFailed++
        return $false
    }
}

# ═══════════════════════════════════════════════════════════════════
# TEST 1: Health Check
# ═══════════════════════════════════════════════════════════════════
Test-Endpoint "1️⃣ Health Check" {
    $health = Invoke-RestMethod -Uri "$BaseUrl/health" -TimeoutSec 10
    
    if ($health.status -ne "Healthy") {
        throw "Expected status 'Healthy', got '$($health.status)'"
    }
    
    Write-Host "  → Status: $($health.status)"
    Write-Host "  → Timestamp: $($health.timestamp)"
}

# ═══════════════════════════════════════════════════════════════════
# TEST 2: Swagger UI
# ═══════════════════════════════════════════════════════════════════
Test-Endpoint "2️⃣ Swagger UI" {
    $swagger = Invoke-WebRequest -Uri "$BaseUrl/swagger/index.html" -TimeoutSec 10
    
    if ($swagger.StatusCode -ne 200) {
        throw "Expected 200, got $($swagger.StatusCode)"
    }
    
    Write-Host "  → Swagger available at $BaseUrl/swagger"
}

# ═══════════════════════════════════════════════════════════════════
# TEST 3: Login Desktop (JWT)
# ═══════════════════════════════════════════════════════════════════
$global:token = $null

Test-Endpoint "3️⃣ Login JWT" {
    $loginBody = @{
        email = $Email
        password = $Password
    } | ConvertTo-Json

    $login = Invoke-RestMethod -Uri "$BaseUrl/api/v1/auth/login-desktop" `
        -Method POST `
        -ContentType "application/json" `
        -Body $loginBody `
        -TimeoutSec 10

    if ([string]::IsNullOrEmpty($login.token)) {
        throw "Token is null or empty"
    }
    
    $global:token = $login.token
    
    Write-Host "  → User: $($login.user.name)"
    Write-Host "  → Role: $($login.user.role)"
    Write-Host "  → Token: $($login.token.Substring(0, 30))..."
    Write-Host "  → Expires: $($login.expiresAt)"
}

# ═══════════════════════════════════════════════════════════════════
# TEST 4: /api/v2/informes/partes (Fecha específica)
# ═══════════════════════════════════════════════════════════════════
Test-Endpoint "4️⃣ GET /api/v2/informes/partes (date)" {
    if ([string]::IsNullOrEmpty($global:token)) {
        throw "Token not available (login failed?)"
    }
    
    $headers = @{ "Authorization" = "Bearer $global:token" }
    $today = (Get-Date).ToString("yyyy-MM-dd")
    
    $partes = Invoke-RestMethod -Uri "$BaseUrl/api/v2/informes/partes?date=$today&pageSize=10" `
        -Headers $headers `
        -TimeoutSec 15

    if ($null -eq $partes.total) {
        throw "Response missing 'total' field"
    }
    
    Write-Host "  → Total: $($partes.total)"
    Write-Host "  → Page: $($partes.page) / PageSize: $($partes.pageSize)"
    Write-Host "  → Items: $($partes.items.Count)"
}

# ═══════════════════════════════════════════════════════════════════
# TEST 5: /api/v2/informes/partes (Semana ISO)
# ═══════════════════════════════════════════════════════════════════
Test-Endpoint "5️⃣ GET /api/v2/informes/partes (weekIso)" {
    $headers = @{ "Authorization" = "Bearer $global:token" }
    
    # Calcular semana ISO actual
    $date = Get-Date
    $weekIso = "$($date.Year)-W$(Get-Date $date -UFormat %V)"
    
    $partes = Invoke-RestMethod -Uri "$BaseUrl/api/v2/informes/partes?weekIso=$weekIso&pageSize=20" `
        -Headers $headers `
        -TimeoutSec 15

    Write-Host "  → WeekIso: $weekIso"
    Write-Host "  → Total: $($partes.total)"
}

# ═══════════════════════════════════════════════════════════════════
# TEST 6: /api/v2/informes/partes (Rango de fechas)
# ═══════════════════════════════════════════════════════════════════
Test-Endpoint "6️⃣ GET /api/v2/informes/partes (from+to)" {
    $headers = @{ "Authorization" = "Bearer $global:token" }
    
    $to = (Get-Date).ToString("yyyy-MM-dd")
    $from = (Get-Date).AddDays(-7).ToString("yyyy-MM-dd")
    
    $partes = Invoke-RestMethod -Uri "$BaseUrl/api/v2/informes/partes?from=$from&to=$to&pageSize=50" `
        -Headers $headers `
        -TimeoutSec 15

    Write-Host "  → Range: $from to $to"
    Write-Host "  → Total: $($partes.total)"
}

# ═══════════════════════════════════════════════════════════════════
# TEST 7: /api/v2/informes/partes (Búsqueda + Filtros)
# ═══════════════════════════════════════════════════════════════════
Test-Endpoint "7️⃣ GET /api/v2/informes/partes (search+filters)" {
    $headers = @{ "Authorization" = "Bearer $global:token" }
    $today = (Get-Date).ToString("yyyy-MM-dd")
    
    $partes = Invoke-RestMethod -Uri "$BaseUrl/api/v2/informes/partes?date=$today&q=instalación&sort=duracion_min:desc&pageSize=10" `
        -Headers $headers `
        -TimeoutSec 15

    Write-Host "  → Query: 'instalación'"
    Write-Host "  → Sort: duracion_min:desc"
    Write-Host "  → Results: $($partes.total)"
}

# ═══════════════════════════════════════════════════════════════════
# TEST 8: /api/v2/informes/resumen (scope=day)
# ═══════════════════════════════════════════════════════════════════
Test-Endpoint "8️⃣ GET /api/v2/informes/resumen (day)" {
    $headers = @{ "Authorization" = "Bearer $global:token" }
    $today = (Get-Date).ToString("yyyy-MM-dd")
    
    $resumen = Invoke-RestMethod -Uri "$BaseUrl/api/v2/informes/resumen?scope=day&date=$today" `
        -Headers $headers `
        -TimeoutSec 15

    if ($null -eq $resumen.partsCount) {
        throw "Response missing 'partsCount' field"
    }
    
    Write-Host "  → Parts: $($resumen.partsCount)"
    Write-Host "  → Recorded: $($resumen.recordedMinutes) min"
    Write-Host "  → Covered: $($resumen.coveredMinutes) min"
    Write-Host "  → Overlap: $($resumen.overlapMinutes) min"
}

# ═══════════════════════════════════════════════════════════════════
# TEST 9: /api/v2/informes/resumen (scope=week)
# ═══════════════════════════════════════════════════════════════════
Test-Endpoint "9️⃣ GET /api/v2/informes/resumen (week)" {
    $headers = @{ "Authorization" = "Bearer $global:token" }
    
    $date = Get-Date
    $weekIso = "$($date.Year)-W$(Get-Date $date -UFormat %V)"
    
    $resumen = Invoke-RestMethod -Uri "$BaseUrl/api/v2/informes/resumen?scope=week&weekIso=$weekIso" `
        -Headers $headers `
        -TimeoutSec 15

    Write-Host "  → WeekIso: $weekIso"
    Write-Host "  → Parts: $($resumen.partsCount)"
    Write-Host "  → Daily summaries: $($resumen.byDay.Count)"
}

# ═══════════════════════════════════════════════════════════════════
# TEST 10: /api/v2/informes/resumen (scope=range)
# ═══════════════════════════════════════════════════════════════════
Test-Endpoint "🔟 GET /api/v2/informes/resumen (range)" {
    $headers = @{ "Authorization" = "Bearer $global:token" }
    
    $to = (Get-Date).ToString("yyyy-MM-dd")
    $from = (Get-Date).AddDays(-30).ToString("yyyy-MM-dd")
    
    $resumen = Invoke-RestMethod -Uri "$BaseUrl/api/v2/informes/resumen?scope=range&from=$from&to=$to" `
        -Headers $headers `
        -TimeoutSec 20

    Write-Host "  → Range: $from to $to"
    Write-Host "  → Parts: $($resumen.partsCount)"
    Write-Host "  → Gaps: $($resumen.gaps.Count)"
    Write-Host "  → Merged Intervals: $($resumen.mergedIntervals.Count)"
}

# ═══════════════════════════════════════════════════════════════════
# TEST 11: Error Handling (400 Bad Request)
# ═══════════════════════════════════════════════════════════════════
Test-Endpoint "1️⃣1️⃣ Error Handling (400)" {
    $headers = @{ "Authorization" = "Bearer $global:token" }
    
    try {
        Invoke-RestMethod -Uri "$BaseUrl/api/v2/informes/partes?pageSize=10" -Headers $headers -TimeoutSec 10
        throw "Expected 400 error, got 200 OK"
    } catch {
        if ($_.Exception.Response.StatusCode -ne 400) {
            throw "Expected 400, got $($_.Exception.Response.StatusCode)"
        }
        Write-Host "  → 400 Bad Request (expected)"
    }
}

# ═══════════════════════════════════════════════════════════════════
# TEST 12: Error Handling (401 Unauthorized)
# ═══════════════════════════════════════════════════════════════════
Test-Endpoint "1️⃣2️⃣ Error Handling (401)" {
    $badHeaders = @{ "Authorization" = "Bearer invalid-token-xyz" }
    $today = (Get-Date).ToString("yyyy-MM-dd")
    
    try {
        Invoke-RestMethod -Uri "$BaseUrl/api/v2/informes/partes?date=$today&pageSize=10" -Headers $badHeaders -TimeoutSec 10
        throw "Expected 401 error, got 200 OK"
    } catch {
        if ($_.Exception.Response.StatusCode -ne 401) {
            throw "Expected 401, got $($_.Exception.Response.StatusCode)"
        }
        Write-Host "  → 401 Unauthorized (expected)"
    }
}

# ═══════════════════════════════════════════════════════════════════
# RESUMEN FINAL
# ═══════════════════════════════════════════════════════════════════
Write-Host @"

═══════════════════════════════════════════════════════════════════
 📊 RESUMEN DE TESTS
═══════════════════════════════════════════════════════════════════
"@ -ForegroundColor Cyan

$total = $testsPassed + $testsFailed
$successRate = if ($total -gt 0) { [math]::Round(($testsPassed / $total) * 100, 2) } else { 0 }

Write-Host "  Total:      $total tests" -ForegroundColor White
Write-Host "  ✅ Passed:  $testsPassed" -ForegroundColor Green
Write-Host "  ❌ Failed:  $testsFailed" -ForegroundColor $(if ($testsFailed -eq 0) { "Green" } else { "Red" })
Write-Host "  📈 Success: $successRate%" -ForegroundColor $(if ($successRate -eq 100) { "Green" } elseif ($successRate -gt 80) { "Yellow" } else { "Red" })

Write-Host @"
═══════════════════════════════════════════════════════════════════
"@ -ForegroundColor Cyan

if ($testsFailed -eq 0) {
    Write-Host "`n🎉 All tests passed! Deploy is SUCCESSFUL." -ForegroundColor Green
    Write-Host "`n📋 Next steps:" -ForegroundColor Cyan
    Write-Host "  1. Share URL with team: $BaseUrl" -ForegroundColor White
    Write-Host "  2. Update desktop app config with production URL" -ForegroundColor White
    Write-Host "  3. Setup monitoring and alerts in Render Dashboard" -ForegroundColor White
    Write-Host "  4. Configure automatic backups for PostgreSQL" -ForegroundColor White
    exit 0
} else {
    Write-Host "`n❌ Some tests failed. Review errors above." -ForegroundColor Red
    Write-Host "`n🔍 Troubleshooting tips:" -ForegroundColor Yellow
    Write-Host "  - Check Render logs: $BaseUrl (Dashboard → Logs)" -ForegroundColor White
    Write-Host "  - Verify environment variables in Render Dashboard" -ForegroundColor White
    Write-Host "  - Ensure PostgreSQL database is running and accessible" -ForegroundColor White
    Write-Host "  - Check docs/RENDER_DEPLOY_GUIDE.md for troubleshooting" -ForegroundColor White
    exit 1
}
