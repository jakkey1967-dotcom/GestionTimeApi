using Microsoft.AspNetCore.Mvc;

namespace GestionTime.Api.Controllers;

[ApiController]
public class RootController : ControllerBase
{
    private readonly ILogger<RootController> _logger;

    public RootController(ILogger<RootController> logger)
    {
        _logger = logger;
    }

    [HttpGet("/")]
    public IActionResult GetRoot()
    {
        _logger.LogDebug("Root endpoint solicitado");
        var response = new
        {
            name = "GestionTime API",
            version = "1.0.0",
            status = "running",
            message = "Bienvenido a GestionTime API - Sistema de gestión de tiempo y partes de trabajo",
            endpoints = new
            {
                swagger = "/swagger",
                health = "/health",
                api = "/api/v1"
            },
            documentation = "Visita /swagger para ver la documentación completa de la API"
        };

        return Ok(response);
    }
}
