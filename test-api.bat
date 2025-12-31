cd C:\GestionTime\GestionTimeApi

Write-Host "`n=== DIAGNÓSTICO DE LOGOS ===" -ForegroundColor Cyan

# 1. Listar archivos en wwwroot-pss_dvnx/images
Write-Host "`n?? Archivos en wwwroot-pss_dvnx/images:" -ForegroundColor Yellow
Get-ChildItem wwwroot-pss_dvnx\images -File | Select-Object Name, Length, LastWriteTime | Format-Table

# 2. Logo configurado en clients.config.json
Write-Host "`n???  Logo en clients.config.json:" -ForegroundColor Yellow
$config = Get-Content clients.config.json | ConvertFrom-Json
$pssClient = $config.Clients | Where-Object { $_.Id -eq "pss_dvnx" }
Write-Host "   Logo configurado: $($pssClient.Logo)" -ForegroundColor White

# 3. Verificar si pss_dvnx_logo.png es válido
Write-Host "`n?? Validando pss_dvnx_logo.png:" -ForegroundColor Yellow
$logoPath = "wwwroot-pss_dvnx\images\pss_dvnx_logo.png"
if (Test-Path $logoPath) {
    $file = Get-Item $logoPath
    Write-Host "   ? Existe: $($file.Length) bytes" -ForegroundColor Green
    
    # Intentar leer los primeros bytes (debe ser PNG)
    $bytes = [System.IO.File]::ReadAllBytes($logoPath) | Select-Object -First 8
    $pngHeader = @(137, 80, 78, 71, 13, 10, 26, 10)
    $isPng = ($bytes[0..7] -join ",") -eq ($pngHeader -join ",")
    
    if ($isPng) {
        Write-Host "   ? Formato PNG válido" -ForegroundColor Green
    } else {
        Write-Host "   ? NO es PNG válido (corrupto)" -ForegroundColor Red
    }
} else {
    Write-Host "   ? NO EXISTE" -ForegroundColor Red
}

Write-Host "`n=== FIN DIAGNÓSTICO ===`n" -ForegroundColor Cyan