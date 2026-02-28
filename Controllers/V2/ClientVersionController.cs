using GestionTime.Api.Contracts.ClientVersion;
using GestionTime.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GestionTime.Api.Controllers.V2;

/// <summary>Control de versiones del cliente Desktop.</summary>
[ApiController]
[Route("api/v2/client-version")]
[Authorize]
[ApiExplorerSettings(GroupName = "v2")]
public class ClientVersionController : ControllerBase
{
    private readonly ClientVersionService _service;
    private readonly ILogger<ClientVersionController> _logger;

    public ClientVersionController(ClientVersionService service, ILogger<ClientVersionController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Registra la versión del cliente y devuelve si requiere actualización.</summary>
    /// <remarks>
    /// Cualquier usuario autenticado puede llamar a este endpoint para registrar su versión.
    /// El userId se extrae del JWT, nunca del body.
    ///
    /// **Respuesta:**
    /// - `updateRequired`: true si la versión está por debajo del mínimo obligatorio.
    /// - `updateAvailable`: true si existe una versión más nueva (pero no obligatoria).
    /// - `updateUrl`: URL de descarga (solo si hay actualización requerida o disponible).
    ///
    /// **Ejemplo:**
    /// POST /api/v2/client-version
    /// { "appVersion": "1.9.5-beta", "platform": "Desktop", "osVersion": "Windows 11", "machineName": "PC-JUAN" }
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(RegisterVersionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Register([FromBody] RegisterVersionRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { error = "Token inválido." });

        try
        {
            var result = await _service.RegisterAsync(userId, req, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registrando versión para userId={UserId}", userId);
            return StatusCode(500, new { error = "Error interno al registrar versión." });
        }
    }

    /// <summary>Devuelve la última versión registrada por cada usuario.</summary>
    [HttpGet("all")]
    [Authorize(Roles = "ADMIN,EDITOR")]
    [ProducesResponseType(typeof(List<ClientVersionSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        try
        {
            var result = await _service.GetLatestPerUserAsync(ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo versiones de clientes");
            return StatusCode(500, new { error = "Error interno." });
        }
    }

    /// <summary>Devuelve usuarios cuya versión está por debajo del mínimo requerido.</summary>
    [HttpGet("outdated")]
    [Authorize(Roles = "ADMIN,EDITOR")]
    [ProducesResponseType(typeof(List<ClientVersionSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOutdated(CancellationToken ct)
    {
        try
        {
            var result = await _service.GetOutdatedAsync(ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo usuarios desactualizados");
            return StatusCode(500, new { error = "Error interno." });
        }
    }
}
