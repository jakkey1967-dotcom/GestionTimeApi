using GestionTime.Api.Contracts.Admin;
using GestionTime.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionTime.Api.Controllers.V2;

/// <summary>Endpoints ADMIN para salud de clientes Desktop, campañas de email e histórico.</summary>
[ApiController]
[Route("api/v2/admin")]
[Authorize(Roles = "ADMIN")]
[ApiExplorerSettings(GroupName = "v2")]
public class AdminDesktopController : ControllerBase
{
    private readonly DesktopClientHealthService _healthService;
    private readonly DesktopClientCampaignService _campaignService;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<AdminDesktopController> _logger;

    public AdminDesktopController(
        DesktopClientHealthService healthService,
        DesktopClientCampaignService campaignService,
        IEmailSender emailSender,
        ILogger<AdminDesktopController> logger)
    {
        _healthService = healthService;
        _campaignService = campaignService;
        _emailSender = emailSender;
        _logger = logger;
    }

    // GL-BEGIN: GetDesktopClientHealth (P2)
    /// <summary>Estado de salud de clientes Desktop por usuario (paginado, filtrable).</summary>
    [HttpGet("desktop-client-health")]
    [ProducesResponseType(typeof(DesktopClientHealthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDesktopClientHealth(
        [FromQuery] DesktopClientHealthQuery query, CancellationToken ct)
    {
        try
        {
            var result = await _healthService.GetHealthAsync(query, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consultando desktop-client-health");
            return StatusCode(500, new { error = "Error interno al consultar salud de clientes." });
        }
    }
    // GL-END: GetDesktopClientHealth

    // GL-BEGIN: RunCampaign (P3)
    /// <summary>Ejecuta la campaña de emails Desktop (dry-run o real).</summary>
    [HttpPost("desktop-campaign/run-now")]
    [ProducesResponseType(typeof(DesktopCampaignRunResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RunCampaignNow(
        [FromQuery] bool dryRun = true, CancellationToken ct = default)
    {
        try
        {
            var result = await _campaignService.RunCampaignAsync(dryRun, ct);

            // Si no es dry-run, enviar los PENDING
            if (!dryRun && result.Enqueued > 0)
            {
                var (sent, errors) = await _campaignService.SendPendingAsync(_emailSender, ct);
                _logger.LogInformation("RunCampaignNow: sent={Sent} errors={Errors}", sent, errors);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ejecutando campaña Desktop");
            return StatusCode(500, new { error = "Error interno al ejecutar campaña." });
        }
    }
    // GL-END: RunCampaign

    // GL-BEGIN: SendManualEmail
    /// <summary>Envía manualmente el correo de novedades Desktop a un destinatario específico.</summary>
    [HttpPost("desktop-campaign/send-manual")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SendManualEmail(
        [FromBody] SendManualEmailRequest request, CancellationToken ct)
    {
        try
        {
            await _campaignService.SendManualAsync(request.Email, request.FullName, _emailSender, ct);
            return Ok(new { sent = true, email = request.Email, fullName = request.FullName });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando email manual a {Email}", request.Email);
            return StatusCode(500, new { error = $"Error enviando email: {ex.Message}" });
        }
    }
    // GL-END: SendManualEmail

    // GL-BEGIN: GetEmailHistory (P6)
    /// <summary>Histórico de emails enviados a un usuario (paginado).</summary>
    [HttpGet("desktop-client-health/emails")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetEmailHistory(
        [FromQuery] EmailOutboxQuery query, CancellationToken ct)
    {
        try
        {
            var (items, total) = await _healthService.GetEmailHistoryAsync(query, ct);
            return Ok(new
            {
                userId = query.UserId,
                page = query.Page,
                pageSize = query.PageSize,
                total,
                items
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consultando histórico de emails userId={UserId}", query.UserId);
            return StatusCode(500, new { error = "Error interno al consultar histórico de emails." });
        }
    }
    // GL-END: GetEmailHistory
}
