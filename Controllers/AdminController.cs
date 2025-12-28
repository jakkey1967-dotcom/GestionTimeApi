using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionTime.Api.Controllers;

[ApiController]
[Route("api/v1/admin")]
public class AdminController : ControllerBase
{
    private readonly ILogger<AdminController> _logger;

    public AdminController(ILogger<AdminController> logger)
    {
        _logger = logger;
    }

    [Authorize(Roles = "ADMIN")]
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        _logger.LogDebug("Admin ping recibido");
        return Ok(new { ok = true, role = "ADMIN" });
    }
}
