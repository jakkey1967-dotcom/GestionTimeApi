# Script de test completo para CRUD de Clientes con logging detallado
param(
    [string]$LogFile = "logs/clientes-test-$(Get-Date -Format 'yyyy-MM-dd_HH-mm-ss').log"
)

Write-Host "╔══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "║ TEST CLIENTES CRUD - CON CAPTURA DE LOGS" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════════" -ForegroundColor Cyan

# Crear directorio de logs si no existe
$logDir = Split-Path $LogFile -Parent
if (-not (Test-Path $logDir)) {
    New-Item -ItemType Directory -Path $logDir -Force | Out-Null
}

Write-Host "`n📋 Los logs se guardarán en: $LogFile" -ForegroundColor Yellow
Write-Host "`n🔍 Buscando proceso de la API..." -ForegroundColor Cyan

# Verificar si la API está corriendo
$apiProcess = Get-Process -Name "GestionTime.Api" -ErrorAction SilentlyContinue

if ($null -eq $apiProcess) {
    Write-Host "❌ La API NO está corriendo." -ForegroundColor Red
    Write-Host "   Por favor, inicia la API desde Visual Studio (F5) y vuelve a ejecutar este script." -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ API corriendo (PID: $($apiProcess.Id))" -ForegroundColor Green

# Configuración
$baseUrl = "https://localhost:2502/api/v1"
$EMAIL = "psantos@global-retail.com"
$PASSWORD = "12345678"

# Forzar UTF-8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

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

# Función para loguear
function Write-Log {
    param([string]$Message, [string]$Color = "White")
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss.fff"
    $logMessage = "[$timestamp] $Message"
    
    Write-Host $Message -ForegroundColor $Color
    Add-Content -Path $LogFile -Value $logMessage
}

# Función para loguear requests/responses
function Write-ApiLog {
    param(
        [string]$Method,
        [string]$Url,
        [object]$Body = $null,
        [object]$Response = $null,
        [int]$StatusCode = 0,
        [string]$Error = $null
    )
    
    $separator = "=" * 80
    Write-Log "`n$separator" -Color Gray
    Write-Log "► $Method $Url" -Color Cyan
    Write-Log $separator -Color Gray
    
    if ($Body) {
        Write-Log "REQUEST BODY:" -Color Yellow
        Write-Log ($Body | ConvertTo-Json -Depth 10) -Color White
    }
    
    if ($Response) {
        Write-Log "RESPONSE ($StatusCode):" -Color $(if ($StatusCode -ge 200 -and $StatusCode -lt 300) { "Green" } else { "Red" })
        Write-Log ($Response | ConvertTo-Json -Depth 10) -Color White
    }
    
    if ($Error) {
        Write-Log "ERROR:" -Color Red
        Write-Log $Error -Color Red
    }
    
    Write-Log $separator -Color Gray
}

Write-Log "`n╔══════════════════════════════════════════════════════════════" -Color Cyan
Write-Log "║ INICIANDO TESTS DE CLIENTES" -Color Cyan
Write-Log "╚══════════════════════════════════════════════════════════════" -Color Cyan

# 1. LOGIN
Write-Log "`n🔐 [1/8] Login Desktop..." -Color Cyan
$loginBody = @{
    Email = $EMAIL
    Password = $PASSWORD
}

try {
    $loginResponse = Invoke-RestMethod `
        -Uri "$baseUrl/auth/login-desktop" `
        -Method POST `
        -ContentType "application/json" `
        -Body ($loginBody | ConvertTo-Json)
    
    $accessToken = $loginResponse.accessToken
    $headers = @{ "Authorization" = "Bearer $accessToken" }
    
    Write-ApiLog -Method "POST" -Url "/auth/login-desktop" -Body $loginBody -Response $loginResponse -StatusCode 200
    Write-Log "✅ Login exitoso" -Color Green
} catch {
    Write-ApiLog -Method "POST" -Url "/auth/login-desktop" -Body $loginBody -Error $_.Exception.Message
    Write-Log "❌ Login falló" -Color Red
    exit 1
}

# 2. LISTAR CLIENTES (primera página)
Write-Log "`n📋 [2/8] GET /clientes?page=1&pageSize=10 - Listar primera página..." -Color Cyan
try {
    $clientes = Invoke-RestMethod -Uri "$baseUrl/clientes?page=1&pageSize=10" -Method GET -Headers $headers
    Write-ApiLog -Method "GET" -Url "/clientes?page=1&pageSize=10" -Response $clientes -StatusCode 200
    Write-Log "✅ Página 1: $($clientes.totalItems) clientes totales, mostrando $($clientes.items.Count)" -Color Green
} catch {
    Write-ApiLog -Method "GET" -Url "/clientes?page=1&pageSize=10" -Error $_.Exception.Message
}

# 3. BUSCAR CLIENTES por nombre
Write-Log "`n🔍 [3/8] GET /clientes?search=test&page=1&pageSize=5 - Buscar clientes..." -Color Cyan
try {
    $busqueda = Invoke-RestMethod -Uri "$baseUrl/clientes?search=test&page=1&pageSize=5" -Method GET -Headers $headers
    Write-ApiLog -Method "GET" -Url "/clientes?search=test" -Response $busqueda -StatusCode 200
    Write-Log "✅ Búsqueda: $($busqueda.totalItems) resultados encontrados" -Color Green
} catch {
    Write-ApiLog -Method "GET" -Url "/clientes?search=test" -Error $_.Exception.Message
}

# 4. CREAR CLIENTE
Write-Log "`n📝 [4/8] POST /clientes - Crear nuevo cliente..." -Color Cyan
$newCliente = @{
    Nombre = "Cliente Test $(Get-Date -Format 'HHmmss')"
    IdPuntoop = 9999
    LocalNum = 1
    NombreComercial = "Test Comercial"
    Provincia = "Test Province"
    Nota = "Cliente creado desde test automatizado"
}

try {
    $createdCliente = Invoke-RestMethod -Uri "$baseUrl/clientes" -Method POST -Headers $headers -Body ($newCliente | ConvertTo-Json) -ContentType "application/json"
    Write-ApiLog -Method "POST" -Url "/clientes" -Body $newCliente -Response $createdCliente -StatusCode 201
    Write-Log "✅ Cliente creado: ID=$($createdCliente.id)" -Color Green
    $clienteId = $createdCliente.id
} catch {
    Write-ApiLog -Method "POST" -Url "/clientes" -Body $newCliente -Error $_.Exception.Message
    $clienteId = 0
}

# 5. OBTENER CLIENTE POR ID
if ($clienteId -gt 0) {
    Write-Log "`n🔍 [5/8] GET /clientes/$clienteId - Obtener cliente por ID..." -Color Cyan
    try {
        $cliente = Invoke-RestMethod -Uri "$baseUrl/clientes/$clienteId" -Method GET -Headers $headers
        Write-ApiLog -Method "GET" -Url "/clientes/$clienteId" -Response $cliente -StatusCode 200
        Write-Log "✅ Cliente obtenido: $($cliente.nombre)" -Color Green
    } catch {
        Write-ApiLog -Method "GET" -Url "/clientes/$clienteId" -Error $_.Exception.Message
    }
}

# 6. ACTUALIZAR CLIENTE
if ($clienteId -gt 0) {
    Write-Log "`n✏️  [6/8] PUT /clientes/$clienteId - Actualizar cliente..." -Color Cyan
    $updateCliente = @{
        Nombre = "Cliente Test Actualizado $(Get-Date -Format 'HHmmss')"
        IdPuntoop = 9999
        LocalNum = 2
        NombreComercial = "Test Comercial Updated"
        Provincia = "Updated Province"
        Nota = "Cliente actualizado desde test"
    }
    
    try {
        $updatedCliente = Invoke-RestMethod -Uri "$baseUrl/clientes/$clienteId" -Method PUT -Headers $headers -Body ($updateCliente | ConvertTo-Json) -ContentType "application/json"
        Write-ApiLog -Method "PUT" -Url "/clientes/$clienteId" -Body $updateCliente -Response $updatedCliente -StatusCode 200
        Write-Log "✅ Cliente actualizado: $($updatedCliente.nombre)" -Color Green
    } catch {
        Write-ApiLog -Method "PUT" -Url "/clientes/$clienteId" -Body $updateCliente -Error $_.Exception.Message
    }
}

# 7. ACTUALIZAR SOLO NOTA
if ($clienteId -gt 0) {
    Write-Log "`n📝 [7/8] PATCH /clientes/$clienteId/nota - Actualizar solo nota..." -Color Cyan
    $updateNota = @{
        Nota = "Nota actualizada parcialmente $(Get-Date -Format 'HH:mm:ss')"
    }
    
    try {
        $clienteConNota = Invoke-RestMethod -Uri "$baseUrl/clientes/$clienteId/nota" -Method PATCH -Headers $headers -Body ($updateNota | ConvertTo-Json) -ContentType "application/json"
        Write-ApiLog -Method "PATCH" -Url "/clientes/$clienteId/nota" -Body $updateNota -Response $clienteConNota -StatusCode 200
        Write-Log "✅ Nota actualizada: $($clienteConNota.nota)" -Color Green
    } catch {
        Write-ApiLog -Method "PATCH" -Url "/clientes/$clienteId/nota" -Body $updateNota -Error $_.Exception.Message
    }
}

# 8. ELIMINAR CLIENTE
if ($clienteId -gt 0) {
    Write-Log "`n🗑️  [8/8] DELETE /clientes/$clienteId - Eliminar cliente..." -Color Cyan
    try {
        Invoke-RestMethod -Uri "$baseUrl/clientes/$clienteId" -Method DELETE -Headers $headers
        Write-ApiLog -Method "DELETE" -Url "/clientes/$clienteId" -StatusCode 204
        Write-Log "✅ Cliente eliminado correctamente" -Color Green
    } catch {
        Write-ApiLog -Method "DELETE" -Url "/clientes/$clienteId" -Error $_.Exception.Message
    }
}

Write-Log "`n╔══════════════════════════════════════════════════════════════" -Color Cyan
Write-Log "║ TESTS COMPLETADOS" -Color Cyan
Write-Log "╚══════════════════════════════════════════════════════════════" -Color Cyan

Write-Host "`n📄 Log completo guardado en: $LogFile" -ForegroundColor Green
Write-Host "`n💡 Para abrir el log del test:" -ForegroundColor Yellow
Write-Host "   notepad $LogFile" -ForegroundColor White
