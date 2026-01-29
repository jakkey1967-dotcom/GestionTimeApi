# Script que ejecuta tests de Tipos/Grupos y captura logs detallados
param(
    [string]$LogFile = "logs/tipos-grupos-test-$(Get-Date -Format 'yyyy-MM-dd_HH-mm-ss').log"
)

Write-Host "╔══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "║ TEST TIPOS Y GRUPOS - CON CAPTURA DE LOGS" -ForegroundColor Cyan
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
Write-Log "║ INICIANDO TESTS" -Color Cyan
Write-Log "╚══════════════════════════════════════════════════════════════" -Color Cyan

# 1. LOGIN
Write-Log "`n🔐 [1/10] Login Desktop..." -Color Cyan
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

# 2. LISTAR TIPOS
Write-Log "`n📋 [2/10] GET /tipos - Listar todos..." -Color Cyan
try {
    $tipos = Invoke-RestMethod -Uri "$baseUrl/tipos" -Method GET -Headers $headers
    Write-ApiLog -Method "GET" -Url "/tipos" -Response $tipos -StatusCode 200
    Write-Log "✅ $($tipos.Count) tipos encontrados" -Color Green
} catch {
    Write-ApiLog -Method "GET" -Url "/tipos" -Error $_.Exception.Message
}

# 3. CREAR TIPO
Write-Log "`n📝 [3/10] POST /tipos - Crear nuevo..." -Color Cyan
$newTipo = @{
    Nombre = "Test Tipo $(Get-Date -Format 'HHmmss')"
    Descripcion = "Tipo de prueba"
}

try {
    $createdTipo = Invoke-RestMethod -Uri "$baseUrl/tipos" -Method POST -Headers $headers -Body ($newTipo | ConvertTo-Json) -ContentType "application/json"
    Write-ApiLog -Method "POST" -Url "/tipos" -Body $newTipo -Response $createdTipo -StatusCode 201
    Write-Log "✅ Tipo creado: ID=$($createdTipo.id)" -Color Green
    $tipoId = $createdTipo.id
} catch {
    Write-ApiLog -Method "POST" -Url "/tipos" -Body $newTipo -Error $_.Exception.Message
    $tipoId = 0
}

# 4. OBTENER TIPO POR ID
if ($tipoId -gt 0) {
    Write-Log "`n🔍 [4/10] GET /tipos/$tipoId - Obtener por ID..." -Color Cyan
    try {
        $tipo = Invoke-RestMethod -Uri "$baseUrl/tipos/$tipoId" -Method GET -Headers $headers
        Write-ApiLog -Method "GET" -Url "/tipos/$tipoId" -Response $tipo -StatusCode 200
        Write-Log "✅ Tipo obtenido correctamente" -Color Green
    } catch {
        Write-ApiLog -Method "GET" -Url "/tipos/$tipoId" -Error $_.Exception.Message
    }
}

# 5. ACTUALIZAR TIPO
if ($tipoId -gt 0) {
    Write-Log "`n✏️  [5/10] PUT /tipos/$tipoId - Actualizar..." -Color Cyan
    $updateTipo = @{
        Nombre = "Test Tipo Actualizado $(Get-Date -Format 'HHmmss')"
        Descripcion = "Tipo actualizado"
    }
    
    try {
        $updatedTipo = Invoke-RestMethod -Uri "$baseUrl/tipos/$tipoId" -Method PUT -Headers $headers -Body ($updateTipo | ConvertTo-Json) -ContentType "application/json"
        Write-ApiLog -Method "PUT" -Url "/tipos/$tipoId" -Body $updateTipo -Response $updatedTipo -StatusCode 200
        Write-Log "✅ Tipo actualizado correctamente" -Color Green
    } catch {
        Write-ApiLog -Method "PUT" -Url "/tipos/$tipoId" -Body $updateTipo -Error $_.Exception.Message
    }
}

# 6. ELIMINAR TIPO
if ($tipoId -gt 0) {
    Write-Log "`n🗑️  [6/10] DELETE /tipos/$tipoId - Eliminar..." -Color Cyan
    try {
        Invoke-RestMethod -Uri "$baseUrl/tipos/$tipoId" -Method DELETE -Headers $headers
        Write-ApiLog -Method "DELETE" -Url "/tipos/$tipoId" -StatusCode 204
        Write-Log "✅ Tipo eliminado correctamente" -Color Green
    } catch {
        Write-ApiLog -Method "DELETE" -Url "/tipos/$tipoId" -Error $_.Exception.Message
    }
}

# 7. LISTAR GRUPOS
Write-Log "`n📋 [7/10] GET /grupos - Listar todos..." -Color Cyan
try {
    $grupos = Invoke-RestMethod -Uri "$baseUrl/grupos" -Method GET -Headers $headers
    Write-ApiLog -Method "GET" -Url "/grupos" -Response $grupos -StatusCode 200
    Write-Log "✅ $($grupos.Count) grupos encontrados" -Color Green
} catch {
    Write-ApiLog -Method "GET" -Url "/grupos" -Error $_.Exception.Message
}

# 8. CREAR GRUPO
Write-Log "`n📝 [8/10] POST /grupos - Crear nuevo..." -Color Cyan
$newGrupo = @{
    Nombre = "Test Grupo $(Get-Date -Format 'HHmmss')"
    Descripcion = "Grupo de prueba"
}

try {
    $createdGrupo = Invoke-RestMethod -Uri "$baseUrl/grupos" -Method POST -Headers $headers -Body ($newGrupo | ConvertTo-Json) -ContentType "application/json"
    Write-ApiLog -Method "POST" -Url "/grupos" -Body $newGrupo -Response $createdGrupo -StatusCode 201
    Write-Log "✅ Grupo creado: ID=$($createdGrupo.id)" -Color Green
    $grupoId = $createdGrupo.id
} catch {
    Write-ApiLog -Method "POST" -Url "/grupos" -Body $newGrupo -Error $_.Exception.Message
    $grupoId = 0
}

# 9. ACTUALIZAR GRUPO
if ($grupoId -gt 0) {
    Write-Log "`n✏️  [9/10] PUT /grupos/$grupoId - Actualizar..." -Color Cyan
    $updateGrupo = @{
        Nombre = "Test Grupo Actualizado $(Get-Date -Format 'HHmmss')"
        Descripcion = "Grupo actualizado"
    }
    
    try {
        $updatedGrupo = Invoke-RestMethod -Uri "$baseUrl/grupos/$grupoId" -Method PUT -Headers $headers -Body ($updateGrupo | ConvertTo-Json) -ContentType "application/json"
        Write-ApiLog -Method "PUT" -Url "/grupos/$grupoId" -Body $updateGrupo -Response $updatedGrupo -StatusCode 200
        Write-Log "✅ Grupo actualizado correctamente" -Color Green
    } catch {
        Write-ApiLog -Method "PUT" -Url "/grupos/$grupoId" -Body $updateGrupo -Error $_.Exception.Message
    }
}

# 10. ELIMINAR GRUPO
if ($grupoId -gt 0) {
    Write-Log "`n🗑️  [10/10] DELETE /grupos/$grupoId - Eliminar..." -Color Cyan
    try {
        Invoke-RestMethod -Uri "$baseUrl/grupos/$grupoId" -Method DELETE -Headers $headers
        Write-ApiLog -Method "DELETE" -Url "/grupos/$grupoId" -StatusCode 204
        Write-Log "✅ Grupo eliminado correctamente" -Color Green
    } catch {
        Write-ApiLog -Method "DELETE" -Url "/grupos/$grupoId" -Error $_.Exception.Message
    }
}

Write-Log "`n╔══════════════════════════════════════════════════════════════" -Color Cyan
Write-Log "║ TESTS COMPLETADOS" -Color Cyan
Write-Log "╚══════════════════════════════════════════════════════════════" -Color Cyan

Write-Host "`n📄 Log completo guardado en: $LogFile" -ForegroundColor Green
Write-Host "`n💡 Para ver los logs de la API, revisa:" -ForegroundColor Yellow
Write-Host "   - Consola de Visual Studio (donde corre la API)" -ForegroundColor White
Write-Host "   - logs/log-*.txt (logs de la aplicación)" -ForegroundColor White
Write-Host "`n💡 Para abrir el log del test:" -ForegroundColor Yellow
Write-Host "   notepad $LogFile" -ForegroundColor White
