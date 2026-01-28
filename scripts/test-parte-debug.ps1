# Script para debuggear el problema del 401 en Render

$API = "https://gestiontimeapi.onrender.com/api/v1"
$EMAIL = "psantos@global-retail.com"
$PASSWORD = "12345678"

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "🔍 DEBUG: Creación de Parte con Tags" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan

# ========================================
# 1. LOGIN Y VERIFICAR TOKEN
# ========================================
Write-Host "`n[1/5] 🔐 Login..." -ForegroundColor Yellow

$loginBody = @{
    email = $EMAIL
    password = $PASSWORD
} | ConvertTo-Json

Write-Host "      📤 Request: $API/auth/login" -ForegroundColor Gray

try {
    $login = Invoke-RestMethod -Uri "$API/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    
    $token = $login.token
    Write-Host "      ✅ Login exitoso" -ForegroundColor Green
    Write-Host "      👤 Usuario: $($login.user.fullName)" -ForegroundColor Gray
    Write-Host "      🔑 Token (primeros 50 chars): $($token.Substring(0, [Math]::Min(50, $token.Length)))..." -ForegroundColor Gray
    Write-Host "      📏 Longitud token: $($token.Length) caracteres" -ForegroundColor Gray
} catch {
    Write-Host "      ❌ Error en login: $_" -ForegroundColor Red
    exit 1
}

# ========================================
# 2. VERIFICAR QUE EL TOKEN FUNCIONA
# ========================================
Write-Host "`n[2/5] ✅ Verificando token con endpoint público..." -ForegroundColor Yellow

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

try {
    # Probar con un endpoint simple primero
    $healthCheck = Invoke-RestMethod -Uri "$API/health" -Method Get
    Write-Host "      ✅ Health check OK" -ForegroundColor Green
} catch {
    Write-Host "      ⚠️  Health check falló: $_" -ForegroundColor Yellow
}

# ========================================
# 3. PROBAR ENDPOINT AUTENTICADO (GET)
# ========================================
Write-Host "`n[3/5] 🔐 Probando GET /partes (autenticado)..." -ForegroundColor Yellow

try {
    $partes = Invoke-RestMethod -Uri "$API/partes?fecha=2026-01-25" -Method Get -Headers $headers
    Write-Host "      ✅ GET /partes funciona correctamente" -ForegroundColor Green
    Write-Host "      📊 Partes encontrados: $($partes.Count)" -ForegroundColor Gray
} catch {
    Write-Host "      ❌ Error en GET /partes: $_" -ForegroundColor Red
    Write-Host "      StatusCode: $($_.Exception.Response.StatusCode.Value__)" -ForegroundColor Red
    
    if ($_.Exception.Response.StatusCode.Value__ -eq 401) {
        Write-Host "" -ForegroundColor Red
        Write-Host "      ⚠️  EL TOKEN NO ES VÁLIDO PARA ENDPOINTS AUTENTICADOS" -ForegroundColor Red
        Write-Host "      💡 Posibles causas:" -ForegroundColor Yellow
        Write-Host "         1. El token expiró inmediatamente" -ForegroundColor Yellow
        Write-Host "         2. Problema con la configuración JWT en Render" -ForegroundColor Yellow
        Write-Host "         3. El usuario no tiene permisos" -ForegroundColor Yellow
        exit 1
    }
}

# ========================================
# 4. CREAR PARTE CON TAGS
# ========================================
Write-Host "`n[4/5] 📝 Creando parte con tags..." -ForegroundColor Yellow

$parteBody = @{
    fecha_trabajo = "2026-01-25"
    hora_inicio = "09:00"
    hora_fin = "11:30"
    id_cliente = 1
    accion = "Prueba con tags desde PowerShell - Debug"
    tags = @("test", "powershell", "render-debug")
} | ConvertTo-Json

Write-Host "      📤 Request body:" -ForegroundColor Gray
Write-Host $parteBody -ForegroundColor DarkGray

try {
    $parte = Invoke-RestMethod -Uri "$API/partes" -Method Post -Headers $headers -Body $parteBody
    
    Write-Host "      ✅ Parte creado exitosamente" -ForegroundColor Green
    Write-Host "      🆔 ID: $($parte.id)" -ForegroundColor Gray
    
    $parteId = $parte.id
    
} catch {
    Write-Host "      ❌ Error al crear parte: $_" -ForegroundColor Red
    Write-Host "      StatusCode: $($_.Exception.Response.StatusCode.Value__)" -ForegroundColor Red
    
    if ($_.ErrorDetails.Message) {
        Write-Host "      📋 Detalles: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
    
    exit 1
}

# ========================================
# 5. VERIFICAR QUE SE GUARDÓ CON TAGS
# ========================================
Write-Host "`n[5/5] 🔍 Verificando parte creado..." -ForegroundColor Yellow

Start-Sleep -Seconds 2

try {
    $partesList = Invoke-RestMethod -Uri "$API/partes?fecha=2026-01-25" -Method Get -Headers $headers
    
    $parteCreado = $partesList | Where-Object { $_.id -eq $parteId }
    
    if ($parteCreado) {
        Write-Host "      ✅ Parte encontrado" -ForegroundColor Green
        Write-Host "      🏷️  Tags: $($parteCreado.tags.Count)" -ForegroundColor Gray
        
        if ($parteCreado.tags -and $parteCreado.tags.Count -gt 0) {
            $parteCreado.tags | ForEach-Object {
                Write-Host "         ✓ $_" -ForegroundColor Green
            }
            
            # Verificar que aparecen en suggest
            Write-Host "`n      🔍 Verificando tags en suggest..." -ForegroundColor Yellow
            
            $tagsSuggest = Invoke-RestMethod -Uri "$API/tags/suggest?term=test&limit=10" -Method Get -Headers $headers
            
            if ($tagsSuggest.tags -contains "test") {
                Write-Host "         ✅ Tag 'test' aparece en suggest" -ForegroundColor Green
            } else {
                Write-Host "         ❌ Tag 'test' NO aparece en suggest" -ForegroundColor Red
            }
            
        } else {
            Write-Host "         ❌ NO SE GUARDARON LAS TAGS" -ForegroundColor Red
        }
    } else {
        Write-Host "      ⚠️  Parte no encontrado" -ForegroundColor Yellow
    }
    
} catch {
    Write-Host "      ❌ Error al verificar: $_" -ForegroundColor Red
}

Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "✅ DEBUG COMPLETADO" -ForegroundColor Green
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
