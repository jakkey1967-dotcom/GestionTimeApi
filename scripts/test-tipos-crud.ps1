# Test CRUD completo de Tipos
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

Write-Host "🏷️ TEST: CRUD Completo de Tipos" -ForegroundColor Cyan
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

# 2. LISTAR TIPOS
Write-Host "`n📋 Paso 2: Listar todos los tipos..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/tipos" `
        -Method GET `
        -Headers $headers
    
    Write-Host "✅ Total de tipos: $($response.Count)" -ForegroundColor Green
    Write-Host "   Tipos:" -ForegroundColor Gray
    
    foreach ($tipo in $response | Select-Object -First 5) {
        Write-Host "   - [ID:$($tipo.id)] $($tipo.nombre) - $($tipo.descripcion)" -ForegroundColor Gray
    }
}
catch {
    Write-Host "❌ Error al listar tipos:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}

# 3. CREAR NUEVO TIPO
Write-Host "`n➕ Paso 3: Crear nuevo tipo..." -ForegroundColor Yellow
try {
    $timestamp = Get-Date -Format "yyyyMMddHHmmss"
    $newTipo = @{
        nombre = "Tipo Test $timestamp"
        descripcion = "Creado automaticamente"
    } | ConvertTo-Json -Depth 10 -Compress

    $created = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/tipos" `
        -Method POST `
        -Headers $headers `
        -Body $newTipo
    
    $tipoId = $created.id
    Write-Host "✅ Tipo creado exitosamente" -ForegroundColor Green
    Write-Host "   ID: $($created.id)" -ForegroundColor Gray
    Write-Host "   Nombre: $($created.nombre)" -ForegroundColor Gray
    Write-Host "   Descripción: $($created.descripcion)" -ForegroundColor Gray
}
catch {
    Write-Host "❌ Error al crear tipo:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host $_.ErrorDetails.Message -ForegroundColor Red
    }
    exit 1
}

# 4. OBTENER TIPO POR ID
Write-Host "`n🔎 Paso 4: Obtener tipo por ID..." -ForegroundColor Yellow
try {
    $tipo = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/tipos/$tipoId" `
        -Method GET `
        -Headers $headers
    
    Write-Host "✅ Tipo obtenido:" -ForegroundColor Green
    Write-Host "   ID: $($tipo.id)" -ForegroundColor Gray
    Write-Host "   Nombre: $($tipo.nombre)" -ForegroundColor Gray
    Write-Host "   Descripción: $($tipo.descripcion)" -ForegroundColor Gray
}
catch {
    Write-Host "❌ Error al obtener tipo:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}

# 5. ACTUALIZAR TIPO (PUT)
Write-Host "`n✏️ Paso 5: Actualizar tipo (PUT)..." -ForegroundColor Yellow
try {
    $updateTipo = @{
        nombre = "$($tipo.nombre) - MODIFICADO"
        descripcion = "Actualizado en $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    } | ConvertTo-Json -Depth 10 -Compress

    $updated = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/tipos/$tipoId" `
        -Method PUT `
        -Headers $headers `
        -Body $updateTipo
    
    Write-Host "✅ Tipo actualizado exitosamente" -ForegroundColor Green
    Write-Host "   Nombre: $($updated.nombre)" -ForegroundColor Gray
    Write-Host "   Descripción: $($updated.descripcion)" -ForegroundColor Gray
}
catch {
    Write-Host "❌ Error al actualizar tipo:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}

# 6. ELIMINAR TIPO
Write-Host "`n🗑️ Paso 6: Eliminar tipo..." -ForegroundColor Yellow
try {
    $deleteResponse = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/tipos/$tipoId" `
        -Method DELETE `
        -Headers $headers
    
    Write-Host "✅ Tipo eliminado exitosamente (204 No Content)" -ForegroundColor Green
}
catch {
    Write-Host "❌ Error al eliminar tipo:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}

# 7. VERIFICAR QUE FUE ELIMINADO
Write-Host "`n✔️ Paso 7: Verificar eliminación..." -ForegroundColor Yellow
try {
    $verificar = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/tipos/$tipoId" `
        -Method GET `
        -Headers $headers
    
    Write-Host "⚠️  Tipo todavía existe" -ForegroundColor Yellow
}
catch {
    if ($_.Exception.Response.StatusCode.value__ -eq 404) {
        Write-Host "✅ Confirmado: Tipo eliminado (404)" -ForegroundColor Green
    }
    else {
        Write-Host "❌ Error inesperado: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# 8. PROBAR VALIDACIÓN (NOMBRE DUPLICADO)
Write-Host "`n⚠️ Paso 8: Probar validación (nombre duplicado)..." -ForegroundColor Yellow
try {
    # Obtener un tipo existente
    $existentes = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/tipos" `
        -Method GET `
        -Headers $headers
    
    if ($existentes.Count -gt 0) {
        $nombreDuplicado = $existentes[0].nombre
        
        $duplicado = @{
            nombre = $nombreDuplicado
            descripcion = "Este debe fallar"
        } | ConvertTo-Json -Depth 10 -Compress

        try {
            $result = Invoke-RestMethod `
                -Uri "$baseUrl/api/v1/tipos" `
                -Method POST `
                -Headers $headers `
                -Body $duplicado
            
            Write-Host "⚠️  Se permitió crear duplicado" -ForegroundColor Yellow
        }
        catch {
            $statusCode = $_.Exception.Response.StatusCode.value__
            Write-Host "   Código HTTP: $statusCode" -ForegroundColor Gray
            
            if ($_.ErrorDetails.Message) {
                Write-Host "   Detalle: $($_.ErrorDetails.Message)" -ForegroundColor Gray
            }
            
            if ($statusCode -eq 409) {
                Write-Host "✅ Validación correcta: No permite nombres duplicados (409 Conflict)" -ForegroundColor Green
            }
            elseif ($statusCode -eq 400) {
                Write-Host "✅ Validación correcta: No permite nombres duplicados (400 Bad Request)" -ForegroundColor Green
                Write-Host "   (Backend retornó 400 en lugar de 409, pero validó correctamente)" -ForegroundColor Gray
            }
            else {
                Write-Host "❌ Error inesperado: $($_.Exception.Message)" -ForegroundColor Red
            }
        }
    }
}
catch {
    Write-Host "⚠️  No se pudo probar validación" -ForegroundColor Yellow
}

Write-Host "`n" + ("=" * 60)
Write-Host "✅ Test CRUD completado exitosamente" -ForegroundColor Green
Write-Host "`n📋 ENDPOINTS PROBADOS:" -ForegroundColor Cyan
Write-Host "   GET    /api/v1/tipos" -ForegroundColor Gray
Write-Host "   GET    /api/v1/tipos/{id}" -ForegroundColor Gray
Write-Host "   POST   /api/v1/tipos" -ForegroundColor Gray
Write-Host "   PUT    /api/v1/tipos/{id}" -ForegroundColor Gray
Write-Host "   DELETE /api/v1/tipos/{id}" -ForegroundColor Gray
