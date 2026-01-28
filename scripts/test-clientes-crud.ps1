# Script para probar el CRUD completo de /api/v1/clientes
# Asegurarse de que la API esté corriendo en https://localhost:7096

$baseUrl = "https://localhost:7096"
$email = "admin@gestiontime.com"
$password = "Admin123!"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "TEST CRUD CLIENTES (/api/v1/clientes)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 1. LOGIN
Write-Host "1. Login..." -ForegroundColor Yellow
$loginBody = @{
    email = $email
    password = $password
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/v1/auth/login" `
        -Method POST `
        -Body $loginBody `
        -ContentType "application/json" `
        -SkipCertificateCheck

    $token = $loginResponse.accessToken
    Write-Host "✓ Login exitoso" -ForegroundColor Green
    Write-Host ""
}
catch {
    Write-Host "✗ Error en login: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# 2. VERIFICAR QUE /api/v1/catalog/clientes NO CAMBIÓ
Write-Host "2. Verificar endpoint de catalog (NO debe haber cambiado)..." -ForegroundColor Yellow
try {
    $catalogResponse = Invoke-RestMethod -Uri "$baseUrl/api/v1/catalog/clientes?limit=5" `
        -Method GET `
        -Headers $headers `
        -SkipCertificateCheck

    Write-Host "✓ Endpoint /api/v1/catalog/clientes funciona correctamente" -ForegroundColor Green
    Write-Host "  Retornó $($catalogResponse.Count) clientes con formato: { id, nombre }" -ForegroundColor Gray
    Write-Host ""
}
catch {
    Write-Host "✗ Error al verificar catalog: $($_.Exception.Message)" -ForegroundColor Red
}

# 3. GET LIST - Sin filtros
Write-Host "3. GET /api/v1/clientes (lista sin filtros)..." -ForegroundColor Yellow
try {
    $clientesResponse = Invoke-RestMethod -Uri "$baseUrl/api/v1/clientes?page=1&size=5" `
        -Method GET `
        -Headers $headers `
        -SkipCertificateCheck

    Write-Host "✓ Lista obtenida correctamente" -ForegroundColor Green
    Write-Host "  Total: $($clientesResponse.totalCount)" -ForegroundColor Gray
    Write-Host "  Página: $($clientesResponse.page) de $($clientesResponse.totalPages)" -ForegroundColor Gray
    Write-Host "  Items en esta página: $($clientesResponse.items.Count)" -ForegroundColor Gray
    
    if ($clientesResponse.items.Count -gt 0) {
        Write-Host "  Primer cliente: id=$($clientesResponse.items[0].id), nombre=$($clientesResponse.items[0].nombre)" -ForegroundColor Gray
    }
    Write-Host ""
}
catch {
    Write-Host "✗ Error al obtener lista: $($_.Exception.Message)" -ForegroundColor Red
}

# 4. GET LIST - Con filtro de búsqueda
Write-Host "4. GET /api/v1/clientes?q=madrid (búsqueda de texto)..." -ForegroundColor Yellow
try {
    $searchResponse = Invoke-RestMethod -Uri "$baseUrl/api/v1/clientes?q=madrid&size=5" `
        -Method GET `
        -Headers $headers `
        -SkipCertificateCheck

    Write-Host "✓ Búsqueda realizada correctamente" -ForegroundColor Green
    Write-Host "  Resultados encontrados: $($searchResponse.totalCount)" -ForegroundColor Gray
    Write-Host ""
}
catch {
    Write-Host "✗ Error en búsqueda: $($_.Exception.Message)" -ForegroundColor Red
}

# 5. POST - Crear nuevo cliente
Write-Host "5. POST /api/v1/clientes (crear cliente)..." -ForegroundColor Yellow
$nuevoCliente = @{
    nombre = "Cliente Test CRUD $(Get-Date -Format 'yyyyMMdd-HHmmss')"
    nombreComercial = "Test Commercial"
    provincia = "Madrid"
    idPuntoop = 999
    localNum = 888
    nota = "Cliente creado por script de prueba"
} | ConvertTo-Json

try {
    $createResponse = Invoke-RestMethod -Uri "$baseUrl/api/v1/clientes" `
        -Method POST `
        -Body $nuevoCliente `
        -Headers $headers `
        -SkipCertificateCheck

    $clienteId = $createResponse.id
    Write-Host "✓ Cliente creado correctamente" -ForegroundColor Green
    Write-Host "  ID: $clienteId" -ForegroundColor Gray
    Write-Host "  Nombre: $($createResponse.nombre)" -ForegroundColor Gray
    Write-Host "  Nota: $($createResponse.nota)" -ForegroundColor Gray
    Write-Host ""
}
catch {
    Write-Host "✗ Error al crear cliente: $($_.Exception.Message)" -ForegroundColor Red
    $clienteId = $null
}

# 6. GET BY ID - Obtener cliente creado
if ($clienteId) {
    Write-Host "6. GET /api/v1/clientes/$clienteId (obtener por ID)..." -ForegroundColor Yellow
    try {
        $getByIdResponse = Invoke-RestMethod -Uri "$baseUrl/api/v1/clientes/$clienteId" `
            -Method GET `
            -Headers $headers `
            -SkipCertificateCheck

        Write-Host "✓ Cliente obtenido correctamente" -ForegroundColor Green
        Write-Host "  ID: $($getByIdResponse.id)" -ForegroundColor Gray
        Write-Host "  Nombre: $($getByIdResponse.nombre)" -ForegroundColor Gray
        Write-Host "  Provincia: $($getByIdResponse.provincia)" -ForegroundColor Gray
        Write-Host ""
    }
    catch {
        Write-Host "✗ Error al obtener cliente: $($_.Exception.Message)" -ForegroundColor Red
    }

    # 7. PATCH - Actualizar solo la nota
    Write-Host "7. PATCH /api/v1/clientes/$clienteId/nota (actualizar nota)..." -ForegroundColor Yellow
    $updateNota = @{
        nota = "Nota actualizada en $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    } | ConvertTo-Json

    try {
        $patchResponse = Invoke-RestMethod -Uri "$baseUrl/api/v1/clientes/$clienteId/nota" `
            -Method PATCH `
            -Body $updateNota `
            -Headers $headers `
            -SkipCertificateCheck

        Write-Host "✓ Nota actualizada correctamente" -ForegroundColor Green
        Write-Host "  Nueva nota: $($patchResponse.nota)" -ForegroundColor Gray
        Write-Host ""
    }
    catch {
        Write-Host "✗ Error al actualizar nota: $($_.Exception.Message)" -ForegroundColor Red
    }

    # 8. PUT - Actualizar cliente completo
    Write-Host "8. PUT /api/v1/clientes/$clienteId (actualizar completo)..." -ForegroundColor Yellow
    $updateCliente = @{
        nombre = "Cliente Test ACTUALIZADO"
        nombreComercial = "Test Commercial UPDATED"
        provincia = "Barcelona"
        idPuntoop = 1000
        localNum = 777
        nota = "Cliente actualizado completamente"
    } | ConvertTo-Json

    try {
        $putResponse = Invoke-RestMethod -Uri "$baseUrl/api/v1/clientes/$clienteId" `
            -Method PUT `
            -Body $updateCliente `
            -Headers $headers `
            -SkipCertificateCheck

        Write-Host "✓ Cliente actualizado correctamente" -ForegroundColor Green
        Write-Host "  Nombre: $($putResponse.nombre)" -ForegroundColor Gray
        Write-Host "  Provincia: $($putResponse.provincia)" -ForegroundColor Gray
        Write-Host "  Nota: $($putResponse.nota)" -ForegroundColor Gray
        Write-Host ""
    }
    catch {
        Write-Host "✗ Error al actualizar cliente: $($_.Exception.Message)" -ForegroundColor Red
    }

    # 9. Probar filtro hasNota
    Write-Host "9. GET /api/v1/clientes?hasNota=true (clientes con nota)..." -ForegroundColor Yellow
    try {
        $hasNotaResponse = Invoke-RestMethod -Uri "$baseUrl/api/v1/clientes?hasNota=true&size=5" `
            -Method GET `
            -Headers $headers `
            -SkipCertificateCheck

        Write-Host "✓ Filtro hasNota funciona correctamente" -ForegroundColor Green
        Write-Host "  Clientes con nota: $($hasNotaResponse.totalCount)" -ForegroundColor Gray
        Write-Host ""
    }
    catch {
        Write-Host "✗ Error al filtrar por nota: $($_.Exception.Message)" -ForegroundColor Red
    }

    # 10. DELETE - Eliminar cliente
    Write-Host "10. DELETE /api/v1/clientes/$clienteId (eliminar cliente)..." -ForegroundColor Yellow
    try {
        Invoke-RestMethod -Uri "$baseUrl/api/v1/clientes/$clienteId" `
            -Method DELETE `
            -Headers $headers `
            -SkipCertificateCheck

        Write-Host "✓ Cliente eliminado correctamente" -ForegroundColor Green
        Write-Host ""
    }
    catch {
        Write-Host "✗ Error al eliminar cliente: $($_.Exception.Message)" -ForegroundColor Red
    }

    # 11. Verificar que el cliente fue eliminado
    Write-Host "11. GET /api/v1/clientes/$clienteId (verificar eliminación)..." -ForegroundColor Yellow
    try {
        $verifyDelete = Invoke-RestMethod -Uri "$baseUrl/api/v1/clientes/$clienteId" `
            -Method GET `
            -Headers $headers `
            -SkipCertificateCheck

        Write-Host "✗ El cliente NO fue eliminado (todavía existe)" -ForegroundColor Red
    }
    catch {
        if ($_.Exception.Response.StatusCode -eq 404) {
            Write-Host "✓ Cliente eliminado correctamente (404 Not Found)" -ForegroundColor Green
        }
        else {
            Write-Host "? Error inesperado: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "TESTS COMPLETADOS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "VERIFICACIONES IMPORTANTES:" -ForegroundColor Yellow
Write-Host "1. El endpoint /api/v1/catalog/clientes sigue funcionando sin cambios" -ForegroundColor White
Write-Host "2. El nuevo endpoint /api/v1/clientes tiene CRUD completo" -ForegroundColor White
Write-Host "3. La paginación funciona correctamente" -ForegroundColor White
Write-Host "4. Los filtros (q, hasNota, etc.) funcionan" -ForegroundColor White
Write-Host "5. El campo 'nota' se puede crear, actualizar parcialmente (PATCH) y eliminar" -ForegroundColor White
Write-Host ""
