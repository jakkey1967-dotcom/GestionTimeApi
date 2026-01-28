# Script que simula el flujo de autenticación del Desktop
# 1. Login → obtiene access + refresh tokens
# 2. Usa access token para crear parte con tags
# 3. Si el access expira (15 min), usa refresh para renovar

$API = "https://gestiontimeapi.onrender.com/api/v1"
$EMAIL = "psantos@global-retail.com"
$PASSWORD = "12345678"

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "🖥️  FLUJO DE AUTENTICACIÓN DESKTOP" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan

# ========================================
# 1. LOGIN
# ========================================
Write-Host "`n[1/3] 🔐 Login inicial..." -ForegroundColor Yellow

$loginBody = @{
    email = $EMAIL
    password = $PASSWORD
} | ConvertTo-Json

try {
    $loginResponse = Invoke-WebRequest -Uri "$API/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    
    # Extraer tokens de las cookies (comportamiento web)
    $accessToken = $null
    $refreshToken = $null
    
    foreach ($cookie in $loginResponse.Headers.'Set-Cookie') {
        if ($cookie -match 'access_token=([^;]+)') {
            $accessToken = $matches[1]
        }
        if ($cookie -match 'refresh_token=([^;]+)') {
            $refreshToken = $matches[1]
        }
    }
    
    if ($null -eq $accessToken) {
        Write-Host "      ❌ No se pudo extraer access_token de las cookies" -ForegroundColor Red
        Write-Host "      💡 Posible problema: la API no está devolviendo tokens en cookies" -ForegroundColor Yellow
        exit 1
    }
    
    Write-Host "      ✅ Login exitoso" -ForegroundColor Green
    Write-Host "      🔑 Access Token (primeros 50 chars): $($accessToken.Substring(0, [Math]::Min(50, $accessToken.Length)))..." -ForegroundColor Gray
    Write-Host "      🔄 Refresh Token: $(if($refreshToken) { '✓ obtenido' } else { '✗ no encontrado' })" -ForegroundColor Gray
    
} catch {
    Write-Host "      ❌ Error en login: $_" -ForegroundColor Red
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
    hora_inicio = "09:00"
    hora_fin = "11:30"
    id_cliente = 1
    accion = "Prueba con tags - Flujo Desktop correcto"
    tags = @("desktop-test", "render-test", "ps-$(Get-Date -Format 'HHmm')")
} | ConvertTo-Json

try {
    $createResponse = Invoke-RestMethod -Uri "$API/partes" -Method Post -Headers $headers -Body $parteBody
    
    Write-Host "      ✅ Parte creado: ID $($createResponse.id)" -ForegroundColor Green
    $parteId = $createResponse.id
    
} catch {
    Write-Host "      ❌ Error al crear parte: $_" -ForegroundColor Red
    
    if ($_.Exception.Response.StatusCode.Value__ -eq 401) {
        Write-Host "`n      🔄 Token expirado, intentando refresh..." -ForegroundColor Yellow
        
        if ($null -eq $refreshToken) {
            Write-Host "         ❌ No hay refresh token disponible" -ForegroundColor Red
            exit 1
        }
        
        # REFRESH TOKEN
        try {
            $refreshBody = @{ refreshToken = $refreshToken } | ConvertTo-Json
            $refreshResponse = Invoke-RestMethod -Uri "$API/auth/refresh" -Method Post -Body $refreshBody -ContentType "application/json"
            
            $accessToken = $refreshResponse.accessToken
            $refreshToken = $refreshResponse.refreshToken
            
            Write-Host "         ✅ Tokens renovados" -ForegroundColor Green
            
            # Reintentar creación de parte
            $headers = @{
                "Authorization" = "Bearer $accessToken"
                "Content-Type" = "application/json"
            }
            
            $createResponse = Invoke-RestMethod -Uri "$API/partes" -Method Post -Headers $headers -Body $parteBody
            Write-Host "      ✅ Parte creado (tras refresh): ID $($createResponse.id)" -ForegroundColor Green
            $parteId = $createResponse.id
            
        } catch {
            Write-Host "         ❌ Error en refresh: $_" -ForegroundColor Red
            exit 1
        }
    } else {
        exit 1
    }
}

# ========================================
# 3. VERIFICAR TAGS
# ========================================
Write-Host "`n[3/3] 🔍 Verificando parte con tags..." -ForegroundColor Yellow

Start-Sleep -Seconds 2

try {
    $partesList = Invoke-RestMethod -Uri "$API/partes?fecha=2026-01-25" -Method Get -Headers $headers
    
    $parte = $partesList | Where-Object { $_.id -eq $parteId }
    
    if ($parte) {
        Write-Host "      ✅ Parte encontrado" -ForegroundColor Green
        Write-Host "      🏷️  Tags ($($parte.tags.Count)):" -ForegroundColor Gray
        
        if ($parte.tags -and $parte.tags.Count -gt 0) {
            $parte.tags | ForEach-Object {
                Write-Host "         ✓ $_" -ForegroundColor Green
            }
        } else {
            Write-Host "         ❌ NO HAY TAGS" -ForegroundColor Red
        }
    } else {
        Write-Host "      ⚠️  Parte no encontrado" -ForegroundColor Yellow
    }
    
    # Verificar en suggest
    Write-Host "`n      🔍 Verificando en suggest..." -ForegroundColor Yellow
    $tagsSuggest = Invoke-RestMethod -Uri "$API/tags/suggest?term=desktop-test&limit=5" -Method Get -Headers $headers
    
    if ($tagsSuggest.tags -contains "desktop-test") {
        Write-Host "         ✅ Tag 'desktop-test' aparece en suggest" -ForegroundColor Green
    } else {
        Write-Host "         ❌ Tag 'desktop-test' NO aparece en suggest" -ForegroundColor Red
    }
    
} catch {
    Write-Host "      ❌ Error al verificar: $_" -ForegroundColor Red
}

Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "✅ PRUEBA COMPLETADA" -ForegroundColor Green
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan

Write-Host "`n💡 NOTA IMPORTANTE PARA DESKTOP:" -ForegroundColor Yellow
Write-Host "   El Desktop DEBE implementar refresh token automático" -ForegroundColor White
Write-Host "   cuando recibe 401 Unauthorized." -ForegroundColor White
Write-Host "`n   Flujo correcto:" -ForegroundColor White
Write-Host "   1. Guardar accessToken y refreshToken tras login" -ForegroundColor Gray
Write-Host "   2. Usar accessToken en todos los requests" -ForegroundColor Gray
Write-Host "   3. Si recibe 401 → POST /auth/refresh con refreshToken" -ForegroundColor Gray
Write-Host "   4. Actualizar tokens y reintentar request" -ForegroundColor Gray
Write-Host ""
