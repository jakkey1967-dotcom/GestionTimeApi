using GestionTime.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GestionTime.Api.Middleware;

/// <summary>
/// Middleware para actualizar la presencia (LastSeenAt) de usuarios autenticados
/// </summary>
public class PresenceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PresenceMiddleware> _logger;
    
    // Throttle: solo actualizar si han pasado más de 30 segundos desde último update
    private const int THROTTLE_SECONDS = 30;

    public PresenceMiddleware(RequestDelegate next, ILogger<PresenceMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, GestionTimeDbContext db)
    {
        // Solo procesar si el usuario está autenticado
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var sidClaim = context.User.FindFirst("sid")?.Value;
            
            if (!string.IsNullOrEmpty(sidClaim) && Guid.TryParse(sidClaim, out var sessionId))
            {
                try
                {
                    // Buscar sesión activa
                    var session = await db.UserSessions
                        .Where(s => s.Id == sessionId && s.RevokedAt == null)
                        .FirstOrDefaultAsync();
                    
                    if (session != null)
                    {
                        var now = DateTime.UtcNow;
                        var elapsed = (now - session.LastSeenAt).TotalSeconds;
                        
                        // Solo actualizar si han pasado más de THROTTLE_SECONDS
                        if (elapsed > THROTTLE_SECONDS)
                        {
                            session.LastSeenAt = now;
                            await db.SaveChangesAsync();
                            
                            _logger.LogTrace("Presencia actualizada para sesión {SessionId} (último visto hace {Elapsed}s)", 
                                sessionId, (int)elapsed);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Sesión no encontrada o revocada: {SessionId}", sessionId);
                    }
                }
                catch (Exception ex)
                {
                    // No fallar la request si hay error actualizando presencia
                    _logger.LogError(ex, "Error actualizando presencia para sesión {SessionId}", sessionId);
                }
            }
        }
        
        await _next(context);
    }
}

/// <summary>
/// Extension method para registrar el middleware
/// </summary>
public static class PresenceMiddlewareExtensions
{
    public static IApplicationBuilder UsePresenceTracking(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<PresenceMiddleware>();
    }
}
