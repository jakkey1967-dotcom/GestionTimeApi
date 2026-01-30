using System.Text;

namespace GestionTime.Api.Middleware;

/// <summary>
/// Middleware que loguea el body de requests con error de validación
/// </summary>
public class RequestBodyLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestBodyLoggingMiddleware> _logger;

    public RequestBodyLoggingMiddleware(RequestDelegate next, ILogger<RequestBodyLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Solo loguear POST/PUT/PATCH con body
        if (context.Request.Method == "POST" || context.Request.Method == "PUT" || context.Request.Method == "PATCH")
        {
            _logger.LogInformation("🔍 RequestBodyLoggingMiddleware - Processing {Method} {Path}", 
                context.Request.Method, 
                context.Request.Path);

            // Habilitar buffering para poder leer el body múltiples veces
            context.Request.EnableBuffering();

            // Leer el body
            var bodyAsText = await ReadRequestBodyAsync(context.Request);

            // Resetear la posición del stream
            context.Request.Body.Position = 0;

            // Guardar para uso posterior si hay error
            context.Items["OriginalRequestBody"] = bodyAsText;
            
            _logger.LogDebug("📦 Request body length: {Length} bytes", bodyAsText?.Length ?? 0);

            // Capturar la respuesta
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            // Si la respuesta es 400, loguear el body original
            if (context.Response.StatusCode == 400)
            {
                _logger.LogWarning("╔══════════════════════════════════════════════════════════════");
                _logger.LogWarning("║ REQUEST BODY RAW (Status 400):");
                _logger.LogWarning("╠══════════════════════════════════════════════════════════════");
                _logger.LogWarning("║ Path: {Method} {Path}", context.Request.Method, context.Request.Path);
                _logger.LogWarning("║ Content-Type: {ContentType}", context.Request.ContentType ?? "null");
                _logger.LogWarning("╠══════════════════════════════════════════════════════════════");
                _logger.LogWarning("║ BODY:");
                
                if (!string.IsNullOrWhiteSpace(bodyAsText))
                {
                    var lines = bodyAsText.Split('\n');
                    foreach (var line in lines)
                    {
                        _logger.LogWarning("║ {Line}", line);
                    }
                }
                else
                {
                    _logger.LogWarning("║ <vacío>");
                }
                
                _logger.LogWarning("╚══════════════════════════════════════════════════════════════");
            }

            // Copiar la respuesta de vuelta al stream original
            context.Response.Body.Position = 0;
            await responseBody.CopyToAsync(originalBodyStream);
        }
        else
        {
            await _next(context);
        }
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        using var reader = new StreamReader(
            request.Body,
            encoding: Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024,
            leaveOpen: true);

        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;
        return body;
    }
}
