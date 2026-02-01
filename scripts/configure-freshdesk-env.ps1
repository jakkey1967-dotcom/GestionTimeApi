# ==============================================================================
# CONFIGURAR VARIABLES DE ENTORNO PARA FRESHDESK
# ==============================================================================

Write-Host "🔧 Configurando variables de entorno para Freshdesk..." -ForegroundColor Cyan
Write-Host ""

# Solicitar API Key si no está configurada
if ([string]::IsNullOrEmpty($env:Freshdesk__ApiKey) -or $env:Freshdesk__ApiKey -eq "DISABLED") {
    Write-Host "📝 Ingresa tu Freshdesk API Key:" -ForegroundColor Yellow
    Write-Host "   (La puedes obtener en: Admin > API Settings)" -ForegroundColor Gray
    $apiKey = Read-Host "API Key"
    
    if ([string]::IsNullOrWhiteSpace($apiKey)) {
        Write-Host "❌ API Key no puede estar vacía" -ForegroundColor Red
        exit 1
    }
    
    $env:Freshdesk__ApiKey = $apiKey
    Write-Host "✅ API Key configurada" -ForegroundColor Green
} else {
    Write-Host "✅ API Key ya configurada: $($env:Freshdesk__ApiKey.Substring(0, 4))..." -ForegroundColor Green
}

# Configurar Domain si no está
if ([string]::IsNullOrEmpty($env:Freshdesk__Domain)) {
    Write-Host ""
    Write-Host "📝 Ingresa tu Freshdesk Domain:" -ForegroundColor Yellow
    Write-Host "   (Ejemplo: si tu URL es https://tuempresa.freshdesk.com, ingresa 'tuempresa')" -ForegroundColor Gray
    $domain = Read-Host "Domain"
    
    if ([string]::IsNullOrWhiteSpace($domain)) {
        $domain = "alterasoftware"
        Write-Host "⚠️ Usando domain por defecto: $domain" -ForegroundColor Yellow
    }
    
    $env:Freshdesk__Domain = $domain
    Write-Host "✅ Domain configurado: $domain" -ForegroundColor Green
} else {
    Write-Host "✅ Domain ya configurado: $env:Freshdesk__Domain" -ForegroundColor Green
}

# Configurar SyncEnabled
$env:Freshdesk__SyncEnabled = "true"
$env:Freshdesk__SyncIntervalHours = "24"

Write-Host ""
Write-Host "🎉 Variables configuradas exitosamente!" -ForegroundColor Green
Write-Host ""
Write-Host "📋 Variables actuales:" -ForegroundColor Cyan
Write-Host "   Freshdesk__Domain: $env:Freshdesk__Domain" -ForegroundColor Gray
Write-Host "   Freshdesk__ApiKey: $($env:Freshdesk__ApiKey.Substring(0, 4))...$($env:Freshdesk__ApiKey.Substring($env:Freshdesk__ApiKey.Length - 4))" -ForegroundColor Gray
Write-Host "   Freshdesk__SyncEnabled: $env:Freshdesk__SyncEnabled" -ForegroundColor Gray
Write-Host "   Freshdesk__SyncIntervalHours: $env:Freshdesk__SyncIntervalHours" -ForegroundColor Gray
Write-Host ""
Write-Host "ℹ️ Estas variables solo están activas en esta sesión de PowerShell" -ForegroundColor Yellow
Write-Host "   Para hacerlas permanentes, agrégalas a las variables de entorno del sistema" -ForegroundColor Yellow
Write-Host ""
Write-Host "🚀 Ahora puedes ejecutar la API:" -ForegroundColor Cyan
Write-Host "   dotnet run --project GestionTime.Api" -ForegroundColor Gray
