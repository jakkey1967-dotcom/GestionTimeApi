# ========================================
# 🧪 TEST DE TAGS EN RENDER (PRODUCCIÓN)
# ========================================

$ErrorActionPreference = "Continue"
$baseUrl = "https://gestiontimeapi.onrender.com"

Write-Host "🧪 TEST: Tags en Render (Producción)" -ForegroundColor Cyan
Write-Host "=" * 60

# 1. HEALTH CHECK
Write-Host "`n🏥 Paso 1: Health Check..." -ForegroundColor Yellow
try {
    $health = Invoke-RestMethod -Uri "$baseUrl/health" -Method GET -TimeoutSec 10
    Write-Host "✅ Servicio activo" -ForegroundColor Green
    Write-Host ($health | ConvertTo-Json -Depth 3) -ForegroundColor Gray
}
catch {
    Write-Host "❌ Servicio no responde" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

# 2. LOGIN
Write-Host "`n🔐 Paso 2: Login..." -ForegroundColor Yellow
try {
    $loginBody = @{
        email = "psantos@global-retail.com"
        password = "12345678"
    } | ConvertTo-Json

    $loginResponse = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/auth/login-desktop" `
        -Method POST `
        -ContentType "application/json" `
        -Body $loginBody
    
    $token = $loginResponse.accessToken
    Write-Host "✅ Login exitoso" -ForegroundColor Green
    Write-Host "   Email: $($loginResponse.user.email)" -ForegroundColor Gray
    Write-Host "   Token: $($token.Substring(0, 30))..." -ForegroundColor Gray
}
catch {
    Write-Host "❌ Error en login:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host $_.ErrorDetails.Message -ForegroundColor Red
    }
    exit 1
}

# 3. OBTENER TAGS SIN FILTRO
Write-Host "`n📊 Paso 3: Obtener todos los tags (sin filtro)..." -ForegroundColor Yellow
try {
    $headers = @{
        "Authorization" = "Bearer $token"
        "Accept" = "application/json"
    }
    
    $tags = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/tags?limit=20" `
        -Method GET `
        -Headers $headers
    
    Write-Host "✅ Tags obtenidos: $($tags.Count)" -ForegroundColor Green
    Write-Host "   Primeros 10:" -ForegroundColor Gray
    $tags | Select-Object -First 10 | ForEach-Object { Write-Host "     - $_" -ForegroundColor Gray }
}
catch {
    Write-Host "❌ Error obteniendo tags:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host $_.ErrorDetails.Message -ForegroundColor Red
    }
}

# 4. OBTENER TAGS FILTRADOS POR LETRA
Write-Host "`n🔍 Paso 4: Filtrar tags por letra..." -ForegroundColor Yellow

$letras = @("a", "b", "c", "t", "s", "p")

foreach ($letra in $letras) {
    try {
        $tags = Invoke-RestMethod `
            -Uri "$baseUrl/api/v1/tags?source=$letra&limit=10" `
            -Method GET `
            -Headers $headers
        
        Write-Host "   [$letra] → $($tags.Count) tags encontrados" -ForegroundColor $(if ($tags.Count -gt 0) { "Green" } else { "Yellow" })
        
        if ($tags.Count -gt 0) {
            $tags | Select-Object -First 3 | ForEach-Object { Write-Host "        - $_" -ForegroundColor Gray }
        }
    }
    catch {
        Write-Host "   [$letra] → ❌ Error" -ForegroundColor Red
    }
    
    Start-Sleep -Milliseconds 200
}

# 5. OBTENER STATS
Write-Host "`n📈 Paso 5: Obtener estadísticas de tags..." -ForegroundColor Yellow
try {
    $stats = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/tags/stats" `
        -Method GET `
        -Headers $headers
    
    Write-Host "✅ Estadísticas obtenidas:" -ForegroundColor Green
    Write-Host ($stats | ConvertTo-Json -Depth 3) -ForegroundColor Gray
}
catch {
    Write-Host "❌ Error obteniendo stats:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}

Write-Host "`n✅ TEST COMPLETADO" -ForegroundColor Green
Write-Host "`n📋 Test 2: Buscar tags con término 'test'" -ForegroundColor Yellow
try {
    $tagsResponse2 = Invoke-RestMethod -Uri "$API_URL/tags/suggest?term=test&limit=5" -Method Get -Headers $headers
    Write-Host "✅ Respuesta exitosa" -ForegroundColor Green
    Write-Host "   Total tags: $($tagsResponse2.count)" -ForegroundColor White
    
    if ($tagsResponse2.tags -and $tagsResponse2.tags.Count -gt 0) {
        Write-Host "   Tags encontradas:" -ForegroundColor White
        $tagsResponse2.tags | ForEach-Object { Write-Host "      - $_" -ForegroundColor Gray }
    } else {
        Write-Host "   ℹ️  No hay tags que coincidan con 'test'" -ForegroundColor Cyan
    }
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Crear un parte con tags y verificar que se guardaron
Write-Host "`n📝 Test 3: Crear parte con tags" -ForegroundColor Yellow

$today = Get-Date -Format "yyyy-MM-dd"
$partePayload = @{
    fecha_trabajo = $today
    hora_inicio = "10:00"
    hora_fin = "11:00"
    id_cliente = 1
    accion = "Test de sincronización de tags desde PowerShell"
    ticket = $null
    tienda = $null
    id_grupo = $null
    id_tipo = $null
    tags = @("test-powershell", "sync-test", "render-test")
} | ConvertTo-Json

try {
    $createResponse = Invoke-RestMethod -Uri "$API_URL/partes" -Method Post -Body $partePayload -Headers $headers
    $parteId = $createResponse.id
    Write-Host "✅ Parte creado: ID $parteId" -ForegroundColor Green
    
    # Verificar que el parte se creó con tags
    Start-Sleep -Seconds 1
    
    $parteDetail = Invoke-RestMethod -Uri "$API_URL/partes?fecha=$today" -Method Get -Headers $headers
    $parte = $parteDetail | Where-Object { $_.id -eq $parteId }
    
    if ($parte -and $parte.tags) {
        Write-Host "   ✅ Parte tiene tags: $($parte.tags -join ', ')" -ForegroundColor Green
        
        # Ahora verificar que las tags aparecen en suggest
        Write-Host "`n📋 Test 4: Verificar que las nuevas tags aparecen en suggest" -ForegroundColor Yellow
        Start-Sleep -Seconds 1
        
        $tagsResponse3 = Invoke-RestMethod -Uri "$API_URL/tags/suggest?term=test-&limit=10" -Method Get -Headers $headers
        Write-Host "   Total tags con 'test-': $($tagsResponse3.count)" -ForegroundColor White
        
        if ($tagsResponse3.tags -contains "test-powershell") {
            Write-Host "   ✅ Tag 'test-powershell' encontrada en suggest" -ForegroundColor Green
        } else {
            Write-Host "   ❌ Tag 'test-powershell' NO encontrada en suggest" -ForegroundColor Red
        }
    } else {
        Write-Host "   ❌ El parte NO tiene tags" -ForegroundColor Red
    }
    
    # Limpiar: eliminar el parte de prueba
    Write-Host "`n🧹 Limpiando: eliminando parte de prueba..." -ForegroundColor Yellow
    try {
        Invoke-RestMethod -Uri "$API_URL/partes/$parteId" -Method Delete -Headers $headers | Out-Null
        Write-Host "   ✅ Parte eliminado" -ForegroundColor Green
    } catch {
        Write-Host "   ⚠️  No se pudo eliminar el parte: $_" -ForegroundColor Yellow
    }
    
} catch {
    Write-Host "❌ Error al crear parte: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   $($_.ErrorDetails.Message)" -ForegroundColor Red
}

Write-Host "`n✅ PRUEBAS COMPLETADAS" -ForegroundColor Cyan
