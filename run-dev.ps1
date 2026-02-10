# Script para ejecutar la API en desarrollo con UTF-8 forzado

# Configurar UTF-8 en la consola actual
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
[Console]::InputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

# Configurar variables de entorno
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_SYSTEM_CONSOLE_ALLOW_ANSI_COLOR_REDIRECTION = 'true'

Write-Host "╔══════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║      Iniciando GestionTime API (Desarrollo)         ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan

Write-Host "⚙️  Configuración:" -ForegroundColor Yellow
Write-Host "   Encoding: UTF-8 ✅" -ForegroundColor Green
Write-Host "   Environment: Development" -ForegroundColor Green
Write-Host "   Puerto HTTP: 2501" -ForegroundColor Green
Write-Host "   Puerto HTTPS: 2502`n" -ForegroundColor Green

# Ejecutar la API
dotnet run --project GestionTime.Api.csproj
