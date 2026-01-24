# 🏥 Script de Comprobación del Endpoint /health

param(
    [string]$Url = "http://localhost:5000",
    [switch]$Render,
    [switch]$Detailed
)

# Si se especifica -Render, usar URL de producción
if ($Render) {
    $Url = "https://gestiontimeapi.onrender.com"
}

$healthUrl = "$Url/health"

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║         🏥 COMPROBACIÓN DE HEALTH CHECK 🏥              ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""
Write-Host "URL: $healthUrl" -ForegroundColor Yellow
Write-Host ""

try {
    # Medir tiempo de respuesta
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $response = Invoke-WebRequest -Uri $healthUrl -Method Get -TimeoutSec 30
    $stopwatch.Stop()
    
    $latency = $stopwatch.ElapsedMilliseconds
    $statusCode = $response.StatusCode
    $contentType = $response.Headers['Content-Type']
    
    # Parsear JSON
    $healthData = $response.Content | ConvertFrom-Json
    
    # ==================== RESULTADOS ====================
    
    Write-Host "✅ CONEXIÓN EXITOSA" -ForegroundColor Green
    Write-Host ""
    
    # Status HTTP
    Write-Host "📊 HTTP Status:" -ForegroundColor Cyan
    Write-Host "   Status Code: $statusCode" -ForegroundColor $(if ($statusCode -eq 200) { "Green" } else { "Yellow" })
    Write-Host "   Content-Type: $contentType"
    Write-Host "   Latencia: ${latency}ms" -ForegroundColor $(if ($latency -lt 1000) { "Green" } elseif ($latency -lt 3000) { "Yellow" } else { "Red" })
    Write-Host ""
    
    # Información Básica
    Write-Host "📋 INFORMACIÓN BÁSICA:" -ForegroundColor Cyan
    Write-Host "   Status: $($healthData.status)" -ForegroundColor $(if ($healthData.status -eq "OK") { "Green" } else { "Red" })
    Write-Host "   Service: $($healthData.service)"
    Write-Host "   Version: $($healthData.version)"
    Write-Host "   Environment: $($healthData.environment)" -ForegroundColor $(if ($healthData.environment -eq "Production") { "Green" } else { "Yellow" })
    Write-Host "   Timestamp: $($healthData.timestamp)"
    Write-Host ""
    
    # Información del Cliente
    Write-Host "👤 CLIENTE/TENANT:" -ForegroundColor Cyan
    Write-Host "   Client: $($healthData.client)" -ForegroundColor Magenta
    Write-Host "   Client ID: $($healthData.clientId)"
    Write-Host "   Schema: $($healthData.schema)"
    Write-Host ""
    
    # Base de Datos
    Write-Host "💾 BASE DE DATOS:" -ForegroundColor Cyan
    Write-Host "   Database: $($healthData.database)" -ForegroundColor $(if ($healthData.database -eq "connected") { "Green" } else { "Red" })
    Write-Host ""
    
    # Sistema
    Write-Host "⚙️  SISTEMA:" -ForegroundColor Cyan
    Write-Host "   Uptime: $($healthData.uptime)"
    Write-Host ""
    
    # Configuración (si existe)
    if ($healthData.configuration) {
        Write-Host "🔧 CONFIGURACIÓN:" -ForegroundColor Cyan
        Write-Host "   JWT Access: $($healthData.configuration.jwtAccessMinutes) minutos"
        Write-Host "   JWT Refresh: $($healthData.configuration.jwtRefreshDays) días"
        Write-Host "   Email Confirmation: $($healthData.configuration.emailConfirmationRequired)"
        Write-Host "   Self Registration: $($healthData.configuration.selfRegistrationAllowed)"
        Write-Host "   Password Expiration: $($healthData.configuration.passwordExpirationDays) días"
        Write-Host "   Max Users: $($healthData.configuration.maxUsers)"
        Write-Host "   Max Storage: $($healthData.configuration.maxStorageGB) GB"
        Write-Host "   CORS Origins: $($healthData.configuration.corsOriginsCount)"
        Write-Host ""
    }
    
    # ==================== VALIDACIONES ====================
    
    Write-Host "✔️  VALIDACIONES:" -ForegroundColor Cyan
    
    $validations = @()
    
    # Validación 1: Status OK
    if ($healthData.status -eq "OK") {
        Write-Host "   ✅ Status es OK" -ForegroundColor Green
        $validations += $true
    } else {
        Write-Host "   ❌ Status NO es OK: $($healthData.status)" -ForegroundColor Red
        $validations += $false
    }
    
    # Validación 2: Base de datos conectada
    if ($healthData.database -eq "connected") {
        Write-Host "   ✅ Base de datos conectada" -ForegroundColor Green
        $validations += $true
    } else {
        Write-Host "   ❌ Base de datos NO conectada: $($healthData.database)" -ForegroundColor Red
        $validations += $false
    }
    
    # Validación 3: Cliente configurado
    if ($healthData.client -and $healthData.client -ne "unknown") {
        Write-Host "   ✅ Cliente configurado: $($healthData.client)" -ForegroundColor Green
        $validations += $true
    } else {
        Write-Host "   ⚠️  Cliente no configurado o desconocido" -ForegroundColor Yellow
        $validations += $false
    }
    
    # Validación 4: Schema configurado
    if ($healthData.schema -and $healthData.schema -ne "unknown") {
        Write-Host "   ✅ Schema configurado: $($healthData.schema)" -ForegroundColor Green
        $validations += $true
    } else {
        Write-Host "   ❌ Schema no configurado" -ForegroundColor Red
        $validations += $false
    }
    
    # Validación 5: Entorno correcto
    if ($Render) {
        if ($healthData.environment -eq "Production") {
            Write-Host "   ✅ Entorno correcto: Production" -ForegroundColor Green
            $validations += $true
        } else {
            Write-Host "   ⚠️  Entorno incorrecto: $($healthData.environment) (esperado: Production)" -ForegroundColor Yellow
            $validations += $false
        }
    } else {
        Write-Host "   ℹ️  Entorno: $($healthData.environment) (local)" -ForegroundColor Cyan
        $validations += $true
    }
    
    # Validación 6: Latencia aceptable
    if ($latency -lt 1000) {
        Write-Host "   ✅ Latencia excelente: ${latency}ms" -ForegroundColor Green
        $validations += $true
    } elseif ($latency -lt 3000) {
        Write-Host "   ⚠️  Latencia aceptable: ${latency}ms" -ForegroundColor Yellow
        $validations += $true
    } else {
        Write-Host "   ❌ Latencia alta: ${latency}ms" -ForegroundColor Red
        $validations += $false
    }
    
    # Validación 7: Configuración presente
    if ($healthData.configuration) {
        Write-Host "   ✅ Configuración detallada presente" -ForegroundColor Green
        $validations += $true
    } else {
        Write-Host "   ⚠️  Configuración detallada no presente" -ForegroundColor Yellow
        $validations += $false
    }
    
    Write-Host ""
    
    # ==================== RESUMEN ====================
    
    $passed = ($validations | Where-Object { $_ -eq $true }).Count
    $total = $validations.Count
    $percentage = [math]::Round(($passed / $total) * 100)
    
    Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║                    📊 RESUMEN                            ║" -ForegroundColor Cyan
    Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "   Validaciones pasadas: $passed / $total ($percentage%)" -ForegroundColor $(if ($percentage -eq 100) { "Green" } elseif ($percentage -ge 70) { "Yellow" } else { "Red" })
    Write-Host ""
    
    if ($percentage -eq 100) {
        Write-Host "   ✅ TODOS LOS CHECKS PASARON" -ForegroundColor Green
        Write-Host "   🎉 El endpoint está funcionando correctamente" -ForegroundColor Green
    } elseif ($percentage -ge 70) {
        Write-Host "   ⚠️  LA MAYORÍA DE CHECKS PASARON" -ForegroundColor Yellow
        Write-Host "   ℹ️  Revisa las validaciones fallidas arriba" -ForegroundColor Yellow
    } else {
        Write-Host "   ❌ VARIOS CHECKS FALLARON" -ForegroundColor Red
        Write-Host "   🔧 Se requieren correcciones" -ForegroundColor Red
    }
    Write-Host ""
    
    # ==================== JSON COMPLETO (si se solicita) ====================
    
    if ($Detailed) {
        Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
        Write-Host "║               📄 RESPUESTA JSON COMPLETA                 ║" -ForegroundColor Cyan
        Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
        Write-Host ""
        $healthData | ConvertTo-Json -Depth 10 | Write-Host -ForegroundColor Gray
        Write-Host ""
    }
    
    # ==================== GUARDAR RESULTADO ====================
    
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $filename = "health-check-$timestamp.json"
    $healthData | ConvertTo-Json -Depth 10 | Out-File $filename -Encoding UTF8
    Write-Host "💾 Resultado guardado en: $filename" -ForegroundColor Cyan
    Write-Host ""
    
} catch {
    Write-Host ""
    Write-Host "❌ ERROR AL CONECTAR CON EL ENDPOINT" -ForegroundColor Red
    Write-Host ""
    Write-Host "Detalles del error:" -ForegroundColor Yellow
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    
    if ($_.Exception.InnerException) {
        Write-Host "Error interno:" -ForegroundColor Yellow
        Write-Host $_.Exception.InnerException.Message -ForegroundColor Red
        Write-Host ""
    }
    
    Write-Host "🔧 POSIBLES CAUSAS:" -ForegroundColor Cyan
    Write-Host "   • La API no está corriendo" -ForegroundColor Yellow
    Write-Host "   • URL incorrecta: $healthUrl" -ForegroundColor Yellow
    Write-Host "   • Firewall bloqueando la conexión" -ForegroundColor Yellow
    Write-Host "   • Timeout (el servidor tarda más de 30 segundos)" -ForegroundColor Yellow
    Write-Host ""
    
    exit 1
}
