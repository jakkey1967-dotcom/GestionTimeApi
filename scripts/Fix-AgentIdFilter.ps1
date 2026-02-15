# Fix-AgentIdFilter.ps1
# Corrige el filtro de agentId en InformesService.cs

$file = "C:\GestionTime\GestionTimeApi\Services\InformesService.cs"

Write-Host "🔧 Aplicando fix en InformesService.cs..." -ForegroundColor Cyan

$content = Get-Content $file -Raw

$oldPattern = @"
        // Filtro por agente (con control de rol)
        var agentIds = ResolveAgentIds(query.AgentId, query.AgentIds, currentUserId, userRole);
        if (agentIds.Any())
        {
            baseQuery = baseQuery.Where(p => agentIds.Contains(p.IdUsuario));
        }
"@

$newPattern = @"
        // Filtro por agente (con control de rol)
        var agentIds = ResolveAgentIds(query.AgentId, query.AgentIds, currentUserId, userRole);

        // Si es EDITOR/ADMIN y no especificó agentId, usar currentUserId por defecto
        if (!agentIds.Any() && (userRole == "EDITOR" || userRole == "ADMIN"))
        {
            agentIds.Add(currentUserId);
        }

        // SIEMPRE aplicar filtro de agente (ahora nunca estará vacío)
        baseQuery = baseQuery.Where(p => agentIds.Contains(p.IdUsuario));
"@

# Contar ocurrencias
$matches = [regex]::Matches($content, [regex]::Escape($oldPattern))
Write-Host "📊 Encontradas $($matches.Count) ocurrencias del patrón" -ForegroundColor Yellow

if ($matches.Count -eq 0) {
    Write-Host "❌ No se encontró el patrón exacto" -ForegroundColor Red
    Write-Host "Mostrando líneas con 'var agentIds = ResolveAgentIds':" -ForegroundColor Yellow
    Get-Content $file | Select-String "var agentIds = ResolveAgentIds" -Context 2,5
    exit 1
}

# Reemplazar TODAS las ocurrencias
$newContent = $content -replace [regex]::Escape($oldPattern), $newPattern

# Guardar
Set-Content $file $newContent -NoNewline -Encoding UTF8

Write-Host "✅ Fix aplicado exitosamente en $($matches.Count) ubicaciones" -ForegroundColor Green
Write-Host ""
Write-Host "📝 Cambios realizados:" -ForegroundColor Cyan
Write-Host "  - GetResumenAsync: Filtro de agentId ahora usa currentUserId por defecto para EDITOR/ADMIN" -ForegroundColor White
Write-Host "  - GetPartesAsync: Filtro de agentId ahora usa currentUserId por defecto para EDITOR/ADMIN" -ForegroundColor White
Write-Host ""
Write-Host "⚠️  IMPORTANTE: Reinicia el backend para aplicar los cambios" -ForegroundColor Yellow
