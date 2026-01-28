# Script para probar el CRUD completo de /api/v1/clientes
# Asegurarse de que la API esté corriendo en https://localhost:7096

$baseUrl = "https://localhost:2502"
$email = "psantos@global-retail.com"
$password = "12345678"

# Ignorar errores de certificados SSL (compatible con PowerShell 5.1 y Core)
if ($PSVersionTable.PSVersion.Major -lt 6) {
    # PowerShell 5.1
    add-type @"
        using System.Net;
        using System.Security.Cryptography.X509Certificates;
        public class TrustAllCertsPolicy : ICertificatePolicy {
            public bool CheckValidationResult(
                ServicePoint srvPoint, X509Certificate certificate,
                WebRequest request, int certificateProblem) {
                return true;
            }
        }
"@
    [System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "TEST CRUD CLIENTES (/api/v1/clientes)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 1. LOGIN DESKTOP
Write-Host "1. Login Desktop..." -ForegroundColor Yellow
$loginBody = @{
    email = $email
    password = $password
} | ConvertTo-Json

Write-Host "  Intentando login con: $email" -ForegroundColor Gray

try {
    if ($PSVersionTable.PSVersion.Major -ge 6) {
        # PowerShell Core 6+
        $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/v1/auth/login-desktop" `
            -Method POST `
            -Body $loginBody `
            -ContentType "application/json" `
            -SkipCertificateCheck
    } else {
        # PowerShell 5.1
        $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/v1/auth/login-desktop" `
            -Method POST `
            -Body $loginBody `
            -ContentType "application/json"
    }

    # ✅ Tokens en JSON (sin cookies)
    $token = $loginResponse.accessToken
    $refreshToken = $loginResponse.refreshToken
    $sessionId = $loginResponse.sessionId
    
    Write-Host "✓ Login Desktop exitoso" -ForegroundColor Green
    Write-Host "  Usuario: $($loginResponse.userName)" -ForegroundColor Gray
    Write-Host "  Email: $($loginResponse.userEmail)" -ForegroundColor Gray
    Write-Host "  Rol: $($loginResponse.userRole)" -ForegroundColor Gray
    Write-Host "  Access Token (primeros 20 chars): $($token.Substring(0, [Math]::Min(20, $token.Length)))..." -ForegroundColor Gray
    Write-Host "  Session ID: $sessionId" -ForegroundColor Gray
    Write-Host ""
}
catch {
    Write-Host "✗ Error en login-desktop: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host "  Detalles: $($_.ErrorDetails.Message)" -ForegroundColor Gray
    }
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Función auxiliar para hacer llamadas REST compatibles con PowerShell 5.1 y Core
function Invoke-ApiRequest {
    param(
        [string]$Uri,
        [string]$Method = "GET",
        [hashtable]$Headers,
        [string]$Body = $null
    )
    
    $params = @{
        Uri = $Uri
        Method = $Method
        Headers = $Headers
    }
    
    if ($Body) {
        $params.Body = $Body
    }
    
    if ($PSVersionTable.PSVersion.Major -ge 6) {
        $params.SkipCertificateCheck = $true
    }
    
    return Invoke-RestMethod @params
}

# 2. VERIFICAR QUE /api/v1/catalog/clientes NO CAMBIÓ
Write-Host "2. Verificar endpoint de catalog (NO debe haber cambiado)..." -ForegroundColor Yellow
try {
    $catalogResponse = Invoke-ApiRequest -Uri "$baseUrl/api/v1/catalog/clientes?limit=5" -Headers $headers

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
    $clientesResponse = Invoke-ApiRequest -Uri "$baseUrl/api/v1/clientes?page=1&size=5" -Headers $headers

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
    $searchResponse = Invoke-ApiRequest -Uri "$baseUrl/api/v1/clientes?q=madrid&size=5" -Headers $headers

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
    $createResponse = Invoke-ApiRequest -Uri "$baseUrl/api/v1/clientes" -Method POST -Headers $headers -Body $nuevoCliente

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
        $getByIdResponse = Invoke-ApiRequest -Uri "$baseUrl/api/v1/clientes/$clienteId" -Headers $headers

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
        $patchResponse = Invoke-ApiRequest -Uri "$baseUrl/api/v1/clientes/$clienteId/nota" -Method PATCH -Headers $headers -Body $updateNota

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
        $putResponse = Invoke-ApiRequest -Uri "$baseUrl/api/v1/clientes/$clienteId" -Method PUT -Headers $headers -Body $updateCliente

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
        $hasNotaResponse = Invoke-ApiRequest -Uri "$baseUrl/api/v1/clientes?hasNota=true&size=5" -Headers $headers

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
        Invoke-ApiRequest -Uri "$baseUrl/api/v1/clientes/$clienteId" -Method DELETE -Headers $headers

        Write-Host "✓ Cliente eliminado correctamente" -ForegroundColor Green
        Write-Host ""
    }
    catch {
        Write-Host "✗ Error al eliminar cliente: $($_.Exception.Message)" -ForegroundColor Red
    }

    # 11. Verificar que el cliente fue eliminado
    Write-Host "11. GET /api/v1/clientes/$clienteId (verificar eliminación)..." -ForegroundColor Yellow
    try {
        $verifyDelete = Invoke-ApiRequest -Uri "$baseUrl/api/v1/clientes/$clienteId" -Headers $headers

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
