# Script para probar tags en Render
# Verifica si las tablas de tags existen y funcionan

$API_URL = "https://gestiontimeapi.onrender.com/api/v1"
$EMAIL = "tu-email@ejemplo.com"
$PASSWORD = "tu-password"

Write-Host "🔐 INICIANDO SESIÓN EN RENDER..." -ForegroundColor Cyan

# Login
$loginPayload = @{
    email = $EMAIL
    password = $PASSWORD
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$API_URL/auth/login" -Method Post -Body $loginPayload -ContentType "application/json"
    $TOKEN = $loginResponse.token
    Write-Host "✅ Login exitoso" -ForegroundColor Green
} catch {
    Write-Host "❌ Error en login: $_" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $TOKEN"
    "Content-Type" = "application/json"
}

Write-Host "`n🏷️  PROBANDO ENDPOINT DE TAGS..." -ForegroundColor Cyan

# Test 1: GET /api/v1/tags/suggest (sin filtro)
Write-Host "`n📋 Test 1: Sugerir tags (sin filtro)" -ForegroundColor Yellow
try {
    $tagsResponse = Invoke-RestMethod -Uri "$API_URL/tags/suggest?limit=10" -Method Get -Headers $headers
    Write-Host "✅ Respuesta exitosa" -ForegroundColor Green
    Write-Host "   Total tags: $($tagsResponse.count)" -ForegroundColor White
    
    if ($tagsResponse.tags -and $tagsResponse.tags.Count -gt 0) {
        Write-Host "   Tags encontradas:" -ForegroundColor White
        $tagsResponse.tags | ForEach-Object { Write-Host "      - $_" -ForegroundColor Gray }
    } else {
        Write-Host "   ⚠️  No hay tags en la base de datos" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   $($_.ErrorDetails.Message)" -ForegroundColor Red
}

# Test 2: GET /api/v1/tags/suggest?term=x
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
