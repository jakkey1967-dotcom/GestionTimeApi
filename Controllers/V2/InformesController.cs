using GestionTime.Api.Contracts.Informes;
using GestionTime.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GestionTime.Api.Controllers.V2;

/// <summary>Controlador de informes v2 (solo lectura) con vistas SQL optimizadas.</summary>
[ApiController]
[Route("api/v2/informes")]
[Authorize]
[ApiExplorerSettings(GroupName = "v2")]
public class InformesController : ControllerBase
{
    private readonly InformesService _informesService;
    private readonly ILogger<InformesController> _logger;

    public InformesController(InformesService informesService, ILogger<InformesController> logger)
    {
        _informesService = informesService;
        _logger = logger;
    }

    /// <summary>Obtiene partes de trabajo con paginación, filtros y ordenamiento.</summary>
    /// <remarks>
    /// Requiere especificar OBLIGATORIAMENTE uno de: date (YYYY-MM-DD), weekIso (YYYY-Www), o from+to (YYYY-MM-DD).
    /// 
    /// **Seguridad por roles:**
    /// - USER: Solo puede ver sus propios datos (id_usuario del JWT). Si intenta usar agentId/agentIds distinto devuelve 403.
    /// - EDITOR/ADMIN: Pueden ver cualquier agente usando agentId o agentIds (separados por comas).
    /// 
    /// **Filtros opcionales:**
    /// - q: búsqueda de texto libre en ticket, accion, tienda, cliente_nombre
    /// - clientId, groupId, typeId: filtros por catálogos
    /// - sort: orden múltiple (ejemplo: fecha_trabajo:desc,hora_inicio:asc)
    /// 
    /// **Ejemplo:**
    /// GET /api/v2/informes/partes?date=2026-02-14&amp;page=1&amp;pageSize=50&amp;sort=fecha_trabajo:desc
    /// </remarks>
    [HttpGet("partes")]
    [ProducesResponseType(typeof(PartesResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPartes([FromQuery] PartesQueryDto query)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "USER";

            var result = await _informesService.GetPartesAsync(query, userId, userRole);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("403 Forbidden: {Message}", ex.Message);
            return StatusCode(403, new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("400 Bad Request: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Obtiene resumen estadístico con cálculo de solapes y gaps.</summary>
    /// <remarks>
    /// Calcula:
    /// - partsCount: número de partes
    /// - recordedMinutes: suma total de minutos registrados (puede tener solapes)
    /// - coveredMinutes: minutos reales cubiertos (sin solapes)
    /// - overlapMinutes: minutos de solape detectados
    /// - mergedIntervals: intervalos unificados después de eliminar solapes
    /// - gaps: espacios entre intervalos consecutivos
    /// - firstStart, lastEnd: inicio y fin global
    /// - byDay: resumen por día (solo para scope=week o scope=range)
    /// 
    /// **Seguridad por roles:**
    /// - USER: Solo puede ver sus propios datos.
    /// - EDITOR/ADMIN: Pueden ver cualquier agente.
    /// 
    /// **Scopes:**
    /// - day: requiere date=YYYY-MM-DD
    /// - week: requiere weekIso=YYYY-Www
    /// - range: requiere from=YYYY-MM-DD&amp;to=YYYY-MM-DD
    /// 
    /// **Ejemplo:**
    /// GET /api/v2/informes/resumen?scope=day&amp;date=2026-02-14
    /// GET /api/v2/informes/resumen?scope=week&amp;weekIso=2026-W07
    /// GET /api/v2/informes/resumen?scope=range&amp;from=2026-02-01&amp;to=2026-02-28&amp;agentId=...
    /// </remarks>
    [HttpGet("resumen")]
    [ProducesResponseType(typeof(ResumenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetResumen([FromQuery] ResumenQueryDto query)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "USER";

            var result = await _informesService.GetResumenAsync(query, userId, userRole);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("403 Forbidden: {Message}", ex.Message);
            return StatusCode(403, new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("400 Bad Request: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }
}
