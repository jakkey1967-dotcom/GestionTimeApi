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

    /// <summary>Obtiene la lista completa de tags disponibles ordenados por uso reciente.</summary>
    [HttpGet]
    public async Task<ActionResult<List<string>>> GetTags(
        [FromQuery] string? source = null,
        [FromQuery] int? limit = null)
    {
        try
        {
            // Query directa sin filtros para debug
            var allTags = await _db.FreshdeskTags
                .OrderByDescending(t => t.LastSeenAt)
                .ToListAsync();

            _logger.LogInformation("Total tags en BD: {Total}", allTags.Count);

            // Aplicar filtros en memoria para debug
            var filtered = allTags.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(source))
            {
                filtered = filtered.Where(t => t.Name.StartsWith(source, StringComparison.OrdinalIgnoreCase));
                _logger.LogInformation("Después de filtrar por Name que empieza con '{Source}': {Count}", source, filtered.Count());
            }

            if (limit.HasValue && limit.Value > 0)
            {
                filtered = filtered.Take(limit.Value);
            }

            var tags = filtered.Select(t => t.Name).ToList();

            _logger.LogInformation("Tags retornados: {Count} (source={Source}, limit={Limit})", 
                tags.Count, source ?? "todos", limit?.ToString() ?? "sin límite");

            return Ok(tags);
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P01")
        {
            _logger.LogWarning("Tabla pss_dvnx.freshdesk_tags no existe. Error: {Message}", ex.Message);
            return Ok(new List<string>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener lista de tags. Error: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error al obtener tags", detalle = ex.Message });
        }
    }

    /// <summary>Obtiene estadísticas de uso de tags.</summary>
    [HttpGet("stats")]
    public async Task<ActionResult<object>> GetTagsStats()
    {
        try
        {
            var stats = await _db.FreshdeskTags
                .AsNoTracking()
                .GroupBy(t => t.Source)
                .Select(g => new
                {
                    source = g.Key,
                    count = g.Count()
                })
                .ToListAsync();

            var totalTags = await _db.FreshdeskTags
                .AsNoTracking()
                .CountAsync();

            var totalPartesTags = await _db.ParteTags
                .AsNoTracking()
                .Select(pt => pt.ParteId)
                .Distinct()
                .CountAsync();

            _logger.LogInformation("Estadísticas de tags: Total={Total}, Partes={Partes}, Fuentes={Fuentes}", 
                totalTags, totalPartesTags, stats.Count);

            return Ok(new
            {
                total_tags = totalTags,
                partes_con_tags = totalPartesTags,
                por_fuente = stats
            });
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P01")
        {
            _logger.LogWarning("Tablas de tags no existen en pss_dvnx. Devolviendo stats vacías. Error: {Message}", ex.Message);
            return Ok(new
            {
                total_tags = 0,
                partes_con_tags = 0,
                por_fuente = new List<object>()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener estadísticas de tags desde pss_dvnx");
            return StatusCode(500, new { error = "Error al obtener estadísticas", detalle = ex.Message });
        }
    }
}
