# Script para crear un parte con tags en RENDER
# Similar al test local pero apuntando a Render

$API_URL = "https://gestiontimeapi.onrender.com/api/v1"

# CONFIGURA TUS CREDENCIALES AQUÍ
$EMAIL = "tu-email@ejemplo.com"
$PASSWORD = "tu-password"

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "🏷️  CREAR PARTE CON TAGS EN RENDER" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan

# ========================================
# 1. LOGIN
# ========================================
Write-Host "`n[1/4] 🔐 Iniciando sesión..." -ForegroundColor Yellow

$loginBody = @{
    email = $EMAIL
    password = $PASSWORD
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod `
        -Uri "$API_URL/auth/login" `
        -Method Post `
        -Body $loginBody `
        -ContentType "application/json"
    
    $TOKEN = $loginResponse.token
    Write-Host "      ✅ Login exitoso" -ForegroundColor Green
    Write-Host "      👤 Usuario: $($loginResponse.user.fullName)" -ForegroundColor Gray
} catch {
    Write-Host "      ❌ Error en login: $_" -ForegroundColor Red
    Write-Host "      💡 Verifica email y password en el script" -ForegroundColor Yellow
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $TOKEN"
    "Content-Type" = "application/json"
}

# ========================================
# 2. VERIFICAR TAGS EXISTENTES
# ========================================
Write-Host "`n[2/4] 🔍 Verificando tags existentes..." -ForegroundColor Yellow

try {
    $tagsResponse = Invoke-RestMethod `
        -Uri "$API_URL/tags/suggest?limit=10" `
        -Method Get `
        -Headers $headers
    
    Write-Host "      📊 Total tags en BD: $($tagsResponse.count)" -ForegroundColor Gray
    
    if ($tagsResponse.count -gt 0) {
        Write-Host "      🏷️  Algunas tags:" -ForegroundColor Gray
        $tagsResponse.tags | Select-Object -First 5 | ForEach-Object {
            Write-Host "         - $_" -ForegroundColor DarkGray
        }
    } else {
        Write-Host "      ℹ️  No hay tags en la BD aún" -ForegroundColor Cyan
    }
} catch {
    Write-Host "      ⚠️  Error al consultar tags: $_" -ForegroundColor Yellow
}

# ========================================
# 3. CREAR PARTE CON TAGS
# ========================================
Write-Host "`n[3/4] 📝 Creando parte con tags..." -ForegroundColor Yellow

$today = Get-Date -Format "yyyy-MM-dd"
$hora = (Get-Date).ToString("HH:mm")

$parteBody = @{
    fecha_trabajo = $today
    hora_inicio = "09:00"
    hora_fin = "10:30"
    id_cliente = 1  # Cliente genérico
    accion = "Prueba de sincronización de tags desde Render - $(Get-Date -Format 'HH:mm:ss')"
    ticket = $null
    tienda = $null
    id_grupo = $null
    id_tipo = $null
    tags = @(
        "test-render",
        "powershell-test",
        "sync-$(Get-Date -Format 'HHmm')"
    )
} | ConvertTo-Json

Write-Host "      📋 Datos del parte:" -ForegroundColor Gray
Write-Host "         Fecha: $today" -ForegroundColor DarkGray
Write-Host "         Horario: 09:00 - 10:30" -ForegroundColor DarkGray
Write-Host "         Tags: $($parteBody | ConvertFrom-Json | Select-Object -ExpandProperty tags | ForEach-Object { $_ })" -ForegroundColor DarkGray

try {
    $createResponse = Invoke-RestMethod `
        -Uri "$API_URL/partes" `
        -Method Post `
        -Body $parteBody `
        -Headers $headers
    
    $parteId = $createResponse.id
    Write-Host "      ✅ Parte creado exitosamente" -ForegroundColor Green
    Write-Host "      🆔 ID: $parteId" -ForegroundColor Gray
} catch {
    Write-Host "      ❌ Error al crear parte: $_" -ForegroundColor Red
    Write-Host "      $($_.ErrorDetails.Message)" -ForegroundColor Red
    exit 1
}

# ========================================
# 4. VERIFICAR QUE SE GUARDÓ CON TAGS
# ========================================
Write-Host "`n[4/4] ✅ Verificando parte creado..." -ForegroundColor Yellow

Start-Sleep -Seconds 1

try {
    $partesList = Invoke-RestMethod `
        -Uri "$API_URL/partes?fecha=$today" `
        -Method Get `
        -Headers $headers
    
    $parteCreado = $partesList | Where-Object { $_.id -eq $parteId }
    
    if ($parteCreado) {
        Write-Host "      ✅ Parte encontrado en la lista" -ForegroundColor Green
        Write-Host "      📋 Acción: $($parteCreado.accion)" -ForegroundColor Gray
        Write-Host "      🏷️  Tags guardadas: $($parteCreado.tags.Count)" -ForegroundColor Gray
        
        if ($parteCreado.tags -and $parteCreado.tags.Count -gt 0) {
            $parteCreado.tags | ForEach-Object {
                Write-Host "         ✓ $_" -ForegroundColor Green
            }
        } else {
            Write-Host "         ❌ NO SE GUARDARON LAS TAGS" -ForegroundColor Red
        }
    } else {
        Write-Host "      ⚠️  Parte no encontrado en la lista" -ForegroundColor Yellow
    }
} catch {
    Write-Host "      ⚠️  Error al verificar parte: $_" -ForegroundColor Yellow
}

# ========================================
# 5. VERIFICAR QUE APARECEN EN SUGGEST
# ========================================
Write-Host "`n[EXTRA] 🔍 Verificando tags en suggest..." -ForegroundColor Yellow

Start-Sleep -Seconds 1

try {
    $tagsResponse2 = Invoke-RestMethod `
        -Uri "$API_URL/tags/suggest?term=test-render&limit=5" `
        -Method Get `
        -Headers $headers
    
    if ($tagsResponse2.tags -contains "test-render") {
        Write-Host "      ✅ Tag 'test-render' aparece en suggest" -ForegroundColor Green
    } else {
        Write-Host "      ❌ Tag 'test-render' NO aparece en suggest" -ForegroundColor Red
        Write-Host "      📋 Tags encontradas: $($tagsResponse2.tags -join ', ')" -ForegroundColor Gray
    }
} catch {
    Write-Host "      ⚠️  Error al consultar suggest: $_" -ForegroundColor Yellow
}

# ========================================
# RESUMEN
# ========================================
Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "✅ PRUEBA COMPLETADA" -ForegroundColor Green
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host ""
Write-Host "📊 Parte ID: $parteId" -ForegroundColor White
Write-Host "🔗 Puedes verlo en la app o eliminarlo con:" -ForegroundColor Gray
Write-Host ""
Write-Host "   DELETE $API_URL/partes/$parteId" -ForegroundColor DarkGray
Write-Host "   Header: Authorization: Bearer TOKEN" -ForegroundColor DarkGray
Write-Host ""
Write-Host "💡 Para eliminar el parte de prueba, ejecuta:" -ForegroundColor Yellow
Write-Host "   Invoke-RestMethod -Uri '$API_URL/partes/$parteId' -Method Delete -Headers @{'Authorization'='Bearer $TOKEN'}" -ForegroundColor Cyan
Write-Host ""
