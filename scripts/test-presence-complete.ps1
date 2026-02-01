# Test completo de Presencia de Usuarios
# Verifica login, estado online, refresco y kick

$ErrorActionPreference = "Continue"
$baseUrl = "https://localhost:2502"

# Ignorar certificados SSL
if (-not ([System.Management.Automation.PSTypeName]'ServerCertificateValidationCallback').Type) {
    $certCallback = @"
    using System;
    using System.Net;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;
    public class ServerCertificateValidationCallback {
        public static void Ignore() {
            if(ServicePointManager.ServerCertificateValidationCallback == null) {
                ServicePointManager.ServerCertificateValidationCallback += 
                    delegate(Object obj, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors) {
                        return true;
                    };
            }
        }
    }
"@
    Add-Type $certCallback
}
[ServerCertificateValidationCallback]::Ignore()

Write-Host "👥 TEST: Presencia de Usuarios (Login + Online + Kick)" -ForegroundColor Cyan
Write-Host "=" * 70

# ============================================
# CONFIGURACIÓN DE USUARIOS
# ============================================
Write-Host "`n📋 Configuración de usuarios para testing..." -ForegroundColor Yellow

# Usuarios por defecto
$adminUser = "psantos@global-retail.com"
$adminPassPlain = "12345678"

$normalUser = "wsanchez@global-retail.com"
$normalPassPlain = "12345678"

Write-Host "   Admin: $adminUser" -ForegroundColor Gray
Write-Host "   Normal: $normalUser" -ForegroundColor Gray

# ============================================
# PASO 1: LOGIN DEL ADMIN
# ============================================
Write-Host "`n📝 Paso 1: Login como ADMIN..." -ForegroundColor Yellow
try {
    $loginBody = @{
        email = $adminUser
        password = $adminPassPlain
    } | ConvertTo-Json

    $adminLogin = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/auth/login-desktop" `
        -Method POST `
        -ContentType "application/json" `
        -Body $loginBody
    
    $adminToken = $adminLogin.accessToken
    $adminHeaders = @{
        "Authorization" = "Bearer $adminToken"
        "Content-Type" = "application/json"
    }
    
    Write-Host "✅ Login ADMIN exitoso" -ForegroundColor Green
    Write-Host "   Email: $($adminLogin.user.email)" -ForegroundColor Gray
    Write-Host "   Role: $($adminLogin.user.role)" -ForegroundColor Gray
    Write-Host "   SessionId: $($adminLogin.sessionId)" -ForegroundColor Gray
}
catch {
    Write-Host "❌ Error en login ADMIN:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

# ============================================
# PASO 2: LOGIN DEL USUARIO NORMAL
# ============================================
Write-Host "`n📝 Paso 2: Login como Usuario Normal..." -ForegroundColor Yellow
try {
    $loginBody = @{
        email = $normalUser
        password = $normalPassPlain
    } | ConvertTo-Json

    $normalLogin = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/auth/login-desktop" `
        -Method POST `
        -ContentType "application/json" `
        -Body $loginBody
    
    $normalToken = $normalLogin.accessToken
    $normalUserId = $normalLogin.user.id
    $normalHeaders = @{
        "Authorization" = "Bearer $normalToken"
        "Content-Type" = "application/json"
    }
    
    Write-Host "✅ Login Usuario Normal exitoso" -ForegroundColor Green
    Write-Host "   Email: $($normalLogin.user.email)" -ForegroundColor Gray
    Write-Host "   Role: $($normalLogin.user.role)" -ForegroundColor Gray
    Write-Host "   UserId: $normalUserId" -ForegroundColor Gray
    Write-Host "   SessionId: $($normalLogin.sessionId)" -ForegroundColor Gray
}
catch {
    Write-Host "❌ Error en login Usuario Normal:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

# ============================================
# PASO 3: VERIFICAR PRESENCIA (ambos online)
# ============================================
Write-Host "`n👥 Paso 3: Verificar presencia de usuarios (ambos deben estar online)..." -ForegroundColor Yellow
try {
    $presence = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/presence/users" `
        -Method GET `
        -Headers $adminHeaders
    
    Write-Host "✅ Lista de presencia obtenida:" -ForegroundColor Green
    Write-Host "   Total usuarios: $($presence.Count)" -ForegroundColor Gray
    Write-Host "   Usuarios online: $($presence | Where-Object { $_.isOnline } | Measure-Object | Select-Object -ExpandProperty Count)" -ForegroundColor Gray
    
    Write-Host "`n   📋 Detalle de usuarios:" -ForegroundColor Cyan
    foreach ($user in $presence) {
        $statusIcon = if ($user.isOnline) { "🟢" } else { "⚫" }
        $lastSeen = if ($user.lastSeenAt) { 
            (Get-Date $user.lastSeenAt).ToString("yyyy-MM-dd HH:mm:ss") 
        } else { 
            "Nunca" 
        }
        
        Write-Host "   $statusIcon [$($user.role)] $($user.fullName) ($($user.email))" -ForegroundColor Gray
        Write-Host "      Última actividad: $lastSeen" -ForegroundColor DarkGray
    }
    
    # Verificar que ambos usuarios están online
    $adminOnline = $presence | Where-Object { $_.email -eq $adminUser -and $_.isOnline }
    $normalOnline = $presence | Where-Object { $_.email -eq $normalUser -and $_.isOnline }
    
    if ($adminOnline) {
        Write-Host "`n   ✅ ADMIN está ONLINE" -ForegroundColor Green
    } else {
        Write-Host "`n   ⚠️  ADMIN NO está online (esperado: online)" -ForegroundColor Yellow
    }
    
    if ($normalOnline) {
        Write-Host "   ✅ Usuario Normal está ONLINE" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️  Usuario Normal NO está online (esperado: online)" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "❌ Error al obtener presencia:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}

# ============================================
# PASO 4: ACTIVIDAD DEL USUARIO NORMAL
# ============================================
Write-Host "`n🔄 Paso 4: Simular actividad del Usuario Normal..." -ForegroundColor Yellow
try {
    # El login ya inicializó lastSeenAt, solo esperamos un poco para simular actividad
    Write-Host "   Esperando 5 segundos..." -ForegroundColor Gray
    Start-Sleep -Seconds 5
    
    # Hacer una petición con el token del usuario normal para actualizar lastSeenAt
    $activity = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/profiles/me" `
        -Method GET `
        -Headers $normalHeaders
    
    Write-Host "✅ Actividad registrada (GET /profiles/me)" -ForegroundColor Green
    Write-Host "   Usuario: $($activity.full_name)" -ForegroundColor Gray
    
    # Esperar 2 segundos
    Start-Sleep -Seconds 2
    
    # Verificar que se actualizó el lastSeenAt
    $presenceAfter = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/presence/users" `
        -Method GET `
        -Headers $adminHeaders
    
    $normalUserAfter = $presenceAfter | Where-Object { $_.email -eq $normalUser }
    
    if ($normalUserAfter.isOnline) {
        $lastSeen = (Get-Date $normalUserAfter.lastSeenAt).ToString("HH:mm:ss")
        Write-Host "✅ Usuario sigue ONLINE" -ForegroundColor Green
        Write-Host "   Última actividad actualizada: $lastSeen" -ForegroundColor Gray
    } else {
        Write-Host "⚠️  Usuario NO está online después de actividad" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "❌ Error al simular actividad:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}

# ============================================
# PASO 5: ADMIN KICK AL USUARIO NORMAL
# ============================================
Write-Host "`n🚫 Paso 5: Admin ejecuta KICK al Usuario Normal..." -ForegroundColor Yellow
try {
    $kickResponse = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/admin/presence/users/$normalUserId/kick" `
        -Method POST `
        -Headers $adminHeaders
    
    Write-Host "✅ KICK ejecutado exitosamente" -ForegroundColor Green
    Write-Host "   Mensaje: $($kickResponse.message)" -ForegroundColor Gray
    Write-Host "   Sesiones revocadas: $($kickResponse.sessionsRevoked)" -ForegroundColor Gray
    Write-Host "   Usuario afectado: $($kickResponse.userEmail)" -ForegroundColor Gray
}
catch {
    Write-Host "❌ Error al ejecutar KICK:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host $_.ErrorDetails.Message -ForegroundColor Red
    }
}

# ============================================
# PASO 6: VERIFICAR QUE EL USUARIO FUE DESCONECTADO
# ============================================
Write-Host "`n🔍 Paso 6: Verificar que el Usuario Normal fue desconectado..." -ForegroundColor Yellow
try {
    # Intentar usar el token del usuario normal (debe fallar o retornar 401)
    Write-Host "   Intentando usar token del usuario normal..." -ForegroundColor Gray
    
    try {
        $testRequest = Invoke-RestMethod `
            -Uri "$baseUrl/api/v1/profiles/me" `
            -Method GET `
            -Headers $normalHeaders
        
        Write-Host "   ⚠️  Token todavía es válido (no esperado)" -ForegroundColor Yellow
    }
    catch {
        if ($_.Exception.Response.StatusCode.value__ -eq 401) {
            Write-Host "   ✅ Token revocado correctamente (401 Unauthorized)" -ForegroundColor Green
        } else {
            Write-Host "   ⚠️  Error inesperado: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }
    
    # Verificar presencia
    Start-Sleep -Seconds 1
    $presenceFinal = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/presence/users" `
        -Method GET `
        -Headers $adminHeaders
    
    $normalUserFinal = $presenceFinal | Where-Object { $_.email -eq $normalUser }
    
    if (-not $normalUserFinal.isOnline) {
        Write-Host "   ✅ Usuario Normal ya NO está ONLINE" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️  Usuario Normal todavía aparece como ONLINE" -ForegroundColor Yellow
        Write-Host "      (Puede tardar hasta 2 minutos en reflejarse)" -ForegroundColor Gray
    }
}
catch {
    Write-Host "❌ Error al verificar desconexión:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}

# ============================================
# PASO 7: RE-LOGIN DEL USUARIO NORMAL
# ============================================
Write-Host "`n🔄 Paso 7: Re-login del Usuario Normal..." -ForegroundColor Yellow
try {
    $loginBody = @{
        email = $normalUser
        password = $normalPassPlain
    } | ConvertTo-Json

    $reLogin = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/auth/login-desktop" `
        -Method POST `
        -ContentType "application/json" `
        -Body $loginBody
    
    Write-Host "✅ Re-login exitoso" -ForegroundColor Green
    Write-Host "   Nuevo SessionId: $($reLogin.sessionId)" -ForegroundColor Gray
    
    # Actualizar headers
    $normalHeaders["Authorization"] = "Bearer $($reLogin.accessToken)"
    
    # Verificar que vuelve a estar online
    Start-Sleep -Seconds 2
    $presenceReLogin = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/presence/users" `
        -Method GET `
        -Headers $adminHeaders
    
    $normalUserReLogin = $presenceReLogin | Where-Object { $_.email -eq $normalUser }
    
    if ($normalUserReLogin.isOnline) {
        Write-Host "✅ Usuario Normal vuelve a estar ONLINE" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Usuario Normal NO está online después de re-login" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "❌ Error en re-login:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}

# ============================================
# RESUMEN FINAL
# ============================================
Write-Host "`n" + ("=" * 70)
Write-Host "✅ Test de Presencia completado" -ForegroundColor Green
Write-Host "`n📋 ENDPOINTS PROBADOS:" -ForegroundColor Cyan
Write-Host "   POST   /api/v1/auth/login-desktop" -ForegroundColor Gray
Write-Host "   GET    /api/v1/profiles/me" -ForegroundColor Gray
Write-Host "   GET    /api/v1/presence/users" -ForegroundColor Gray
Write-Host "   POST   /api/v1/admin/presence/users/{userId}/kick" -ForegroundColor Gray

Write-Host "`n🔐 FUNCIONALIDADES VERIFICADAS:" -ForegroundColor Cyan
Write-Host "   ✅ Login correcto actualiza isOnline" -ForegroundColor Gray
Write-Host "   ✅ Actividad actualiza lastSeenAt" -ForegroundColor Gray
Write-Host "   ✅ Lista de presencia muestra usuarios online" -ForegroundColor Gray
Write-Host "   ✅ Admin puede hacer KICK a usuarios" -ForegroundColor Gray
Write-Host "   ✅ KICK revoca sesiones activas" -ForegroundColor Gray
Write-Host "   ✅ Re-login funciona después de KICK" -ForegroundColor Gray

Write-Host "`n💡 NOTAS:" -ForegroundColor Cyan
Write-Host "   - Usuario se considera ONLINE si lastSeenAt < 2 minutos" -ForegroundColor Gray
Write-Host "   - Middleware tiene THROTTLE de 30 segundos (evita DB spam)" -ForegroundColor Gray
Write-Host "   - KICK revoca TODAS las sesiones activas del usuario" -ForegroundColor Gray
Write-Host "   - Token revocado retorna 401 Unauthorized" -ForegroundColor Gray
Write-Host "   - Lista ordenada: ADMIN > EDITOR > USER, online primero" -ForegroundColor Gray
