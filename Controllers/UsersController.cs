using GestionTime.Api.Contracts.Users;
using GestionTime.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GestionTime.Api.Controllers;

/// <summary>Gestión de usuarios y roles (solo para Admin).</summary>
[ApiController]
[Route("api/v1/users")]
[Authorize(Roles = "Admin,ADMIN")]
public class UsersController(
    GestionTimeDbContext db,
    ILogger<UsersController> logger) : ControllerBase
{
    /// <summary>Lista todos los usuarios con sus roles.</summary>
    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var adminEmail = User.FindFirstValue(ClaimTypes.Email);
        logger.LogInformation("Admin {Email} consultando lista de usuarios (page: {Page}, pageSize: {PageSize})", 
            adminEmail, page, pageSize);

        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 50;

        var query = db.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .AsQueryable();

        var total = await query.CountAsync();
        
        var users = await query
            .OrderBy(u => u.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email ?? "",
                FullName = u.FullName ?? "",
                Enabled = u.Enabled,
                EmailConfirmed = u.EmailConfirmed,
                MustChangePassword = u.MustChangePassword,
                Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
            })
            .ToListAsync();

        return Ok(new
        {
            total = total,
            page = page,
            pageSize = pageSize,
            totalPages = (int)Math.Ceiling(total / (double)pageSize),
            users = users
        });
    }

    /// <summary>Obtiene un usuario específico por ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var adminEmail = User.FindFirstValue(ClaimTypes.Email);
        logger.LogInformation("Admin {Email} consultando usuario {UserId}", adminEmail, id);

        var user = await db.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .SingleOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound(new { message = "Usuario no encontrado" });
        }

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? "",
            FullName = user.FullName ?? "",
            Enabled = user.Enabled,
            EmailConfirmed = user.EmailConfirmed,
            MustChangePassword = user.MustChangePassword,
            Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
        });
    }

    /// <summary>Lista todos los roles disponibles del sistema.</summary>
    [HttpGet("~/api/v1/roles")]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await db.Roles
            .OrderBy(r => r.Name)
            .Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name
            })
            .ToListAsync();

        return Ok(new { roles = roles });
    }

    /// <summary>Actualiza los roles de un usuario.</summary>
    [HttpPut("{id:guid}/roles")]
    public async Task<IActionResult> UpdateUserRoles(
        Guid id,
        [FromBody] UpdateUserRolesRequest request)
    {
        var adminEmail = User.FindFirstValue(ClaimTypes.Email);
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        logger.LogInformation("Admin {Email} actualizando roles de usuario {UserId} a: {Roles}", 
            adminEmail, id, string.Join(", ", request.Roles));

        // Validar que no sea el propio admin
        if (id.ToString() == adminId)
        {
            logger.LogWarning("Admin {Email} intentó modificar sus propios roles", adminEmail);
            return BadRequest(new { message = "No puedes modificar tus propios roles" });
        }

        var user = await db.Users
            .Include(u => u.UserRoles)
            .SingleOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound(new { message = "Usuario no encontrado" });
        }

        // Validar que los roles existan
        var validRoles = await db.Roles
            .Where(r => request.Roles.Contains(r.Name))
            .ToListAsync();

        if (validRoles.Count != request.Roles.Count)
        {
            var invalidRoles = request.Roles.Except(validRoles.Select(r => r.Name)).ToList();
            return BadRequest(new { 
                message = "Roles inválidos", 
                invalidRoles = invalidRoles 
            });
        }

        // Eliminar roles actuales
        db.UserRoles.RemoveRange(user.UserRoles);

        // Asignar nuevos roles
        foreach (var roleName in request.Roles)
        {
            var role = validRoles.First(r => r.Name == roleName);
            db.UserRoles.Add(new GestionTime.Domain.Auth.UserRole
            {
                UserId = user.Id,
                RoleId = role.Id
            });
        }

        await db.SaveChangesAsync();

        logger.LogInformation("✅ Roles actualizados exitosamente para usuario {Email} (UserId: {UserId})", 
            user.Email, user.Id);

        return Ok(new
        {
            message = "Roles actualizados exitosamente",
            userId = user.Id,
            email = user.Email,
            roles = request.Roles
        });
    }

    /// <summary>Habilita o deshabilita un usuario.</summary>
    [HttpPut("{id:guid}/enabled")]
    public async Task<IActionResult> UpdateUserEnabled(
        Guid id,
        [FromBody] UpdateUserEnabledRequest request)
    {
        var adminEmail = User.FindFirstValue(ClaimTypes.Email);
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        logger.LogInformation("Admin {Email} cambiando estado de usuario {UserId} a: {Enabled}", 
            adminEmail, id, request.Enabled);

        // Validar que no sea el propio admin
        if (id.ToString() == adminId)
        {
            logger.LogWarning("Admin {Email} intentó deshabilitarse a sí mismo", adminEmail);
            return BadRequest(new { message = "No puedes deshabilitarte a ti mismo" });
        }

        var user = await db.Users.FindAsync(id);

        if (user == null)
        {
            return NotFound(new { message = "Usuario no encontrado" });
        }

        user.Enabled = request.Enabled;
        await db.SaveChangesAsync();

        logger.LogInformation("✅ Usuario {Email} (UserId: {UserId}) {Status}", 
            user.Email, user.Id, request.Enabled ? "habilitado" : "deshabilitado");

        return Ok(new
        {
            message = request.Enabled ? "Usuario habilitado" : "Usuario deshabilitado",
            userId = user.Id,
            email = user.Email,
            enabled = user.Enabled
        });
    }
}
