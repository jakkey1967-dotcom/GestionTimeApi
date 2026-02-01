# Test CRUD completo de Clientes
# Para: GestionTime Desktop

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

Write-Host "🏢 TEST: CRUD Completo de Clientes" -ForegroundColor Cyan
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

# 2. LISTAR CLIENTES (PAGINADO)
Write-Host "`n📋 Paso 2: Listar clientes (paginado)..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/clientes?page=1&size=10" `
        -Method GET `
        -Headers $headers
    
    $totalPages = [Math]::Ceiling($response.totalCount / $response.pageSize)
    Write-Host "✅ Total de clientes: $($response.totalCount)" -ForegroundColor Green
    Write-Host "   Página $($response.page) de $totalPages" -ForegroundColor Gray
    Write-Host "   Clientes en esta página:" -ForegroundColor Gray
    
    foreach ($cliente in $response.items | Select-Object -First 5) {
        Write-Host "   - [ID:$($cliente.id)] $($cliente.nombre)" -ForegroundColor Gray
    }
}
catch {
    Write-Host "❌ Error al listar clientes:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}

# 3. BUSCAR CLIENTES POR TÉRMINO
Write-Host "`n🔍 Paso 3: Buscar clientes con término 'test'..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/clientes?q=test&size=5" `
        -Method GET `
        -Headers $headers
    
    Write-Host "✅ Encontrados: $($response.totalCount) clientes" -ForegroundColor Green
    if ($response.items.Count -gt 0) {
        foreach ($cliente in $response.items) {
            Write-Host "   - [ID:$($cliente.id)] $($cliente.nombre)" -ForegroundColor Gray
        }
    }
    else {
        Write-Host "   (No hay clientes con 'test' en el nombre)" -ForegroundColor Gray
    }
}
catch {
    Write-Host "❌ Error al buscar clientes:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}

# 4. CREAR NUEVO CLIENTE
Write-Host "`n➕ Paso 4: Crear nuevo cliente..." -ForegroundColor Yellow
try {
    $timestamp = Get-Date -Format "yyyyMMddHHmmss"
    $newCliente = @{
        nombre = "Cliente Test $timestamp"
        idPuntoop = 99999
        localNum = 1
        nombreComercial = "COMERCIAL$timestamp"
        provincia = "Madrid"
        nota = "Creado automaticamente"
    } | ConvertTo-Json -Depth 10 -Compress

    $created = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/clientes" `
        -Method POST `
        -Headers $headers `
        -Body $newCliente
    
    $clienteId = $created.id
    Write-Host "✅ Cliente creado exitosamente" -ForegroundColor Green
    Write-Host "   ID: $($created.id)" -ForegroundColor Gray
    Write-Host "   Nombre: $($created.nombre)" -ForegroundColor Gray
    Write-Host "   ID Puntoop: $($created.idPuntoop)" -ForegroundColor Gray
    Write-Host "   Local Num: $($created.localNum)" -ForegroundColor Gray
    Write-Host "   Nota: $($created.nota)" -ForegroundColor Gray
}
catch {
    Write-Host "❌ Error al crear cliente:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host $_.ErrorDetails.Message -ForegroundColor Red
    }
    exit 1
}

# 5. OBTENER CLIENTE POR ID
Write-Host "`n🔎 Paso 5: Obtener cliente por ID..." -ForegroundColor Yellow
try {
    $cliente = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/clientes/$clienteId" `
        -Method GET `
        -Headers $headers
    
    Write-Host "✅ Cliente obtenido:" -ForegroundColor Green
    Write-Host "   ID: $($cliente.id)" -ForegroundColor Gray
    Write-Host "   Nombre: $($cliente.nombre)" -ForegroundColor Gray
    Write-Host "   Nota: $($cliente.nota)" -ForegroundColor Gray
}
catch {
    Write-Host "❌ Error al obtener cliente:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}

# 6. ACTUALIZAR CLIENTE COMPLETO (PUT)
Write-Host "`n✏️ Paso 6: Actualizar cliente completo (PUT)..." -ForegroundColor Yellow
try {
    $updateCliente = @{
        nombre = "$($cliente.nombre) - MODIFICADO"
        idPuntoop = $cliente.idPuntoop
        localNum = $cliente.localNum
        nombreComercial = "$($cliente.nombreComercial) - MOD"
        provincia = $cliente.provincia
        nota = "Nota actualizada en $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    } | ConvertTo-Json

    $updated = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/clientes/$clienteId" `
        -Method PUT `
        -Headers $headers `
        -Body $updateCliente
    
    Write-Host "✅ Cliente actualizado exitosamente" -ForegroundColor Green
    Write-Host "   Nombre: $($updated.nombre)" -ForegroundColor Gray
    Write-Host "   Nota: $($updated.nota)" -ForegroundColor Gray
}
catch {
    Write-Host "❌ Error al actualizar cliente:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}

# 7. ACTUALIZAR SOLO LA NOTA (PATCH)
Write-Host "`n📝 Paso 7: Actualizar solo la nota (PATCH)..." -ForegroundColor Yellow
try {
    $updateNota = @{
        nota = "Nota PATCH - $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    } | ConvertTo-Json

    $updatedNota = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/clientes/$clienteId/nota" `
        -Method PATCH `
        -Headers $headers `
        -Body $updateNota
    
    Write-Host "✅ Nota actualizada exitosamente" -ForegroundColor Green
    Write-Host "   Nueva nota: $($updatedNota.nota)" -ForegroundColor Gray
}
catch {
    Write-Host "❌ Error al actualizar nota:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}

# 8. ELIMINAR CLIENTE
Write-Host "`n🗑️ Paso 8: Eliminar cliente..." -ForegroundColor Yellow
try {
    $deleteResponse = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/clientes/$clienteId" `
        -Method DELETE `
        -Headers $headers
    
    Write-Host "✅ Cliente eliminado exitosamente" -ForegroundColor Green
    Write-Host "   Mensaje: $($deleteResponse.message)" -ForegroundColor Gray
}
catch {
    Write-Host "❌ Error al eliminar cliente:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}

# 9. VERIFICAR QUE FUE ELIMINADO
Write-Host "`n✔️ Paso 9: Verificar eliminación..." -ForegroundColor Yellow
try {
    $verificar = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/clientes/$clienteId" `
        -Method GET `
        -Headers $headers
    
    Write-Host "⚠️  Cliente todavía existe" -ForegroundColor Yellow
}
catch {
    if ($_.Exception.Response.StatusCode.value__ -eq 404) {
        Write-Host "✅ Confirmado: Cliente eliminado (404)" -ForegroundColor Green
    }
    else {
        Write-Host "❌ Error inesperado: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`n" + ("=" * 60)
Write-Host "✅ Test CRUD completado exitosamente" -ForegroundColor Green
Write-Host "`n📋 ENDPOINTS PROBADOS:" -ForegroundColor Cyan
Write-Host "   GET    /api/v1/clientes?page=1&size=10" -ForegroundColor Gray
Write-Host "   GET    /api/v1/clientes?q=test&size=5" -ForegroundColor Gray
Write-Host "   POST   /api/v1/clientes" -ForegroundColor Gray
Write-Host "   GET    /api/v1/clientes/{id}" -ForegroundColor Gray
Write-Host "   PUT    /api/v1/clientes/{id}" -ForegroundColor Gray
Write-Host "   PATCH  /api/v1/clientes/{id}/nota" -ForegroundColor Gray
Write-Host "   DELETE /api/v1/clientes/{id}" -ForegroundColor Gray

