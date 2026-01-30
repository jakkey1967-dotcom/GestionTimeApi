using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.Json;

namespace GestionTime.Api.Filters;

/// <summary>
/// Filtro que loguea detalles completos de errores de validación
/// </summary>
public class ValidationLoggingFilter : IActionFilter
{
    private readonly ILogger<ValidationLoggingFilter> _logger;

    public ValidationLoggingFilter(ILogger<ValidationLoggingFilter> logger)
    {
        _logger = logger;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        // Log siempre para confirmar que el filtro se ejecuta
        _logger.LogInformation("🔍 ValidationLoggingFilter.OnActionExecuting - {Controller}.{Action} - ModelState.IsValid={IsValid}, ErrorCount={ErrorCount}",
            context.RouteData.Values["controller"],
            context.RouteData.Values["action"],
            context.ModelState.IsValid,
            context.ModelState.ErrorCount);

        // Si ModelState es inválido, loguear detalles ANTES de que [ApiController] lo maneje
        if (!context.ModelState.IsValid)
        {
            _logger.LogWarning("╔══════════════════════════════════════════════════════════════");
            _logger.LogWarning("║ VALIDACIÓN FALLIDA - ModelState Inválido");
            _logger.LogWarning("╠══════════════════════════════════════════════════════════════");
            _logger.LogWarning("║ Action: {Controller}.{Action}", 
                context.RouteData.Values["controller"], 
                context.RouteData.Values["action"]);
            _logger.LogWarning("║ HTTP: {Method} {Path}", 
                context.HttpContext.Request.Method,
                context.HttpContext.Request.Path);
            _logger.LogWarning("╠══════════════════════════════════════════════════════════════");
            _logger.LogWarning("║ ERRORES DE VALIDACIÓN:");
            _logger.LogWarning("╠══════════════════════════════════════════════════════════════");

            foreach (var (key, value) in context.ModelState)
            {
                if (value.Errors.Any())
                {
                    _logger.LogWarning("║ Campo: '{Key}'", key);
                    foreach (var error in value.Errors)
                    {
                        _logger.LogWarning("║   ❌ {ErrorMessage}", 
                            error.ErrorMessage ?? error.Exception?.Message ?? "Error desconocido");
                    }
                }
            }

            _logger.LogWarning("╠══════════════════════════════════════════════════════════════");
            _logger.LogWarning("║ PARÁMETROS RECIBIDOS:");
            _logger.LogWarning("╠══════════════════════════════════════════════════════════════");

            foreach (var (key, value) in context.ActionArguments)
            {
                try
                {
                    var json = JsonSerializer.Serialize(value, new JsonSerializerOptions 
                    { 
                        WriteIndented = true 
                    });
                    _logger.LogWarning("║ {Key}:\n{Json}", key, json);
                }
                catch
                {
                    _logger.LogWarning("║ {Key}: <no serializable>", key);
                }
            }

            _logger.LogWarning("╠══════════════════════════════════════════════════════════════");
            _logger.LogWarning("║ CAUSA: Validación automática de [ApiController]");
            _logger.LogWarning("║ El request fue rechazado ANTES de llegar al action method");
            _logger.LogWarning("╚══════════════════════════════════════════════════════════════");
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // No hacer nada después de la ejecución
    }
}
