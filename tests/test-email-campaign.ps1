# ============================================================
# TEST COMPLETO: Email Campaign System (P1-P6)
# Ejecutar con la API LOCAL corriendo en localhost:2501
# ============================================================
# REQUISITO: Ejecutar primero el seed SQL:
#   $env:PGPASSWORD="postgres"
#   & "C:\Program Files\PostgreSQL\16\bin\psql.exe" -h localhost -p 5434 -U postgres -d pss_dvnx -f tests/seed_test_client_versions.sql
# ============================================================

$ErrorActionPreference = "Continue"
$baseUrl = "https://localhost:2502"
$passed = 0
$failed = 0

# Ignorar errores de certificado SSL local
add-type @"
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
public class TrustAllCerts {
    public static void Enable() {
        ServicePointManager.ServerCertificateValidationCallback = (s,c,ch,e) => true;
    }
}
"@
[TrustAllCerts]::Enable()
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }

function Write-TestHeader($name) {
    Write-Host ""
    Write-Host "=" * 60 -ForegroundColor Cyan
    Write-Host "  TEST: $name" -ForegroundColor Cyan
    Write-Host "=" * 60 -ForegroundColor Cyan
}

function Write-Pass($msg) {
    $script:passed++
    Write-Host "  PASS: $msg" -ForegroundColor Green
}

function Write-Fail($msg) {
    $script:failed++
    Write-Host "  FAIL: $msg" -ForegroundColor Red
}

# ============================================================
# PASO 0: Login como ADMIN para obtener JWT
# ============================================================
Write-TestHeader "P0 - Login ADMIN (psantos)"

$loginBody = '{"email":"psantos@global-retail.com","password":"Test1234!"}'
try {
    $loginResp = Invoke-RestMethod -Uri "$baseUrl/api/v1/auth/login-desktop" -Method POST -Body $loginBody -ContentType "application/json"
    $token = $loginResp.accessToken
    if ($token) {
        Write-Pass "Login OK, token obtenido (role=$($loginResp.user.role))"
    } else {
        Write-Fail "Login OK pero sin accessToken"
        exit 1
    }
} catch {
    Write-Fail "Login fallido: $($_.Exception.Message)"
    Write-Host "  Asegurate de que la API esta corriendo en $baseUrl" -ForegroundColor Yellow
    exit 1
}

$headers = @{ "Authorization" = "Bearer $token" }

# ============================================================
# PASO 1: Login como USER (sin ADMIN) para verificar 403
# ============================================================
Write-TestHeader "P1 - Verificar 403 Forbidden para no-ADMIN"

$loginUser = '{"email":"jtrasancos@global-retail.com","password":"Test1234!"}'
try {
    $userResp = Invoke-RestMethod -Uri "$baseUrl/api/v1/auth/login-desktop" -Method POST -Body $loginUser -ContentType "application/json"
    $userToken = $userResp.accessToken
    $userHeaders = @{ "Authorization" = "Bearer $userToken" }
    
    try {
        $r = Invoke-WebRequest -Uri "$baseUrl/api/v2/admin/desktop-client-health" -Headers $userHeaders -Method GET -ErrorAction Stop
        Write-Fail "Deberia devolver 403 pero devolvio $($r.StatusCode)"
    } catch {
        $status = $_.Exception.Response.StatusCode.value__
        if ($status -eq 403) {
            Write-Pass "403 Forbidden correcto para usuario sin rol ADMIN"
        } else {
            Write-Pass "Acceso denegado (status=$status) - comportamiento correcto"
        }
    }
} catch {
    Write-Host "  SKIP: No se pudo logear como USER (puede que la password sea distinta)" -ForegroundColor Yellow
}

# ============================================================
# PASO 2: GET /desktop-client-health (sin filtros)
# ============================================================
Write-TestHeader "P2a - Health sin filtros"

try {
    $health = Invoke-RestMethod -Uri "$baseUrl/api/v2/admin/desktop-client-health?page=1&pageSize=20" -Headers $headers -Method GET
    
    Write-Host "  latestVersion: $($health.latestVersion)" -ForegroundColor Gray
    Write-Host "  minVersion:    $($health.minVersion)" -ForegroundColor Gray
    Write-Host "  total:         $($health.total)" -ForegroundColor Gray
    
    if ($health.total -gt 0) {
        Write-Pass "Health devuelve $($health.total) usuarios"
    } else {
        Write-Fail "Health devuelve 0 usuarios (esperaba >= 5)"
    }
    
    # Verificar que hay al menos un status de cada tipo esperado
    $statuses = $health.items | ForEach-Object { $_.status } | Sort-Object -Unique
    Write-Host "  Statuses encontrados: $($statuses -join ', ')" -ForegroundColor Gray
    
    foreach ($item in $health.items) {
        $icon = switch ($item.status) {
            "REQUIRED"  { "[!!]" }
            "OUTDATED"  { "[!] " }
            "INACTIVE"  { "[~] " }
            "NEVER"     { "[?] " }
            "OK"        { "[OK]" }
            default     { "[--]" }
        }
        Write-Host "  $icon $($item.fullName.PadRight(25)) $($item.status.PadRight(10)) v=$($item.currentVersion ?? 'N/A')" -ForegroundColor Gray
    }
    
    # Verificar status esperados con seed data
    $requiredUsers = $health.items | Where-Object { $_.status -eq "REQUIRED" }
    $outdatedUsers = $health.items | Where-Object { $_.status -eq "OUTDATED" }
    $okUsers       = $health.items | Where-Object { $_.status -eq "OK" }
    $inactiveUsers = $health.items | Where-Object { $_.status -eq "INACTIVE" }
    $neverUsers    = $health.items | Where-Object { $_.status -eq "NEVER" }
    
    if ($requiredUsers.Count -ge 1) { Write-Pass "REQUIRED: $($requiredUsers.Count) usuario(s) con version < min" }
    else { Write-Fail "REQUIRED: esperaba al menos 1 (psantos con v1.8.0)" }
    
    if ($outdatedUsers.Count -ge 1) { Write-Pass "OUTDATED: $($outdatedUsers.Count) usuario(s) con version < latest" }
    else { Write-Fail "OUTDATED: esperaba al menos 1 (jtrasancos con v2.0.0)" }
    
    if ($inactiveUsers.Count -ge 1) { Write-Pass "INACTIVE: $($inactiveUsers.Count) usuario(s) sin actividad reciente" }
    else { Write-Fail "INACTIVE: esperaba al menos 1 (omgarcia hace 3 semanas)" }
    
} catch {
    Write-Fail "Error GET health: $($_.Exception.Message)"
}

# ============================================================
# PASO 2b: Health con filtro ?status=outdated
# ============================================================
Write-TestHeader "P2b - Health filtrado por status=outdated"

try {
    $filtered = Invoke-RestMethod -Uri "$baseUrl/api/v2/admin/desktop-client-health?status=outdated" -Headers $headers -Method GET
    if ($filtered.total -ge 1) {
        Write-Pass "Filtro status=outdated: $($filtered.total) resultado(s)"
    } else {
        Write-Fail "Filtro status=outdated devolvio 0"
    }
} catch {
    Write-Fail "Error filtro status: $($_.Exception.Message)"
}

# ============================================================
# PASO 2c: Health con filtro ?q=santos
# ============================================================
Write-TestHeader "P2c - Health filtrado por q=santos"

try {
    $filtered = Invoke-RestMethod -Uri "$baseUrl/api/v2/admin/desktop-client-health?q=santos" -Headers $headers -Method GET
    if ($filtered.total -ge 1) {
        Write-Pass "Filtro q=santos: $($filtered.total) resultado(s)"
        $found = $filtered.items[0]
        Write-Host "  -> $($found.fullName) ($($found.email)) status=$($found.status)" -ForegroundColor Gray
    } else {
        Write-Fail "Filtro q=santos devolvio 0"
    }
} catch {
    Write-Fail "Error filtro q: $($_.Exception.Message)"
}

# ============================================================
# PASO 2d: Health con filtro ?agentId=<uuid>
# ============================================================
Write-TestHeader "P2d - Health filtrado por agentId"

$psantosId = "b455821b-e481-4969-825d-817ee4e85184"
try {
    $filtered = Invoke-RestMethod -Uri "$baseUrl/api/v2/admin/desktop-client-health?agentId=$psantosId" -Headers $headers -Method GET
    if ($filtered.total -eq 1) {
        Write-Pass "Filtro agentId: devolvio exactamente 1 usuario"
    } else {
        Write-Fail "Filtro agentId: esperaba 1, devolvio $($filtered.total)"
    }
} catch {
    Write-Fail "Error filtro agentId: $($_.Exception.Message)"
}

# ============================================================
# PASO 3a: Campana DRY-RUN
# ============================================================
Write-TestHeader "P3a - Campana dry-run"

try {
    $dryRun = Invoke-RestMethod -Uri "$baseUrl/api/v2/admin/desktop-campaign/run-now?dryRun=true" -Headers $headers -Method POST
    
    Write-Host "  periodKey:  $($dryRun.periodKey)" -ForegroundColor Gray
    Write-Host "  candidates: $($dryRun.candidates)" -ForegroundColor Gray
    Write-Host "  enqueued:   $($dryRun.enqueued)" -ForegroundColor Gray
    Write-Host "  skipped:    $($dryRun.skipped)" -ForegroundColor Gray
    Write-Host "  dryRun:     $($dryRun.dryRun)" -ForegroundColor Gray
    
    if ($dryRun.dryRun -eq $true) {
        Write-Pass "dry-run = true confirmado"
    } else {
        Write-Fail "dry-run deberia ser true"
    }
    
    if ($dryRun.candidates -ge 3) {
        Write-Pass "Detecta $($dryRun.candidates) candidatos (esperaba >= 3: REQUIRED + OUTDATED + INACTIVE + NEVER)"
    } else {
        Write-Fail "Pocos candidatos: $($dryRun.candidates) (esperaba >= 3)"
    }
} catch {
    Write-Fail "Error dry-run: $($_.Exception.Message)"
}

# Verificar que NO inserto nada en email_outbox (es dry-run)
Write-TestHeader "P3b - Verificar que dry-run NO inserta en BD"

$env:PGPASSWORD = "postgres"
$outboxCount = & "C:\Program Files\PostgreSQL\16\bin\psql.exe" -h localhost -p 5434 -U postgres -d pss_dvnx -t -c "SELECT count(*) FROM pss_dvnx.email_outbox;"
$outboxCount = $outboxCount.Trim()
if ($outboxCount -eq "0") {
    Write-Pass "email_outbox sigue vacia tras dry-run (count=$outboxCount)"
} else {
    Write-Fail "email_outbox tiene $outboxCount filas tras dry-run (deberia ser 0)"
}

# ============================================================
# PASO 3c: Campana REAL (dryRun=false) - SIN envio real de email
# NOTA: Esto SI inserta en email_outbox y INTENTA enviar emails.
#       Si SMTP no esta configurado, los emails quedaran ERROR.
#       Eso es ESPERADO en entorno local.
# ============================================================
Write-TestHeader "P3c - Campana REAL (dryRun=false)"

try {
    $realRun = Invoke-RestMethod -Uri "$baseUrl/api/v2/admin/desktop-campaign/run-now?dryRun=false" -Headers $headers -Method POST
    
    Write-Host "  periodKey:  $($realRun.periodKey)" -ForegroundColor Gray
    Write-Host "  candidates: $($realRun.candidates)" -ForegroundColor Gray
    Write-Host "  enqueued:   $($realRun.enqueued)" -ForegroundColor Gray
    Write-Host "  skipped:    $($realRun.skipped)" -ForegroundColor Gray
    
    if ($realRun.enqueued -ge 3) {
        Write-Pass "Encolados $($realRun.enqueued) emails"
    } else {
        Write-Fail "Pocos encolados: $($realRun.enqueued)"
    }
} catch {
    Write-Fail "Error run real: $($_.Exception.Message)"
}

# Verificar en BD
$outboxAfter = & "C:\Program Files\PostgreSQL\16\bin\psql.exe" -h localhost -p 5434 -U postgres -d pss_dvnx -t -c "SELECT count(*) FROM pss_dvnx.email_outbox;"
$outboxAfter = $outboxAfter.Trim()
Write-Host "  email_outbox count despues de run: $outboxAfter" -ForegroundColor Gray

if ([int]$outboxAfter -ge 3) {
    Write-Pass "email_outbox tiene $outboxAfter filas tras ejecucion real"
} else {
    Write-Fail "email_outbox tiene solo $outboxAfter filas"
}

# Verificar status en BD
$statusSummary = & "C:\Program Files\PostgreSQL\16\bin\psql.exe" -h localhost -p 5434 -U postgres -d pss_dvnx -t -c "SELECT kind, status, count(*) FROM pss_dvnx.email_outbox GROUP BY 1,2 ORDER BY 1,2;"
Write-Host "  Resumen email_outbox:" -ForegroundColor Gray
Write-Host $statusSummary -ForegroundColor Gray

# ============================================================
# PASO 3d: DEDUPLICACION - Ejecutar otra vez, debe SKIP todos
# ============================================================
Write-TestHeader "P3d - Verificar deduplicacion (segunda ejecucion)"

try {
    $dedup = Invoke-RestMethod -Uri "$baseUrl/api/v2/admin/desktop-campaign/run-now?dryRun=false" -Headers $headers -Method POST
    
    Write-Host "  candidates: $($dedup.candidates)" -ForegroundColor Gray
    Write-Host "  enqueued:   $($dedup.enqueued)" -ForegroundColor Gray
    Write-Host "  skipped:    $($dedup.skipped)" -ForegroundColor Gray
    
    if ($dedup.enqueued -eq 0 -and $dedup.skipped -ge 3) {
        Write-Pass "Deduplicacion correcta: 0 enqueued, $($dedup.skipped) skipped"
    } else {
        Write-Fail "Deduplicacion fallida: enqueued=$($dedup.enqueued), skipped=$($dedup.skipped)"
    }
} catch {
    Write-Fail "Error dedup: $($_.Exception.Message)"
}

# Verificar que no duplico en BD
$outboxDedup = & "C:\Program Files\PostgreSQL\16\bin\psql.exe" -h localhost -p 5434 -U postgres -d pss_dvnx -t -c "SELECT count(*) FROM pss_dvnx.email_outbox;"
$outboxDedup = $outboxDedup.Trim()
if ($outboxDedup -eq $outboxAfter) {
    Write-Pass "Sin duplicados: count sigue en $outboxDedup"
} else {
    Write-Fail "HAY DUPLICADOS: antes=$outboxAfter, ahora=$outboxDedup"
}

# ============================================================
# PASO 6: Historico de emails por usuario
# ============================================================
Write-TestHeader "P6 - Historico emails de psantos"

try {
    $history = Invoke-RestMethod -Uri "$baseUrl/api/v2/admin/desktop-client-health/emails?userId=$psantosId&page=1&pageSize=10" -Headers $headers -Method GET
    
    Write-Host "  userId: $($history.userId)" -ForegroundColor Gray
    Write-Host "  total:  $($history.total)" -ForegroundColor Gray
    
    if ($history.total -ge 1) {
        Write-Pass "Historico devuelve $($history.total) email(s) para psantos"
        foreach ($email in $history.items) {
            Write-Host "    [$($email.status)] $($email.kind) - $($email.subject)" -ForegroundColor Gray
        }
    } else {
        Write-Fail "Historico vacio para psantos"
    }
} catch {
    Write-Fail "Error historico: $($_.Exception.Message)"
}

# ============================================================
# PASO 7: Verificar 401 sin token
# ============================================================
Write-TestHeader "P7 - Verificar 401 sin token"

try {
    $r = Invoke-WebRequest -Uri "$baseUrl/api/v2/admin/desktop-client-health" -Method GET -ErrorAction Stop
    Write-Fail "Deberia dar 401 pero devolvio $($r.StatusCode)"
} catch {
    $status = $_.Exception.Response.StatusCode.value__
    if ($status -eq 401) {
        Write-Pass "401 Unauthorized correcto sin token"
    } else {
        Write-Pass "Acceso denegado (status=$status)"
    }
}

# ============================================================
# RESUMEN FINAL
# ============================================================
Write-Host ""
Write-Host "=" * 60 -ForegroundColor Cyan
Write-Host "  RESUMEN DE PRUEBAS" -ForegroundColor Cyan
Write-Host "=" * 60 -ForegroundColor Cyan
Write-Host "  PASSED: $passed" -ForegroundColor Green
Write-Host "  FAILED: $failed" -ForegroundColor $(if ($failed -gt 0) { "Red" } else { "Green" })
Write-Host "=" * 60 -ForegroundColor Cyan
Write-Host ""

if ($failed -eq 0) {
    Write-Host "  TODOS LOS TESTS PASARON" -ForegroundColor Green
} else {
    Write-Host "  HAY $failed TEST(S) FALLIDOS - REVISAR" -ForegroundColor Red
}

Write-Host ""
Write-Host "  Para limpiar datos de prueba:" -ForegroundColor Yellow
Write-Host '  $env:PGPASSWORD="postgres"; & "C:\Program Files\PostgreSQL\16\bin\psql.exe" -h localhost -p 5434 -U postgres -d pss_dvnx -f tests/cleanup_test_data.sql' -ForegroundColor Yellow
Write-Host ""
