using Microsoft.AspNetCore.Mvc;

namespace GestionTime.Api.Filters;

/// <summary>
/// Respuesta personalizada de validación con ejemplos útiles
/// </summary>
public class CustomValidationProblemDetails : ValidationProblemDetails
{
    public object? ReceivedData { get; set; }
    public string? Suggestion { get; set; }
    public object? Example { get; set; }

    public CustomValidationProblemDetails(IDictionary<string, string[]> errors, string? suggestion = null, object? example = null)
        : base(errors)
    {
        Title = "Error de validación en el request";
        Status = 400;
        Type = "https://httpstatuses.com/400";
        Suggestion = suggestion;
        Example = example;
    }
}
