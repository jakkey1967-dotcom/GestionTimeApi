using GestionTime.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GestionTime.Api.Controllers;

[ApiController]
[Route("api/v1/health")]
public class HealthController : ControllerBase
{
    private readonly GestionTimeDbContext _db;
    private readonly ILogger<HealthController> _logger;

    public HealthController(GestionTimeDbContext db, ILogger<HealthController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Health check del backend + actualización automática de presencia
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        // 1. Si hay usuario autenticado, actualizar presencia automáticamente
        var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId != null && Guid.TryParse(userId, out var userIdGuid))
        {
            try
            {
                // Buscar la sesión activa del usuario (no revocada)
                var session = await _db.UserSessions
                    .FirstOrDefaultAsync(s => s.UserId == userIdGuid && s.RevokedAt == null);
                
                if (session != null)
                {
                    session.LastSeenAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                    _logger.LogDebug("Presencia actualizada para UserId: {UserId} via /health", userIdGuid);
                }
            }
            catch (Exception ex)
            {
                // No fallar el health check si la actualización de presencia falla
                _logger.LogWarning(ex, "Error actualizando presencia en /health para UserId: {UserId}", userIdGuid);
            }
        }
        
        // 2. Devolver siempre la misma respuesta (backward compatible)
        _logger.LogDebug("Health check solicitado{UserInfo}", 
            userId != null ? $" por UserId: {userId}" : " (anónimo)");
        return Ok(new { status = "ok" });
    }
}
