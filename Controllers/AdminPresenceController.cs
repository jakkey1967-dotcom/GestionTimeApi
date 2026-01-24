using GestionTime.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GestionTime.Api.Controllers;

/// <summary>
/// Endpoints administrativos para gestión de presencia (solo ADMIN)
/// </summary>
[ApiController]
[Route("api/v1/admin/presence")]
[Authorize(Roles = "ADMIN")]
public class AdminPresenceController : ControllerBase
{
    private readonly GestionTimeDbContext _db;
    private readonly ILogger<AdminPresenceController> _logger;

    public AdminPresenceController(GestionTimeDbContext db, ILogger<AdminPresenceController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Revoca todas las sesiones activas de un usuario (kick)
    /// </summary>
    /// <param name="userId">ID del usuario a desconectar</param>
    [HttpPost("users/{userId:guid}/kick")]
    public async Task<IActionResult> KickUser(Guid userId)
    {
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var adminEmail = User.FindFirst(ClaimTypes.Email)?.Value;
        
        _logger.LogInformation("Admin {AdminEmail} solicita kick para usuario {UserId}", adminEmail, userId);
        
        // Verificar que el usuario existe
        var targetUser = await _db.Users.FindAsync(userId);
        if (targetUser == null)
        {
            return NotFound(new { success = false, message = "Usuario no encontrado" });
        }
        
        // Obtener todas las sesiones activas del usuario
        var activeSessions = await _db.UserSessions
            .Where(s => s.UserId == userId && s.RevokedAt == null)
            .ToListAsync();
        
        if (activeSessions.Count == 0)
        {
            return Ok(new 
            { 
                success = true, 
                message = "Usuario no tiene sesiones activas",
                sessionsRevoked = 0
            });
        }
        
        // Revocar todas las sesiones
        var now = DateTime.UtcNow;
        foreach (var session in activeSessions)
        {
            session.RevokedAt = now;
        }
        
        await _db.SaveChangesAsync();
        
        _logger.LogWarning("Admin {AdminEmail} revocó {Count} sesiones del usuario {UserEmail} (UserId: {UserId})", 
            adminEmail, activeSessions.Count, targetUser.Email, userId);
        
        return Ok(new 
        { 
            success = true, 
            message = $"Se revocaron {activeSessions.Count} sesión(es) activa(s)",
            sessionsRevoked = activeSessions.Count,
            userEmail = targetUser.Email
        });
    }
}
