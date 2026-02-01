# Script para corregir SuggestTagsAsync usando la relación ParteTag

$file = "C:\GestionTime\GestionTimeApi\GestionTime.Infrastructure\Services\Freshdesk\FreshdeskService.cs"

# Leer contenido
$content = Get-Content $file -Raw

# Método actual (incorrecto)
$oldMethod = @'
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

# Nuevo método usando ParteTag
$newMethod = @'
    public async Task<List<string>> SuggestTagsAsync(string? term, int limit, CancellationToken ct = default)
    {
        // 🆕 MODIFICADO: Buscar tags en ParteTags (many-to-many con PartesDeTrabajo)
        // Extraer todos los tags desde la relación ParteTag
        var query = _db.PartesDeTrabajo
            .Where(p => p.ParteTags.Any())
            .SelectMany(p => p.ParteTags.Select(pt => pt.Nombre))
            .AsQueryable();
        
        // Agrupar por nombre y contar frecuencia
        var tagFrequencyQuery = query
            .GroupBy(tagName => tagName.ToLower())
            .Select(g => new { TagName = g.Key, Count = g.Count() });
        
        // Filtrar por término si se proporciona
        if (!string.IsNullOrWhiteSpace(term))
        {
            var termLower = term.ToLower();
            tagFrequencyQuery = tagFrequencyQuery.Where(t => t.TagName.Contains(termLower));
        }
        
        // Ejecutar query, ordenar por frecuencia y tomar top N
        var results = await tagFrequencyQuery
            .OrderByDescending(t => t.Count)
            .Take(limit)
            .ToListAsync(ct);
        
        // Capitalizar primera letra para consistencia visual
        return results
            .Select(t => CapitalizeFirst(t.TagName))
            .ToList();
    }
    
    /// <summary>Capitaliza la primera letra de un string.</summary>
    private static string CapitalizeFirst(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        if (text.Length == 1) return text.ToUpper();
        return char.ToUpper(text[0]) + text.Substring(1);
    }
'@

if ($content -like "*$oldMethod*")
{
    Write-Host "✅ Método incorrecto encontrado" -ForegroundColor Green
    
    # Reemplazar
    $newContent = $content.Replace($oldMethod, $newMethod)
    $newContent | Set-Content $file -NoNewline
    
    Write-Host "✅ Método corregido para usar ParteTag" -ForegroundColor Green
    Write-Host "   Ahora busca en: PartesDeTrabajo.ParteTags.Nombre" -ForegroundColor White
    Write-Host "   Ordena por: Frecuencia de uso (count DESC)" -ForegroundColor White
}
else
{
    Write-Host "⚠️ No se encontró el método incorrecto" -ForegroundColor Yellow
    Write-Host "Verificando si ya fue corregido..." -ForegroundColor Cyan
}
