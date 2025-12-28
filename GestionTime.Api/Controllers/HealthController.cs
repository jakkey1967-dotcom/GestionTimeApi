using Microsoft.AspNetCore.Mvc;

namespace GestionTime.Api.Controllers;

[ApiController]
[Route("api/v1/health")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Get()
    {
        _logger.LogDebug("Health check solicitado");
        return Ok(new { status = "ok" });
    }
}
