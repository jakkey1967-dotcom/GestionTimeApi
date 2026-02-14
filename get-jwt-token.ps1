#!/usr/bin/env pwsh
# get-jwt-token.ps1
# Obtiene token JWT para usar en Swagger

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                                                                ║" -ForegroundColor Cyan
Write-Host "║  🔑 OBTENER TOKEN JWT PARA SWAGGER                            ║" -ForegroundColor Cyan
Write-Host "║                                                                ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "http://localhost:2501"
$email = "psantos@global-retail.com"
$password = "12345678"

Write-Host "📧 Usuario:  $email" -ForegroundColor Cyan
Write-Host "🌐 API:      $baseUrl" -ForegroundColor Cyan
Write-Host ""
Write-Host "🔄 Obteniendo token..." -ForegroundColor Yellow

try {
    $body = @{
        email = $email
        password = $password
    } | ConvertTo-Json

    $response = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/auth/login-desktop" `
        -Method Post `
        -ContentType "application/json" `
        -Body $body `
        -ErrorAction Stop

    Write-Host ""
    Write-Host "✅ TOKEN OBTENIDO:" -ForegroundColor Green
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
    Write-Host ""
    Write-Host $response.accessToken -ForegroundColor White
    Write-Host ""
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
    Write-Host ""
    
    # Copiar al portapapeles si está disponible
    try {
        Set-Clipboard -Value $response.accessToken
        Write-Host "📋 Token copiado al portapapeles" -ForegroundColor Green
    }
    catch {
        Write-Host "⚠️ No se pudo copiar al portapapeles (copia manualmente)" -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "📝 CÓMO USAR EN SWAGGER:" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  1. Abre Swagger: http://localhost:2501/swagger" -ForegroundColor White
    Write-Host "  2. Haz clic en el botón 🔓 Authorize (esquina superior derecha)" -ForegroundColor White
    Write-Host "  3. Pega el token en el campo 'Value'" -ForegroundColor White
    Write-Host "  4. Haz clic en 'Authorize'" -ForegroundColor White
    Write-Host "  5. Haz clic en 'Close'" -ForegroundColor White
    Write-Host "  6. ✅ Ahora todos los endpoints funcionarán" -ForegroundColor White
    Write-Host ""
    Write-Host "💡 TIP: El token dura 12 horas" -ForegroundColor Yellow
    Write-Host ""
    
    Write-Host "👤 USUARIO AUTENTICADO:" -ForegroundColor Cyan
    Write-Host "   Email:     $($response.user.email)" -ForegroundColor Gray
    Write-Host "   Nombre:    $($response.user.firstName) $($response.user.lastName)" -ForegroundColor Gray
    Write-Host "   Role:      $($response.user.role)" -ForegroundColor Gray
    Write-Host "   Token exp: $(Get-Date).AddHours(12).ToString('yyyy-MM-dd HH:mm')" -ForegroundColor Gray
    Write-Host ""
}
catch {
    Write-Host ""
    Write-Host "❌ ERROR OBTENIENDO TOKEN" -ForegroundColor Red
    Write-Host ""
    
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode.value__
        Write-Host "   Status:  $statusCode" -ForegroundColor Red
        
        if ($statusCode -eq 401) {
            Write-Host "   Motivo:  Credenciales incorrectas" -ForegroundColor Red
            Write-Host ""
            Write-Host "   💡 Verifica que el usuario existe en la BD:" -ForegroundColor Yellow
            Write-Host "      Email:    psantos@global-retail.com" -ForegroundColor Cyan
            Write-Host "      Password: 12345678" -ForegroundColor Cyan
        }
        elseif ($statusCode -eq 404) {
            Write-Host "   Motivo:  Endpoint no encontrado" -ForegroundColor Red
            Write-Host ""
            Write-Host "   💡 ¿La API está corriendo?" -ForegroundColor Yellow
            Write-Host "      Ejecuta: dotnet run --project GestionTime.Api" -ForegroundColor Cyan
        }
    }
    else {
        Write-Host "   Detalle: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host ""
        Write-Host "   💡 ¿La API está corriendo?" -ForegroundColor Yellow
        Write-Host "      Ejecuta: dotnet run --project GestionTime.Api" -ForegroundColor Cyan
    }
    Write-Host ""
    exit 1
}

Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
