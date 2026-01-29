# Test CRUD Tipos y Grupos

# Forzar UTF-8 encoding
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

# ✅ Ignorar certificados SSL auto-firmados en desarrollo
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
                    delegate(
                        Object obj, 
                        X509Certificate certificate, 
                        X509Chain chain, 
                        SslPolicyErrors errors
                    ) {
                        return true;
                    };
            }
        }
    }
"@
    Add-Type $certCallback
}
[ServerCertificateValidationCallback]::Ignore()

$baseUrl = "https://localhost:2502/api/v1"

# ✅ Credenciales correctas
$EMAIL = "psantos@global-retail.com"
$PASSWORD = "12345678"

# Primero hacer login-desktop para obtener el token
Write-Host "🔐 Haciendo login desktop..." -ForegroundColor Cyan
$loginBody = @{
    email = $EMAIL
    password = $PASSWORD
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod `
        -Uri "$baseUrl/auth/login-desktop" `
        -Method POST `
        -ContentType "application/json" `
        -Body $loginBody
    
    $accessToken = $loginResponse.accessToken
    Write-Host "✅ Login exitoso - Token obtenido" -ForegroundColor Green
    
    # Crear headers con el token (sin Content-Type aquí, se añade por request)
    $headers = @{
        "Authorization" = "Bearer $accessToken"
    }
} catch {
    Write-Host "❌ Login falló: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "`n================================================" -ForegroundColor Yellow
Write-Host "TESTING TIPOS CRUD" -ForegroundColor Yellow
Write-Host "================================================`n" -ForegroundColor Yellow

# 1. Listar tipos iniciales
Write-Host "📋 1. GET /api/v1/tipos - Listar todos" -ForegroundColor Cyan
$response = Invoke-RestMethod -Uri "$baseUrl/tipos" -Method GET -Headers $headers
Write-Host "Status: 200 OK" -ForegroundColor Green
Write-Host "Cantidad de tipos: $($response.Count)" -ForegroundColor Yellow
$response | Format-Table -AutoSize

Start-Sleep -Milliseconds 500

# 2. Crear un nuevo tipo
Write-Host "`n📝 2. POST /api/v1/tipos - Crear nuevo tipo" -ForegroundColor Cyan
$newTipo = @{
    nombre = "Instalación"
    descripcion = "Trabajos de instalación de equipos"
} | ConvertTo-Json

try {
    $createdTipo = Invoke-RestMethod -Uri "$baseUrl/tipos" -Method POST -Headers $headers -Body $newTipo -ContentType "application/json"
    Write-Host "Status: 201 Created" -ForegroundColor Green
    Write-Host "Tipo creado: ID=$($createdTipo.id), Nombre=$($createdTipo.nombre)" -ForegroundColor Yellow
    $tipoId = $createdTipo.id
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    $tipoId = 0
}

Start-Sleep -Milliseconds 500

# 3. Obtener el tipo creado
if ($tipoId -gt 0) {
    Write-Host "`n🔍 3. GET /api/v1/tipos/{id} - Obtener tipo por ID" -ForegroundColor Cyan
    $tipo = Invoke-RestMethod -Uri "$baseUrl/tipos/$tipoId" -Method GET -Headers $headers
    Write-Host "Status: 200 OK" -ForegroundColor Green
    $tipo | Format-List
}

Start-Sleep -Milliseconds 500

# 4. Actualizar el tipo
if ($tipoId -gt 0) {
    Write-Host "`n✏️  4. PUT /api/v1/tipos/{id} - Actualizar tipo" -ForegroundColor Cyan
    $updateTipo = @{
        nombre = "Instalación y Configuración"
        descripcion = "Trabajos de instalación y configuración de equipos"
    } | ConvertTo-Json

    try {
        $updatedTipo = Invoke-RestMethod -Uri "$baseUrl/tipos/$tipoId" -Method PUT -Headers $headers -Body $updateTipo -ContentType "application/json"
        Write-Host "Status: 200 OK" -ForegroundColor Green
        Write-Host "Tipo actualizado:" -ForegroundColor Yellow
        $updatedTipo | Format-List
    } catch {
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Start-Sleep -Milliseconds 500

# 5. Probar crear tipo duplicado (debe fallar con 409)
Write-Host "`n❌ 5. POST /api/v1/tipos - Intentar crear duplicado (debe fallar)" -ForegroundColor Cyan
$dupTipo = @{
    nombre = "Instalación y Configuración"
    descripcion = "Prueba de duplicado"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/tipos" -Method POST -Headers $headers -Body $dupTipo -ContentType "application/json"
    Write-Host "⚠️  No debería llegar aquí - debería fallar con 409" -ForegroundColor Yellow
} catch {
    if ($_.Exception.Response.StatusCode -eq 409 -or $_.Exception.Message -contains "409") {
        Write-Host "✅ Correcto: 409 Conflict - Ya existe" -ForegroundColor Green
    } else {
        Write-Host "❌ Error inesperado: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Start-Sleep -Milliseconds 500

# 6. Eliminar el tipo (si no está en uso)
if ($tipoId -gt 0) {
    Write-Host "`n🗑️  6. DELETE /api/v1/tipos/{id} - Eliminar tipo" -ForegroundColor Cyan
    try {
        Invoke-RestMethod -Uri "$baseUrl/tipos/$tipoId" -Method DELETE -Headers $headers
        Write-Host "Status: 204 No Content - Tipo eliminado" -ForegroundColor Green
    } catch {
        if ($_.Exception.Response.StatusCode -eq 409 -or $_.Exception.Message -contains "409") {
            Write-Host "⚠️  409 Conflict - El tipo está en uso, no se puede eliminar" -ForegroundColor Yellow
        } else {
            Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

Write-Host "`n================================================" -ForegroundColor Yellow
Write-Host "TESTING GRUPOS CRUD" -ForegroundColor Yellow
Write-Host "================================================`n" -ForegroundColor Yellow

# 1. Listar grupos iniciales
Write-Host "📋 1. GET /api/v1/grupos - Listar todos" -ForegroundColor Cyan
$response = Invoke-RestMethod -Uri "$baseUrl/grupos" -Method GET -Headers $headers
Write-Host "Status: 200 OK" -ForegroundColor Green
Write-Host "Cantidad de grupos: $($response.Count)" -ForegroundColor Yellow
$response | Format-Table -AutoSize

Start-Sleep -Milliseconds 500

# 2. Crear un nuevo grupo
Write-Host "`n📝 2. POST /api/v1/grupos - Crear nuevo grupo" -ForegroundColor Cyan
$newGrupo = @{
    nombre = "Soporte Premium"
    descripcion = "Clientes con soporte premium 24/7"
} | ConvertTo-Json

try {
    $createdGrupo = Invoke-RestMethod -Uri "$baseUrl/grupos" -Method POST -Headers $headers -Body $newGrupo -ContentType "application/json"
    Write-Host "Status: 201 Created" -ForegroundColor Green
    Write-Host "Grupo creado: ID=$($createdGrupo.id), Nombre=$($createdGrupo.nombre)" -ForegroundColor Yellow
    $grupoId = $createdGrupo.id
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    $grupoId = 0
}

Start-Sleep -Milliseconds 500

# 3. Obtener el grupo creado
if ($grupoId -gt 0) {
    Write-Host "`n🔍 3. GET /api/v1/grupos/{id} - Obtener grupo por ID" -ForegroundColor Cyan
    $grupo = Invoke-RestMethod -Uri "$baseUrl/grupos/$grupoId" -Method GET -Headers $headers
    Write-Host "Status: 200 OK" -ForegroundColor Green
    $grupo | Format-List
}

Start-Sleep -Milliseconds 500

# 4. Actualizar el grupo
if ($grupoId -gt 0) {
    Write-Host "`n✏️  4. PUT /api/v1/grupos/{id} - Actualizar grupo" -ForegroundColor Cyan
    $updateGrupo = @{
        Nombre = "Soporte VIP"
        Descripcion = "Clientes VIP con atención prioritaria"
    } | ConvertTo-Json

    try {
        $updatedGrupo = Invoke-RestMethod -Uri "$baseUrl/grupos/$grupoId" -Method PUT -Headers $headers -Body $updateGrupo -ContentType "application/json"
        Write-Host "Status: 200 OK" -ForegroundColor Green
        Write-Host "Grupo actualizado:" -ForegroundColor Yellow
        $updatedGrupo | Format-List
    } catch {
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Start-Sleep -Milliseconds 500

# 5. Eliminar el grupo (si no está en uso)
if ($grupoId -gt 0) {
    Write-Host "`n🗑️  5. DELETE /api/v1/grupos/{id} - Eliminar grupo" -ForegroundColor Cyan
    try {
        Invoke-RestMethod -Uri "$baseUrl/grupos/$grupoId" -Method DELETE -Headers $headers
        Write-Host "Status: 204 No Content - Grupo eliminado" -ForegroundColor Green
    } catch {
        if ($_.Exception.Response.StatusCode -eq 409 -or $_.Exception.Message -contains "409") {
            Write-Host "⚠️  409 Conflict - El grupo está en uso, no se puede eliminar" -ForegroundColor Yellow
        } else {
            Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

Write-Host "`n================================================" -ForegroundColor Yellow
Write-Host "✅ TESTS COMPLETADOS" -ForegroundColor Green
Write-Host "================================================`n" -ForegroundColor Yellow
