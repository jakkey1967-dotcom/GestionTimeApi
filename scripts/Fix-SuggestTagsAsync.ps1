# Script para modificar SuggestTagsAsync para buscar en partes locales

$file = "C:\GestionTime\GestionTimeApi\GestionTime.Infrastructure\Services\Freshdesk\FreshdeskService.cs"

# Leer contenido
$content = Get-Content $file -Raw

# Buscar el método actual
$oldMethod = @'
    public async Task<List<string>> SuggestTagsAsync(string? term, int limit, CancellationToken ct = default)
    {
        var query = _db.FreshdeskTags.AsQueryable();

        if (!string.IsNullOrWhiteSpace(term))
        {
            query = query.Where(t => EF.Functions.ILike(t.Name, $"{term}%"));
        }

        return await query
            .OrderByDescending(t => t.LastSeenAt)
            .Take(limit)
            .Select(t => t.Name)
            .ToListAsync(ct);
    }
'@

# Nuevo método que busca en partes locales
$newMethod = @'
    public async Task<List<string>> SuggestTagsAsync(string? term, int limit, CancellationToken ct = default)
    {
        // 🆕 MODIFICADO: Buscar tags en partes locales (columna tags es text[] en PostgreSQL)
        var allTags = await _db.PartesDeTrabajo
            .Where(p => p.Tags != null && p.Tags.Length > 0)
            .SelectMany(p => p.Tags!)
            .ToListAsync(ct);
        
        // Agrupar por tag y contar frecuencia
        var tagFrequency = allTags
            .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
            .Select(g => new { Tag = g.Key, Count = g.Count() })
            .ToList();
        
        // Filtrar por término si se proporciona
        if (!string.IsNullOrWhiteSpace(term))
        {
            tagFrequency = tagFrequency
                .Where(t => t.Tag.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        
        // Ordenar por frecuencia descendente y tomar top N
        return tagFrequency
            .OrderByDescending(t => t.Count)
            .Take(limit)
            .Select(t => t.Tag)
            .ToList();
    }
'@

if ($content -like "*$oldMethod*")
{
    Write-Host "✅ Método SuggestTagsAsync encontrado" -ForegroundColor Green
    
    # Backup
    $backupFile = "$file.backup.$(Get-Date -Format 'yyyyMMdd_HHmmss')"
    Copy-Item $file $backupFile
    Write-Host "📦 Backup guardado: $backupFile" -ForegroundColor Cyan
    
    # Reemplazar
    $newContent = $content.Replace($oldMethod, $newMethod)
    $newContent | Set-Content $file -NoNewline
    
    Write-Host "✅ Método actualizado para buscar en partes locales" -ForegroundColor Green
    Write-Host "   Ahora busca en: PartesDeTrabajo.Tags (text[])" -ForegroundColor White
    Write-Host "   Ordena por: Frecuencia de uso (count DESC)" -ForegroundColor White
}
else
{
    Write-Host "❌ No se encontró el método exacto" -ForegroundColor Red
    Write-Host "El método podría tener un formato diferente" -ForegroundColor Yellow
}
