using BCrypt.Net;
using GestionTime.Domain.Auth;
using GestionTime.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionTime.Api.Controllers;

[ApiController]
[Route("api/v1/admin/users")]
[Authorize(Roles = "ADMIN")]
public class AdminUsersController : ControllerBase
{
    private readonly GestionTimeDbContext db;
    private readonly ILogger<AdminUsersController> _logger;

    public AdminUsersController(GestionTimeDbContext db, ILogger<AdminUsersController> logger)
    {
        this.db = db;
        _logger = logger;
    }

    public sealed record CreateUserRequest(
        string Email,
        string FullName,
        string Password,
        string[] Roles
    );

    public sealed record SetRolesRequest(string[] Roles);

    public sealed record SetEnabledRequest(bool Enabled);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? q, [FromQuery] int limit = 50, [FromQuery] int offset = 0)
    {
        limit = Math.Clamp(limit, 1, 200);
        offset = Math.Max(offset, 0);

        _logger.LogDebug("Listando usuarios. Query: {Query}, Limit: {Limit}, Offset: {Offset}", q, limit, offset);

        var usersQuery = db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim().ToLower();
            usersQuery = usersQuery.Where(u =>
                u.Email.ToLower().Contains(q) ||
                u.FullName.ToLower().Contains(q));
        }

        var users = await usersQuery
            .OrderBy(u => u.Email)
            .Skip(offset)
            .Take(limit)
            .Select(u => new { u.Id, u.Email, u.FullName, u.Enabled })
            .ToListAsync();

        var ids = users.Select(u => u.Id).ToList();

        var roles = await (
            from ur in db.UserRoles.AsNoTracking()
            join r in db.Roles.AsNoTracking() on ur.RoleId equals r.Id
            where ids.Contains(ur.UserId)
            select new { ur.UserId, r.Name }
        ).ToListAsync();

        var rolesByUser = roles
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).Distinct().ToArray());

        var result = users.Select(u => new
        {
            u.Id,
            u.Email,
            u.FullName,
            u.Enabled,
            roles = rolesByUser.TryGetValue(u.Id, out var rr) ? rr : Array.Empty<string>()
        });

        _logger.LogInformation("Listados {Count} usuarios", users.Count);

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest req)
    {
        _logger.LogInformation("Creando nuevo usuario: {Email}", req.Email);

        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password) || string.IsNullOrWhiteSpace(req.FullName))
        {
            _logger.LogWarning("Creación de usuario fallida: campos obligatorios vacíos");
            return BadRequest(new { message = "Email, FullName y Password son obligatorios." });
        }

        var email = req.Email.Trim().ToLower();

        var exists = await db.Users.AnyAsync(u => u.Email.ToLower() == email);
        if (exists)
        {
            _logger.LogWarning("Creación de usuario fallida: email {Email} ya existe", email);
            return Conflict(new { message = "Ya existe un usuario con ese email." });
        }

        var roleNames = (req.Roles ?? Array.Empty<string>())
            .Select(x => (x ?? "").Trim().ToUpper())
            .Where(x => x.Length > 0)
            .Distinct()
            .ToArray();

        if (roleNames.Length == 0)
            roleNames = new[] { "USER" };

        var roles = await db.Roles.Where(r => roleNames.Contains(r.Name)).ToListAsync();
        if (roles.Count != roleNames.Length)
        {
            var found = roles.Select(r => r.Name).ToHashSet();
            var missing = roleNames.Where(r => !found.Contains(r)).ToArray();
            _logger.LogWarning("Creación de usuario fallida: roles inválidos {Roles}", string.Join(", ", missing));
            return BadRequest(new { message = "Roles inválidos.", missing });
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FullName = req.FullName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Enabled = true,
            EmailConfirmed = true  // ✅ Usuarios creados por admin están pre-confirmados
        };

        await using var tx = await db.Database.BeginTransactionAsync();

        db.Users.Add(user);
        await db.SaveChangesAsync();

        foreach (var r in roles)
            db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = r.Id });

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        _logger.LogInformation("Usuario creado exitosamente: {UserId} ({Email}) con roles {Roles}", 
            user.Id, email, string.Join(", ", roleNames));

        return Ok(new { user.Id, user.Email, user.FullName });
    }

    [HttpPut("{id:guid}/roles")]
    public async Task<IActionResult> SetRoles([FromRoute] Guid id, [FromBody] SetRolesRequest req)
    {
        _logger.LogInformation("Actualizando roles para usuario {UserId}", id);

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
        {
            _logger.LogWarning("Usuario {UserId} no encontrado", id);
            return NotFound();
        }

        var roleNames = (req.Roles ?? Array.Empty<string>())
            .Select(x => (x ?? "").Trim().ToUpper())
            .Where(x => x.Length > 0)
            .Distinct()
            .ToArray();

        if (roleNames.Length == 0)
            roleNames = new[] { "USER" };

        var roles = await db.Roles.Where(r => roleNames.Contains(r.Name)).ToListAsync();
        if (roles.Count != roleNames.Length)
        {
            var found = roles.Select(r => r.Name).ToHashSet();
            var missing = roleNames.Where(r => !found.Contains(r)).ToArray();
            _logger.LogWarning("Actualización de roles fallida: roles inválidos {Roles}", string.Join(", ", missing));
            return BadRequest(new { message = "Roles inválidos.", missing });
        }

        await using var tx = await db.Database.BeginTransactionAsync();

        var current = await db.UserRoles.Where(ur => ur.UserId == id).ToListAsync();
        db.UserRoles.RemoveRange(current);

        foreach (var r in roles)
            db.UserRoles.Add(new UserRole { UserId = id, RoleId = r.Id });

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        _logger.LogInformation("Roles actualizados para {UserId}: {Roles}", id, string.Join(", ", roleNames));

        return Ok(new { message = "Roles actualizados." });
    }

    [HttpPut("{id:guid}/enabled")]
    public async Task<IActionResult> SetEnabled([FromRoute] Guid id, [FromBody] SetEnabledRequest req)
    {
        _logger.LogInformation("{Action} usuario {UserId}", req.Enabled ? "Habilitando" : "Deshabilitando", id);

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
        {
            _logger.LogWarning("Usuario {UserId} no encontrado", id);
            return NotFound();
        }

        user.Enabled = req.Enabled;
        await db.SaveChangesAsync();

        _logger.LogInformation("Usuario {UserId} ahora está {Estado}", id, req.Enabled ? "habilitado" : "deshabilitado");

        return Ok(new { message = "Estado actualizado.", enabled = user.Enabled });
    }

    public sealed record ResetPasswordRequest(string NewPassword);

    [HttpPost("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword([FromRoute] Guid id, [FromBody] ResetPasswordRequest req)
    {
        _logger.LogInformation("Reset de password solicitado para usuario {UserId}", id);

        if (string.IsNullOrWhiteSpace(req.NewPassword))
        {
            _logger.LogWarning("Reset de password fallido: password vacío");
            return BadRequest(new { message = "NewPassword es obligatorio." });
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
        {
            _logger.LogWarning("Usuario {UserId} no encontrado", id);
            return NotFound();
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        await db.SaveChangesAsync();

        _logger.LogInformation("Password actualizado para usuario {UserId}", id);

        return Ok(new { message = "Password actualizado." });
    }
}

