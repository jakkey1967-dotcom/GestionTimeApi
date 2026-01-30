# ===============================================
# Fix JWT Authentication - Soportar Header y Cookie
# ===============================================
# 
# PROBLEMA:
# El backend solo acepta JWT desde cookie "access_token"
# NO acepta el header Authorization: Bearer {token}
#
# SOLUCIÓN:
# Modificar OnMessageReceived para aceptar AMBOS métodos
# ===============================================

Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🔧 FIX JWT AUTHENTICATION - Backend" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

$programCs = "C:\GestionTime\GestionTimeApi\Program.cs"

if (-not (Test-Path $programCs)) {
    Write-Host "❌ No se encontró Program.cs en: $programCs" -ForegroundColor Red
    exit 1
}

Write-Host "📄 Leyendo Program.cs..." -ForegroundColor Yellow
$content = Get-Content $programCs -Raw

# Backup del archivo original
$backupPath = "$programCs.backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
Copy-Item $programCs $backupPath
Write-Host "💾 Backup creado: $backupPath" -ForegroundColor Green
Write-Host ""

# Buscar el bloque de OnMessageReceived
$oldPattern = @'
            opt.Events = new JwtBearerEvents
            {
                OnMessageReceived = ctx =>
                {
                    if (ctx.Request.Cookies.TryGetValue("access_token", out var token))
                        ctx.Token = token;

                    return Task.CompletedTask;
                }
            };
'@

$newPattern = @'
            opt.Events = new JwtBearerEvents
            {
                OnMessageReceived = ctx =>
                {
                    // ✅ PRIORIDAD 1: Leer desde header Authorization: Bearer {token}
                    // Esto es lo que envía el Desktop y aplicaciones móviles
                    var authHeader = ctx.Request.Headers["Authorization"].ToString();
                    if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        ctx.Token = authHeader.Substring("Bearer ".Length).Trim();
                        return Task.CompletedTask;
                    }

                    // ✅ PRIORIDAD 2: Leer desde cookie "access_token" (para navegadores web)
                    if (ctx.Request.Cookies.TryGetValue("access_token", out var cookieToken))
                    {
                        ctx.Token = cookieToken;
                        return Task.CompletedTask;
                    }

                    // ⚠️ No se encontró token en ningún lugar
                    return Task.CompletedTask;
                }
            };
'@

if ($content -match [regex]::Escape($oldPattern))
{
    Write-Host "✅ Patrón antiguo encontrado - Aplicando FIX..." -ForegroundColor Green
    $content = $content -replace [regex]::Escape($oldPattern), $newPattern
    
    Set-Content $programCs -Value $content -NoNewline
    
    Write-Host "✅ Program.cs actualizado correctamente" -ForegroundColor Green
    Write-Host ""
    Write-Host "📝 CAMBIOS APLICADOS:" -ForegroundColor Cyan
    Write-Host "   • Ahora acepta token desde Authorization header (Desktop/Mobile)" -ForegroundColor White
    Write-Host "   • Sigue aceptando token desde cookie (Web)" -ForegroundColor White
    Write-Host "   • Prioridad: Header > Cookie" -ForegroundColor White
    Write-Host ""
    Write-Host "⚠️  IMPORTANTE: Reinicia el backend para aplicar los cambios:" -ForegroundColor Yellow
    Write-Host "   cd C:\GestionTime\GestionTimeApi" -ForegroundColor White
    Write-Host "   dotnet run" -ForegroundColor White
}
else
{
    Write-Host "⚠️  El patrón no coincide exactamente" -ForegroundColor Yellow
    Write-Host "   Buscando línea OnMessageReceived..." -ForegroundColor Gray
    
    $lines = $content -split "`n"
    $lineNumber = 1
    foreach ($line in $lines)
    {
        if ($line -match 'OnMessageReceived')
        {
            Write-Host "   Encontrado en línea: $lineNumber" -ForegroundColor Cyan
            Write-Host ""
            Write-Host "   Contexto (5 líneas antes y después):" -ForegroundColor Gray
            for ($i = [Math]::Max(0, $lineNumber - 6); $i -lt [Math]::Min($lines.Count, $lineNumber + 5); $i++)
            {
                $prefix = if ($i -eq ($lineNumber - 1)) { ">>>" } else { "   " }
                Write-Host "$prefix $($i + 1): $($lines[$i])" -ForegroundColor Gray
            }
            break
        }
        $lineNumber++
    }
    
    Write-Host ""
    Write-Host "💡 SOLUCIÓN MANUAL:" -ForegroundColor Yellow
    Write-Host "   1. Abre: C:\GestionTime\GestionTimeApi\Program.cs" -ForegroundColor White
    Write-Host "   2. Busca: 'OnMessageReceived = ctx =>'" -ForegroundColor White
    Write-Host "   3. Reemplaza el bloque completo con:" -ForegroundColor White
    Write-Host ""
    Write-Host $newPattern -ForegroundColor Gray
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
