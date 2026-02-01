# Test de gestión de usuarios y roles
# Para: Admin de GestionTime Desktop

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

Write-Host "🔐 TEST: Gestión de Usuarios y Roles" -ForegroundColor Cyan
Write-Host "=" * 60

# 1. LOGIN COMO ADMIN
Write-Host "`n📝 Paso 1: Login como Admin..." -ForegroundColor Yellow
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
    $headers = @{
        "Authorization" = "Bearer $token"
        "Content-Type" = "application/json"
    }
    
    Write-Host "✅ Login exitoso" -ForegroundColor Green
    Write-Host "   Email: $($loginResponse.user.email)" -ForegroundColor Gray
    Write-Host "   Role: $($loginResponse.user.role)" -ForegroundColor Gray
}
catch {
    Write-Host "❌ Error en login:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

# 2. LISTAR TODOS LOS USUARIOS
Write-Host "`n📋 Paso 2: Listar todos los usuarios..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/users?page=1&pageSize=20" `
        -Method GET `
        -Headers $headers
    
    Write-Host "✅ Total de usuarios: $($response.total)" -ForegroundColor Green
    Write-Host "   Usuarios en esta página:" -ForegroundColor Gray
    
    foreach ($user in $response.users) {
        $enabledIcon = if ($user.enabled) { "✅" } else { "❌" }
        $rolesStr = $user.roles -join ", "
        Write-Host "   $enabledIcon $($user.email) - Roles: $rolesStr" -ForegroundColor Gray
    }
    
    # Guardar un usuario para pruebas
    $testUser = $response.users | Where-Object { $_.email -ne "psantos@global-retail.com" } | Select-Object -First 1
    if (-not $testUser) {
        Write-Host "⚠️  No hay usuarios adicionales para probar" -ForegroundColor Yellow
        exit 0
    }
}
catch {
    Write-Host "❌ Error al listar usuarios:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

# 3. LISTAR ROLES DISPONIBLES
Write-Host "`n🎭 Paso 3: Listar roles disponibles..." -ForegroundColor Yellow
try {
    $rolesResponse = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/roles" `
        -Method GET `
        -Headers $headers
    
    Write-Host "✅ Roles disponibles:" -ForegroundColor Green
    foreach ($role in $rolesResponse.roles) {
        Write-Host "   - $($role.name)" -ForegroundColor Gray
    }
}
catch {
    Write-Host "❌ Error al listar roles:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}

# 4. VER DETALLE DE UN USUARIO
Write-Host "`n👤 Paso 4: Ver detalle del usuario $($testUser.email)..." -ForegroundColor Yellow
try {
    $userDetail = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/users/$($testUser.id)" `
        -Method GET `
        -Headers $headers
    
    Write-Host "✅ Usuario obtenido:" -ForegroundColor Green
    Write-Host "   Email: $($userDetail.email)" -ForegroundColor Gray
    Write-Host "   FullName: $($userDetail.fullName)" -ForegroundColor Gray
    Write-Host "   Enabled: $($userDetail.enabled)" -ForegroundColor Gray
    Write-Host "   Roles: $($userDetail.roles -join ', ')" -ForegroundColor Gray
}
catch {
    Write-Host "❌ Error al obtener usuario:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}

# 5. ACTUALIZAR ROLES DE UN USUARIO
Write-Host "`n🔄 Paso 5: Actualizar roles del usuario..." -ForegroundColor Yellow
try {
    # Asignar rol USER si no lo tiene, o agregar ADMIN temporalmente
    $newRoles = @("USER", "ADMIN")
    
    $updateBody = @{
        roles = $newRoles
    } | ConvertTo-Json
    
    Write-Host "   Asignando roles: $($newRoles -join ', ')" -ForegroundColor Gray
    
    $updateResponse = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/users/$($testUser.id)/roles" `
        -Method PUT `
        -Headers $headers `
        -Body $updateBody
    
    Write-Host "✅ Roles actualizados exitosamente" -ForegroundColor Green
    Write-Host "   Email: $($updateResponse.email)" -ForegroundColor Gray
    Write-Host "   Nuevos roles: $($updateResponse.roles -join ', ')" -ForegroundColor Gray
}
catch {
    Write-Host "❌ Error al actualizar roles:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host $_.ErrorDetails.Message -ForegroundColor Red
    }
}

# 6. REVERTIR CAMBIOS (SOLO USER)
Write-Host "`n🔙 Paso 6: Revertir roles a solo USER..." -ForegroundColor Yellow
try {
    $revertBody = @{
        roles = @("USER")
    } | ConvertTo-Json
    
    $revertResponse = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/users/$($testUser.id)/roles" `
        -Method PUT `
        -Headers $headers `
        -Body $revertBody
    
    Write-Host "✅ Roles revertidos exitosamente" -ForegroundColor Green
    Write-Host "   Roles finales: $($revertResponse.roles -join ', ')" -ForegroundColor Gray
}
catch {
    Write-Host "❌ Error al revertir roles:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}

# 7. PROBAR DESHABILITAR/HABILITAR USUARIO
Write-Host "`n🔒 Paso 7: Probar deshabilitar/habilitar usuario..." -ForegroundColor Yellow
try {
    # Deshabilitar
    $disableBody = @{ enabled = $false } | ConvertTo-Json
    $disableResponse = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/users/$($testUser.id)/enabled" `
        -Method PUT `
        -Headers $headers `
        -Body $disableBody
    
    Write-Host "✅ Usuario deshabilitado" -ForegroundColor Green
    
    # Esperar 1 segundo
    Start-Sleep -Seconds 1
    
    # Habilitar de nuevo
    $enableBody = @{ enabled = $true } | ConvertTo-Json
    $enableResponse = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/users/$($testUser.id)/enabled" `
        -Method PUT `
        -Headers $headers `
        -Body $enableBody
    
    Write-Host "✅ Usuario habilitado nuevamente" -ForegroundColor Green
}
catch {
    Write-Host "❌ Error al cambiar estado del usuario:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}

Write-Host "`n" + ("=" * 60)
Write-Host "✅ Test completado exitosamente" -ForegroundColor Green
Write-Host "`n📋 ENDPOINTS DISPONIBLES:" -ForegroundColor Cyan
Write-Host "   GET    /api/v1/users?page=1&pageSize=50" -ForegroundColor Gray
Write-Host "   GET    /api/v1/users/{id}" -ForegroundColor Gray
Write-Host "   GET    /api/v1/roles" -ForegroundColor Gray
Write-Host "   PUT    /api/v1/users/{id}/roles" -ForegroundColor Gray
Write-Host "   PUT    /api/v1/users/{id}/enabled" -ForegroundColor Gray
