using GestionTime.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionTime.Api.Controllers;

/// <summary>
/// Endpoint público para consultar usuarios online (presencia)
/// </summary>
[ApiController]
[Route("api/v1/presence")]
[Authorize] // Cualquier usuario autenticado puede consultar
public class PresenceController : ControllerBase
{
    private readonly GestionTimeDbContext _db;
    private readonly ILogger<PresenceController> _logger;
    
    // Considerar usuario online si lastSeenAt < 30 segundos
    private const int ONLINE_THRESHOLD_SECONDS = 30;

    public PresenceController(GestionTimeDbContext db, ILogger<PresenceController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene lista de todos los usuarios con su estado de presencia
    /// </summary>
    /// <returns>Lista ordenada: ADMIN>EDITOR>USER, online primero, luego alfabético</returns>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var onlineThreshold = DateTime.UtcNow.AddSeconds(-ONLINE_THRESHOLD_SECONDS);
        
        // Obtener usuarios con sus roles y última actividad de sesión
        var users = await _db.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.Sessions.Where(s => s.RevokedAt == null))
            .Where(u => u.Enabled)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList(),
                // LastSeenAt = max de todas las sesiones activas
                LastSeenAt = u.Sessions
                    .Where(s => s.RevokedAt == null)
                    .Max(s => (DateTime?)s.LastSeenAt)
            })
            .ToListAsync();
        
        // Mapear a respuesta con isOnline y role prioritario
        var result = users.Select(u =>
        {
            var role = GetPriorityRole(u.Roles);
            var isOnline = u.LastSeenAt.HasValue && u.LastSeenAt.Value >= onlineThreshold;
            
            return new
            {
                userId = u.Id,
                fullName = u.FullName ?? u.Email?.Split('@')[0] ?? "Usuario",
                email = u.Email,
                role = role,
                lastSeenAt = u.LastSeenAt,
                isOnline = isOnline,
                // Para ordenamiento
                rolePriority = GetRolePriority(role)
            };
        })
        .OrderBy(u => u.rolePriority)      // ADMIN primero (0), luego EDITOR (1), USER (2)
        .ThenByDescending(u => u.isOnline) // Online primero
        .ThenBy(u => u.fullName)           // Alfabético
        .Select(u => new
        {
            u.userId,
            u.fullName,
            u.email,
            u.role,
            u.lastSeenAt,
            u.isOnline
        })
        .ToList();
        
        _logger.LogDebug("Consulta presencia: {TotalUsers} usuarios, {OnlineUsers} online", 
            result.Count, result.Count(u => u.isOnline));
        
        return Ok(result);
    }

    /// <summary>
    /// Obtiene el rol de mayor prioridad
    /// </summary>
    private static string GetPriorityRole(List<string> roles)
    {
        if (roles.Contains("ADMIN")) return "ADMIN";
        if (roles.Contains("EDITOR")) return "EDITOR";
        if (roles.Contains("USER")) return "USER";
        return "USER";
    }

    /// <summary>
    /// Obtiene la prioridad numérica del rol (menor = mayor prioridad)
    /// </summary>
    private static int GetRolePriority(string role)
    {
        return role switch
        {
            "ADMIN" => 0,
            "EDITOR" => 1,
            "USER" => 2,
            _ => 99
        };
    }
}
