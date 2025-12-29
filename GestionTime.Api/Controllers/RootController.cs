using Microsoft.AspNetCore.Mvc;

namespace GestionTime.Api.Controllers;

[ApiController]
public class RootController : ControllerBase
{
    [HttpGet("/")]
    public IActionResult GetRoot()
    {
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
