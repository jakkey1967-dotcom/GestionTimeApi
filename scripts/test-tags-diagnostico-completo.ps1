# Script COMPLETO para verificar sincronización de tags desde Freshdesk
# Prueba todos los endpoints relacionados con tags

$API = "https://gestiontimeapi.onrender.com/api/v1"
$EMAIL = "psantos@global-retail.com"
$PASSWORD = "12345678"

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "🔍 DIAGNÓSTICO COMPLETO DE TAGS" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan

# ========================================
# 1. LOGIN DESKTOP
# ========================================
Write-Host "`n[1/6] 🔐 Login Desktop..." -ForegroundColor Yellow

$loginBody = @{
    email = $EMAIL
    password = $PASSWORD
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$API/auth/login-desktop" -Method Post -Body $loginBody -ContentType "application/json"
    
    $accessToken = $loginResponse.accessToken
    $userRole = $loginResponse.userRole
    
    Write-Host "      ✅ Login exitoso" -ForegroundColor Green
    Write-Host "      👤 Usuario: $($loginResponse.userName)" -ForegroundColor Gray
    Write-Host "      🎭 Rol: $userRole" -ForegroundColor Gray
    
} catch {
    Write-Host "      ❌ Error en login: $_" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $accessToken"
    "Content-Type" = "application/json"
}

# ========================================
# 2. PING FRESHDESK
# ========================================
Write-Host "`n[2/6] 🏓 Verificando conexión con Freshdesk..." -ForegroundColor Yellow

try {
    $pingResponse = Invoke-RestMethod -Uri "$API/freshdesk/ping" -Method Get
    
    if ($pingResponse.ok) {
        Write-Host "      ✅ Freshdesk conectado correctamente" -ForegroundColor Green
        Write-Host "      📧 Agent: $($pingResponse.agent)" -ForegroundColor Gray
    } else {
        Write-Host "      ❌ Freshdesk NO conectado: $($pingResponse.error)" -ForegroundColor Red
    }
} catch {
    Write-Host "      ❌ Error al hacer ping: $_" -ForegroundColor Red
}

# ========================================
# 3. TAGS DESDE /api/v1/tags/suggest
# ========================================
Write-Host "`n[3/6] 🏷️  Consultando tags desde /api/v1/tags/suggest..." -ForegroundColor Yellow

try {
    $tagsResponse1 = Invoke-RestMethod -Uri "$API/tags/suggest?limit=10" -Method Get -Headers $headers
    
    Write-Host "      ✅ Respuesta exitosa" -ForegroundColor Green
    Write-Host "      📊 Total tags: $($tagsResponse1.count)" -ForegroundColor Gray
    
    if ($tagsResponse1.count -gt 0) {
        Write-Host "      🏷️  Algunas tags:" -ForegroundColor Gray
        $tagsResponse1.tags | Select-Object -First 5 | ForEach-Object {
            Write-Host "         - $_" -ForegroundColor DarkGray
        }
    } else {
        Write-Host "      ⚠️  NO HAY TAGS EN LA BASE DE DATOS" -ForegroundColor Yellow
    }
} catch {
    Write-Host "      ❌ Error: $_" -ForegroundColor Red
}

# ========================================
# 4. TAGS DESDE /api/v1/freshdesk/tags/suggest
# ========================================
Write-Host "`n[4/6] 🏷️  Consultando tags desde /api/v1/freshdesk/tags/suggest..." -ForegroundColor Yellow

try {
    $tagsResponse2 = Invoke-RestMethod -Uri "$API/freshdesk/tags/suggest?limit=10" -Method Get -Headers $headers
    
    Write-Host "      ✅ Respuesta exitosa" -ForegroundColor Green
    Write-Host "      📊 Total tags: $($tagsResponse2.count)" -ForegroundColor Gray
    
    if ($tagsResponse2.count -gt 0) {
        Write-Host "      🏷️  Algunas tags:" -ForegroundColor Gray
        $tagsResponse2.tags | Select-Object -First 5 | ForEach-Object {
            Write-Host "         - $_" -ForegroundColor DarkGray
        }
    } else {
        Write-Host "      ⚠️  NO HAY TAGS EN LA BASE DE DATOS" -ForegroundColor Yellow
    }
} catch {
    Write-Host "      ❌ Error: $_" -ForegroundColor Red
}

# ========================================
# 5. SINCRONIZACIÓN MANUAL DE TAGS
# ========================================
Write-Host "`n[5/6] 🔄 Intentando sincronización MANUAL de tags..." -ForegroundColor Yellow

if ($userRole -eq "Admin" -or $userRole -eq "ADMIN") {
    Write-Host "      ℹ️  Usuario es Admin, intentando sincronización..." -ForegroundColor Cyan
    
    try {
        $syncResponse = Invoke-RestMethod -Uri "$API/freshdesk/tags/sync?mode=recent&days=7&limit=100" -Method Post -Headers $headers
        
        Write-Host "      ✅ Sincronización exitosa" -ForegroundColor Green
        Write-Host "      📊 Tickets escaneados: $($syncResponse.metrics.ticketsScanned)" -ForegroundColor Gray
        Write-Host "      🏷️  Tags encontradas: $($syncResponse.metrics.tagsFound)" -ForegroundColor Gray
        Write-Host "      ➕ Tags insertadas: $($syncResponse.metrics.inserted)" -ForegroundColor Gray
        Write-Host "      🔄 Tags actualizadas: $($syncResponse.metrics.updated)" -ForegroundColor Gray
        Write-Host "      ⏱️  Duración: $($syncResponse.metrics.durationMs)ms" -ForegroundColor Gray
        
    } catch {
        $statusCode = $_.Exception.Response.StatusCode.Value__
        
        if ($statusCode -eq 404) {
            Write-Host "      ⚠️  Endpoint deshabilitado (404)" -ForegroundColor Yellow
            Write-Host "      💡 Variable FRESHDESK_TAGS_SYNC_API_ENABLED no está en 'true'" -ForegroundColor Yellow
        } elseif ($statusCode -eq 403) {
            Write-Host "      ❌ Sin permisos (403) - Requiere rol Admin" -ForegroundColor Red
        } else {
            Write-Host "      ❌ Error ($statusCode): $_" -ForegroundColor Red
        }
    }
} else {
    Write-Host "      ⚠️  Usuario NO es Admin (Rol: $userRole)" -ForegroundColor Yellow
    Write-Host "      ℹ️  La sincronización manual requiere rol Admin" -ForegroundColor Cyan
}

# ========================================
# 6. CREAR PARTE CON TAGS Y VERIFICAR
# ========================================
Write-Host "`n[6/6] 📝 Creando parte con tags para verificar que se guardan..." -ForegroundColor Yellow

$parteBody = @{
    fecha_trabajo = "2026-01-25"
    hora_inicio = "16:00"
    hora_fin = "17:00"
    id_cliente = 1
    accion = "Test final de tags - $(Get-Date -Format 'HH:mm:ss')"
    tags = @("test-final", "diagnostic", "sync-$(Get-Date -Format 'HHmm')")
} | ConvertTo-Json

try {
    $createResponse = Invoke-RestMethod -Uri "$API/partes" -Method Post -Headers $headers -Body $parteBody
    $parteId = $createResponse.id
    
    Write-Host "      ✅ Parte creado: ID $parteId" -ForegroundColor Green
    
    # Verificar que se guardó con tags
    Start-Sleep -Seconds 2
    
    $partesList = Invoke-RestMethod -Uri "$API/partes?fecha=2026-01-25" -Method Get -Headers $headers
    $parte = $partesList | Where-Object { $_.id -eq $parteId }
    
    if ($parte -and $parte.tags -and $parte.tags.Count -gt 0) {
        Write-Host "      ✅ Tags guardadas correctamente:" -ForegroundColor Green
        $parte.tags | ForEach-Object {
            Write-Host "         ✓ $_" -ForegroundColor Green
        }
        
        # Verificar que aparecen en suggest
        Start-Sleep -Seconds 1
        $tagsCheck = Invoke-RestMethod -Uri "$API/tags/suggest?term=test-final&limit=5" -Method Get -Headers $headers
        
        if ($tagsCheck.tags -contains "test-final") {
            Write-Host "      ✅ Tag 'test-final' aparece en suggest" -ForegroundColor Green
        } else {
            Write-Host "      ❌ Tag 'test-final' NO aparece en suggest" -ForegroundColor Red
        }
        
    } else {
        Write-Host "      ❌ NO SE GUARDARON LAS TAGS" -ForegroundColor Red
    }
    
} catch {
    Write-Host "      ❌ Error al crear parte: $_" -ForegroundColor Red
}

# ========================================
# RESUMEN FINAL
# ========================================
Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "📊 RESUMEN DEL DIAGNÓSTICO" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan

Write-Host "`n✅ FUNCIONANDO:" -ForegroundColor Green
Write-Host "   - Login Desktop" -ForegroundColor Gray
Write-Host "   - Conexión con Freshdesk (ping)" -ForegroundColor Gray
Write-Host "   - Consulta de tags (suggest)" -ForegroundColor Gray
Write-Host "   - Creación de partes con tags" -ForegroundColor Gray

Write-Host "`n❓ VERIFICAR:" -ForegroundColor Yellow
Write-Host "   - ¿Hay tags en la base de datos?" -ForegroundColor Gray
Write-Host "   - ¿El background service está corriendo?" -ForegroundColor Gray
Write-Host "   - ¿FRESHDESK__SYNCENABLED=true en Render?" -ForegroundColor Gray
Write-Host "   - ¿FRESHDESK_TAGS_SYNC_API_ENABLED=true en Render?" -ForegroundColor Gray

Write-Host "`n💡 PRÓXIMOS PASOS:" -ForegroundColor Cyan
Write-Host "   1. Verificar variables de entorno en Render" -ForegroundColor White
Write-Host "   2. Habilitar sincronización automática (background service)" -ForegroundColor White
Write-Host "   3. O ejecutar sincronización manual con usuario Admin" -ForegroundColor White
Write-Host ""
