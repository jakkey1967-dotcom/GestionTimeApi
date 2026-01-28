# Script para probar autenticación DESKTOP con login-desktop endpoint
# Este endpoint devuelve tokens en JSON directamente (sin cookies)

$API = "https://gestiontimeapi.onrender.com/api/v1"
$EMAIL = "psantos@global-retail.com"
$PASSWORD = "12345678"

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "🖥️  LOGIN DESKTOP + CREAR PARTE CON TAGS" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan

# ========================================
# 1. LOGIN-DESKTOP
# ========================================
Write-Host "`n[1/3] 🔐 Login Desktop..." -ForegroundColor Yellow

$loginBody = @{
    email = $EMAIL
    password = $PASSWORD
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$API/auth/login-desktop" -Method Post -Body $loginBody -ContentType "application/json"
    
    # ✅ Tokens en JSON (sin cookies)
    $accessToken = $loginResponse.accessToken
    $refreshToken = $loginResponse.refreshToken
    $sessionId = $loginResponse.sessionId
    
    Write-Host "      ✅ Login Desktop exitoso" -ForegroundColor Green
    Write-Host "      👤 Usuario: $($loginResponse.userName)" -ForegroundColor Gray
    Write-Host "      📧 Email: $($loginResponse.userEmail)" -ForegroundColor Gray
    Write-Host "      🎭 Rol: $($loginResponse.userRole)" -ForegroundColor Gray
    Write-Host "      🔑 Access Token (50 chars): $($accessToken.Substring(0, [Math]::Min(50, $accessToken.Length)))..." -ForegroundColor Gray
    Write-Host "      🔄 Refresh Token: ✓ obtenido" -ForegroundColor Gray
    Write-Host "      🆔 Session ID: $sessionId" -ForegroundColor Gray
    
} catch {
    Write-Host "      ❌ Error en login-desktop: $_" -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host "      📋 Detalles: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
    exit 1
}

# ========================================
# 2. CREAR PARTE CON TAGS
# ========================================
Write-Host "`n[2/3] 📝 Creando parte con tags..." -ForegroundColor Yellow

$headers = @{
    "Authorization" = "Bearer $accessToken"
    "Content-Type" = "application/json"
}

$parteBody = @{
    fecha_trabajo = "2026-01-25"
    hora_inicio = "14:00"
    hora_fin = "15:30"
    id_cliente = 1
    accion = "Prueba Desktop con tags - $(Get-Date -Format 'HH:mm:ss')"
    tags = @("desktop", "render", "test-$(Get-Date -Format 'HHmm')")
} | ConvertTo-Json

Write-Host "      📋 Datos:" -ForegroundColor Gray
Write-Host "         Fecha: 2026-01-25" -ForegroundColor DarkGray
Write-Host "         Horario: 14:00 - 15:30" -ForegroundColor DarkGray
Write-Host "         Tags: desktop, render, test-$(Get-Date -Format 'HHmm')" -ForegroundColor DarkGray

try {
    $createResponse = Invoke-RestMethod -Uri "$API/partes" -Method Post -Headers $headers -Body $parteBody
    
    Write-Host "      ✅ Parte creado: ID $($createResponse.id)" -ForegroundColor Green
    $parteId = $createResponse.id
    
} catch {
    Write-Host "      ❌ Error al crear parte: $_" -ForegroundColor Red
    Write-Host "      StatusCode: $($_.Exception.Response.StatusCode.Value__)" -ForegroundColor Red
    
    if ($_.ErrorDetails.Message) {
        Write-Host "      📋 Detalles: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
    
    exit 1
}

# ========================================
# 3. VERIFICAR PARTE CON TAGS
# ========================================
Write-Host "`n[3/3] 🔍 Verificando parte creado..." -ForegroundColor Yellow

Start-Sleep -Seconds 2

try {
    $partesList = Invoke-RestMethod -Uri "$API/partes?fecha=2026-01-25" -Method Get -Headers $headers
    
    $parte = $partesList | Where-Object { $_.id -eq $parteId }
    
    if ($parte) {
        Write-Host "      ✅ Parte encontrado" -ForegroundColor Green
        Write-Host "      📋 Acción: $($parte.accion)" -ForegroundColor Gray
        Write-Host "      🏷️  Tags ($($parte.tags.Count)):" -ForegroundColor Gray
        
        if ($parte.tags -and $parte.tags.Count -gt 0) {
            $parte.tags | ForEach-Object {
                Write-Host "         ✓ $_" -ForegroundColor Green
            }
            
            Write-Host "`n      ✅ LAS TAGS SE GUARDARON CORRECTAMENTE" -ForegroundColor Green
            
        } else {
            Write-Host "         ❌ NO SE GUARDARON LAS TAGS" -ForegroundColor Red
        }
    } else {
        Write-Host "      ⚠️  Parte no encontrado" -ForegroundColor Yellow
    }
    
    # Verificar en suggest
    Write-Host "`n      🔍 Verificando tags en suggest..." -ForegroundColor Yellow
    $tagsSuggest = Invoke-RestMethod -Uri "$API/tags/suggest?term=desktop&limit=5" -Method Get -Headers $headers
    
    Write-Host "         📊 Total tags con 'desktop': $($tagsSuggest.count)" -ForegroundColor Gray
    
    if ($tagsSuggest.tags -contains "desktop") {
        Write-Host "         ✅ Tag 'desktop' aparece en suggest" -ForegroundColor Green
    } else {
        Write-Host "         ❌ Tag 'desktop' NO aparece en suggest" -ForegroundColor Red
        if ($tagsSuggest.tags) {
            Write-Host "         📋 Tags encontradas: $($tagsSuggest.tags -join ', ')" -ForegroundColor Gray
        }
    }
    
} catch {
    Write-Host "      ❌ Error al verificar: $_" -ForegroundColor Red
}

Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "✅ PRUEBA COMPLETADA" -ForegroundColor Green
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan

Write-Host "`n📊 RESUMEN:" -ForegroundColor White
Write-Host "   🆔 Parte ID: $parteId" -ForegroundColor Gray
Write-Host "   🔑 Session ID: $sessionId" -ForegroundColor Gray
Write-Host ""
Write-Host "💡 El Desktop debe usar /auth/login-desktop (devuelve tokens en JSON)" -ForegroundColor Yellow
Write-Host "   No /auth/login (devuelve tokens en cookies)" -ForegroundColor Yellow
Write-Host ""
