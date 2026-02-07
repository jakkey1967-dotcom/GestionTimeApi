# ========================================
# 🧪 TEST COMPLETO DE CLIENTES EN RENDER
# ========================================

$ErrorActionPreference = "Continue"
$baseUrl = "https://gestiontimeapi.onrender.com"

Write-Host "🧪 TEST: CRUD Completo de Clientes en Render" -ForegroundColor Cyan
Write-Host "=" * 60

# 1. HEALTH CHECK
Write-Host "`n🏥 Paso 1: Health Check..." -ForegroundColor Yellow
try {
    $health = Invoke-RestMethod -Uri "$baseUrl/health" -Method GET -TimeoutSec 10
    Write-Host "✅ Servicio activo" -ForegroundColor Green
    Write-Host "   Status: $($health.status)" -ForegroundColor Gray
    Write-Host "   Database: $($health.database)" -ForegroundColor Gray
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
    Write-Host "   Role: $($loginResponse.user.role)" -ForegroundColor Gray
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

$headers = @{
    "Authorization" = "Bearer $token"
    "Accept" = "application/json"
    "Content-Type" = "application/json"
}

# 3. GET - LISTAR CLIENTES (CON PAGINACIÓN)
Write-Host "`n📋 Paso 3: Listar clientes (paginación)..." -ForegroundColor Yellow
try {
    $clientesResponse = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/clientes?page=1&pageSize=10" `
        -Method GET `
        -Headers $headers
    
    Write-Host "✅ Clientes obtenidos:" -ForegroundColor Green
    Write-Host "   Total: $($clientesResponse.totalItems)" -ForegroundColor Gray
    Write-Host "   Página actual: $($clientesResponse.currentPage)" -ForegroundColor Gray
    Write-Host "   Items en página: $($clientesResponse.items.Count)" -ForegroundColor Gray
    
    if ($clientesResponse.items.Count -gt 0) {
        Write-Host "`n   Primeros 3 clientes:" -ForegroundColor Gray
        $clientesResponse.items | Select-Object -First 3 | ForEach-Object {
            Write-Host "     - ID: $($_.id) | Nombre: $($_.nombre) | CIF: $($_.cif)" -ForegroundColor Gray
        }
    }
}
catch {
    Write-Host "❌ Error listando clientes:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}

# 4. GET - BUSCAR CLIENTES POR TÉRMINO
Write-Host "`n🔍 Paso 4: Buscar clientes por término..." -ForegroundColor Yellow
try {
    $searchResponse = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/clientes?search=Global&page=1&pageSize=5" `
        -Method GET `
        -Headers $headers
    
    Write-Host "✅ Búsqueda completada:" -ForegroundColor Green
    Write-Host "   Resultados encontrados: $($searchResponse.totalItems)" -ForegroundColor Gray
    
    if ($searchResponse.items.Count -gt 0) {
        $searchResponse.items | ForEach-Object {
            Write-Host "     - $($_.nombre) ($($_.cif))" -ForegroundColor Gray
        }
    }
}
catch {
    Write-Host "❌ Error en búsqueda:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}

# 5. POST - CREAR CLIENTE
Write-Host "`n➕ Paso 5: Crear nuevo cliente..." -ForegroundColor Yellow
$timestamp = Get-Date -Format "HHmmss"
$nuevoCliente = @{
    nombre = "Test Cliente $timestamp"
    cif = "T$timestamp"
    direccion = "Calle Test 123"
    email = "test$timestamp@example.com"
    telefono = "666777888"
    contacto = "Test Contact"
    nota = "Cliente creado desde test automatizado"
} | ConvertTo-Json

try {
    $createResponse = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/clientes" `
        -Method POST `
        -Headers $headers `
        -Body $nuevoCliente
    
    $clienteId = $createResponse.id
    Write-Host "✅ Cliente creado exitosamente:" -ForegroundColor Green
    Write-Host "   ID: $clienteId" -ForegroundColor Gray
    Write-Host "   Nombre: $($createResponse.nombre)" -ForegroundColor Gray
    Write-Host "   CIF: $($createResponse.cif)" -ForegroundColor Gray
}
catch {
    Write-Host "❌ Error al crear cliente:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host $_.ErrorDetails.Message -ForegroundColor Red
    }
    $clienteId = $null
}

# 6. GET - OBTENER CLIENTE POR ID
if ($clienteId) {
    Write-Host "`n🔍 Paso 6: Obtener cliente por ID..." -ForegroundColor Yellow
    try {
        $clienteDetail = Invoke-RestMethod `
            -Uri "$baseUrl/api/v1/clientes/$clienteId" `
            -Method GET `
            -Headers $headers
        
        Write-Host "✅ Cliente obtenido:" -ForegroundColor Green
        Write-Host ($clienteDetail | ConvertTo-Json -Depth 2) -ForegroundColor Gray
    }
    catch {
        Write-Host "❌ Error al obtener cliente:" -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor Red
    }

    # 7. PUT - ACTUALIZAR CLIENTE
    Write-Host "`n✏️  Paso 7: Actualizar cliente completo (PUT)..." -ForegroundColor Yellow
    $clienteActualizado = @{
        nombre = "Test Cliente $timestamp ACTUALIZADO"
        cif = "T$timestamp"
        direccion = "Calle Test 456 NUEVA"
        email = "updated$timestamp@example.com"
        telefono = "999888777"
        contacto = "Contact Updated"
        nota = "Cliente actualizado por PUT"
    } | ConvertTo-Json

    try {
        $updateResponse = Invoke-RestMethod `
            -Uri "$baseUrl/api/v1/clientes/$clienteId" `
            -Method PUT `
            -Headers $headers `
            -Body $clienteActualizado
        
        Write-Host "✅ Cliente actualizado (PUT):" -ForegroundColor Green
        Write-Host "   Nombre: $($updateResponse.nombre)" -ForegroundColor Gray
        Write-Host "   Email: $($updateResponse.email)" -ForegroundColor Gray
        Write-Host "   Dirección: $($updateResponse.direccion)" -ForegroundColor Gray
    }
    catch {
        Write-Host "❌ Error al actualizar cliente:" -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor Red
    }

    # 8. PATCH - ACTUALIZAR SOLO NOTA
    Write-Host "`n📝 Paso 8: Actualizar solo nota (PATCH)..." -ForegroundColor Yellow
    $notaUpdate = @{
        nota = "Nota actualizada mediante PATCH - Test $(Get-Date -Format 'HH:mm:ss')"
    } | ConvertTo-Json

    try {
        $patchResponse = Invoke-RestMethod `
            -Uri "$baseUrl/api/v1/clientes/$clienteId/nota" `
            -Method PATCH `
            -Headers $headers `
            -Body $notaUpdate
        
        Write-Host "✅ Nota actualizada (PATCH):" -ForegroundColor Green
        Write-Host "   Nueva nota: $($patchResponse.nota)" -ForegroundColor Gray
    }
    catch {
        Write-Host "❌ Error al actualizar nota:" -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor Red
    }

    # 9. DELETE - ELIMINAR CLIENTE
    Write-Host "`n🗑️  Paso 9: Eliminar cliente..." -ForegroundColor Yellow
    try {
        Invoke-RestMethod `
            -Uri "$baseUrl/api/v1/clientes/$clienteId" `
            -Method DELETE `
            -Headers $headers | Out-Null
        
        Write-Host "✅ Cliente eliminado correctamente" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ Error al eliminar cliente:" -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor Red
    }

    # 10. VERIFICAR QUE SE ELIMINÓ
    Write-Host "`n🔍 Paso 10: Verificar eliminación..." -ForegroundColor Yellow
    try {
        $deleted = Invoke-RestMethod `
            -Uri "$baseUrl/api/v1/clientes/$clienteId" `
            -Method GET `
            -Headers $headers
        
        Write-Host "⚠️  Cliente aún existe (debería estar eliminado)" -ForegroundColor Yellow
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        if ($statusCode -eq 404) {
            Write-Host "✅ Verificado: Cliente eliminado correctamente (404 Not Found)" -ForegroundColor Green
        }
        else {
            Write-Host "❌ Error inesperado: $statusCode" -ForegroundColor Red
        }
    }
}

# 11. ESTADÍSTICAS FINALES
Write-Host "`n📊 Paso 11: Estadísticas finales..." -ForegroundColor Yellow
try {
    $allClientes = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/clientes?page=1&pageSize=1000" `
        -Method GET `
        -Headers $headers
    
    Write-Host "✅ Total de clientes en BD: $($allClientes.totalItems)" -ForegroundColor Green
}
catch {
    Write-Host "⚠️  No se pudo obtener estadísticas" -ForegroundColor Yellow
}

Write-Host "`n" + ("=" * 60)
Write-Host "✅ TEST DE CLIENTES COMPLETADO" -ForegroundColor Green
Write-Host ("=" * 60)
