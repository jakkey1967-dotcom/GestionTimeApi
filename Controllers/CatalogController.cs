using GestionTime.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionTime.Api.Controllers;

[ApiController]
[Route("api/v1/catalog")]
[Authorize]
public class CatalogController : ControllerBase
{
    private readonly GestionTimeDbContext db;
    private readonly ILogger<CatalogController> _logger;

    public CatalogController(GestionTimeDbContext db, ILogger<CatalogController> logger)
    {
        this.db = db;
        _logger = logger;
    }

    [HttpGet("clientes")]
    public async Task<IActionResult> Clientes([FromQuery] string? q, [FromQuery] int limit = 20, [FromQuery] int offset = 0)
    {
        limit = Math.Clamp(limit, 1, 100);
        offset = Math.Max(offset, 0);

        _logger.LogDebug("Buscando clientes. Query: {Query}, Limit: {Limit}, Offset: {Offset}", q, limit, offset);

        var query = db.Clientes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(c =>
                (c.NombreComercial != null && EF.Functions.ILike(c.NombreComercial, $"%{q}%")) ||
                (c.Nombre != null && EF.Functions.ILike(c.Nombre, $"%{q}%")));
        }

        var items = await query
            .OrderBy(c => c.NombreComercial ?? c.Nombre)
            .Skip(offset)
            .Take(limit)
            .Select(c => new { id = c.Id, nombre = (c.NombreComercial ?? c.Nombre) })
            .ToListAsync();

        _logger.LogDebug("Encontrados {Count} clientes", items.Count);

        return Ok(items);
    }

    [HttpGet("grupos")]
    public async Task<IActionResult> Grupos()
    {
        _logger.LogDebug("Consultando catálogo de grupos");

        var items = await db.Grupos.AsNoTracking()
            .OrderBy(g => g.Nombre)
            .Select(g => new { id_grupo = g.IdGrupo, nombre = g.Nombre })
            .ToListAsync();

        _logger.LogDebug("Encontrados {Count} grupos", items.Count);

        return Ok(items);
    }

    [HttpGet("tipos")]
    public async Task<IActionResult> Tipos()
    {
        _logger.LogDebug("Consultando catálogo de tipos");

        var items = await db.Tipos.AsNoTracking()
            .OrderBy(t => t.Nombre)
            .Select(t => new { id_tipo = t.IdTipo, nombre = t.Nombre })
            .ToListAsync();

        _logger.LogDebug("Encontrados {Count} tipos", items.Count);

        return Ok(items);
    }
}

