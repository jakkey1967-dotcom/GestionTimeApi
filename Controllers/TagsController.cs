using GestionTime.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionTime.Api.Controllers;

[ApiController]
[Route("api/v1/tags")]
[Authorize]
public class TagsController : ControllerBase
{
    private readonly GestionTimeDbContext _db;
    private readonly ILogger<TagsController> _logger;

    public TagsController(GestionTimeDbContext db, ILogger<TagsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/v1/tags/suggest?term=&amp;limit=20
    /// Autocompletado de tags (usa tabla freshdesk_tags unificada)
    /// </summary>
    [HttpGet("suggest")]
    public async Task<IActionResult> Suggest(
        [FromQuery] string? term,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        try
        {
            // Validar y limitar
            limit = Math.Min(limit, 50);
            
            _logger.LogInformation("🏷️ TagsSuggest - term: '{Term}', limit: {Limit}", term ?? "ninguno", limit);
            
            var query = _db.FreshdeskTags.AsQueryable();
            
            // Filtrar por prefijo si se proporciona
            if (!string.IsNullOrWhiteSpace(term))
            {
                var normalizedTerm = term.Trim().ToLowerInvariant();
                query = query.Where(t => EF.Functions.ILike(t.Name, $"{normalizedTerm}%"));
            }
            
            // Ordenar por más recientes primero, luego alfabético
            var tags = await query
                .OrderByDescending(t => t.LastSeenAt)
                .ThenBy(t => t.Name)
                .Take(limit)
                .Select(t => t.Name)
                .ToListAsync(ct);
            
            _logger.LogInformation("   ✅ Encontradas {Count} tags", tags.Count);
            
            return Ok(new
            {
                success = true,
                count = tags.Count,
                tags = tags
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al buscar tags");
            return StatusCode(500, new
            {
                success = false,
                message = "Error al buscar tags"
            });
        }
    }
}
