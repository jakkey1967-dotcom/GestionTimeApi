using GestionTime.Api.Contracts.Users;
using GestionTime.Domain.Auth;
using GestionTime.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GestionTime.Api.Controllers;

[ApiController]
[Route("api/v1/profiles")]
[Authorize]
public class ProfilesController : ControllerBase
{
    private readonly GestionTimeDbContext _db;
    private readonly ILogger<ProfilesController> _logger;

    public ProfilesController(GestionTimeDbContext db, ILogger<ProfilesController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // GET /api/v1/profiles/me - Obtener mi perfil
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = GetUserId();
        _logger.LogInformation("Usuario {UserId} consultando su perfil", userId);

        var profile = await _db.UserProfiles
            .AsNoTracking()
            .Where(p => p.Id == userId)
            .Select(p => new
            {
                id = p.Id,
                first_name = p.FirstName,
                last_name = p.LastName,
                full_name = p.FullName,
                phone = p.Phone,
                mobile = p.Mobile,
                address = p.Address,
                city = p.City,
                postal_code = p.PostalCode,
                department = p.Department,
                position = p.Position,
                employee_type = p.EmployeeType,
                hire_date = p.HireDate,
                avatar_url = p.AvatarUrl,
                notes = p.Notes,
                created_at = p.CreatedAt,
                updated_at = p.UpdatedAt
            })
            .SingleOrDefaultAsync();

        if (profile is null)
        {
            _logger.LogWarning("Perfil no encontrado para usuario {UserId}", userId);
            return NotFound(new { message = "Perfil no encontrado" });
        }

        return Ok(profile);
    }

    // PUT /api/v1/profiles/me - Actualizar mi perfil
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest req)
    {
        var userId = GetUserId();
        _logger.LogInformation("Usuario {UserId} actualizando su perfil", userId);

        var profile = await _db.UserProfiles.SingleOrDefaultAsync(p => p.Id == userId);

        if (profile is null)
        {
            // Crear perfil si no existe
            _logger.LogInformation("Creando perfil para usuario {UserId}", userId);
            profile = new UserProfile
            {
                Id = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.UserProfiles.Add(profile);
        }

        // Actualizar campos
        profile.FirstName = req.first_name;
        profile.LastName = req.last_name;
        profile.Phone = req.phone;
        profile.Mobile = req.mobile;
        profile.Address = req.address;
        profile.City = req.city;
        profile.PostalCode = req.postal_code;
        profile.Department = req.department;
        profile.Position = req.position;
        profile.EmployeeType = req.employee_type;
        profile.HireDate = req.hire_date;
        profile.AvatarUrl = req.avatar_url;
        profile.Notes = req.notes;
        profile.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Perfil actualizado para usuario {UserId}", userId);

        return Ok(new { message = "Perfil actualizado correctamente" });
    }

    // GET /api/v1/profiles/{id} - Obtener perfil de otro usuario (solo admin)
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> GetProfile(Guid id)
    {
        _logger.LogInformation("Consultando perfil del usuario {UserId}", id);

        var profile = await _db.UserProfiles
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new
            {
                id = p.Id,
                first_name = p.FirstName,
                last_name = p.LastName,
                full_name = p.FullName,
                phone = p.Phone,
                mobile = p.Mobile,
                address = p.Address,
                city = p.City,
                postal_code = p.PostalCode,
                department = p.Department,
                position = p.Position,
                employee_type = p.EmployeeType,
                hire_date = p.HireDate,
                avatar_url = p.AvatarUrl,
                notes = p.Notes,
                created_at = p.CreatedAt,
                updated_at = p.UpdatedAt
            })
            .SingleOrDefaultAsync();

        if (profile is null)
        {
            _logger.LogWarning("Perfil no encontrado para usuario {UserId}", id);
            return NotFound(new { message = "Perfil no encontrado" });
        }

        return Ok(profile);
    }

    // PUT /api/v1/profiles/{id} - Actualizar perfil de otro usuario (solo admin)
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> UpdateProfile(Guid id, [FromBody] UpdateProfileRequest req)
    {
        _logger.LogInformation("Actualizando perfil del usuario {UserId}", id);

        // Verificar que el usuario existe
        var userExists = await _db.Users.AnyAsync(u => u.Id == id);
        if (!userExists)
        {
            _logger.LogWarning("Usuario {UserId} no encontrado", id);
            return NotFound(new { message = "Usuario no encontrado" });
        }

        var profile = await _db.UserProfiles.SingleOrDefaultAsync(p => p.Id == id);

        if (profile is null)
        {
            // Crear perfil si no existe
            _logger.LogInformation("Creando perfil para usuario {UserId}", id);
            profile = new UserProfile
            {
                Id = id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.UserProfiles.Add(profile);
        }

        // Actualizar campos
        profile.FirstName = req.first_name;
        profile.LastName = req.last_name;
        profile.Phone = req.phone;
        profile.Mobile = req.mobile;
        profile.Address = req.address;
        profile.City = req.city;
        profile.PostalCode = req.postal_code;
        profile.Department = req.department;
        profile.Position = req.position;
        profile.EmployeeType = req.employee_type;
        profile.HireDate = req.hire_date;
        profile.AvatarUrl = req.avatar_url;
        profile.Notes = req.notes;
        profile.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Perfil actualizado para usuario {UserId}", id);

        return Ok(new { message = "Perfil actualizado correctamente" });
    }

    // GET /api/v1/profiles - Listar todos los perfiles (solo admin)
    [HttpGet]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> ListProfiles(
        [FromQuery] string? employee_type,
        [FromQuery] string? department)
    {
        _logger.LogInformation("Listando perfiles");

        var query = _db.UserProfiles
            .AsNoTracking()
            .Include(p => p.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(employee_type))
        {
            query = query.Where(p => p.EmployeeType == employee_type);
        }

        if (!string.IsNullOrWhiteSpace(department))
        {
            query = query.Where(p => p.Department == department);
        }

        var profiles = await query
            .Select(p => new
            {
                id = p.Id,
                email = p.User.Email,
                first_name = p.FirstName,
                last_name = p.LastName,
                full_name = p.FullName,
                phone = p.Phone,
                mobile = p.Mobile,
                department = p.Department,
                position = p.Position,
                employee_type = p.EmployeeType,
                hire_date = p.HireDate,
                enabled = p.User.Enabled
            })
            .OrderBy(p => p.last_name)
            .ThenBy(p => p.first_name)
            .ToListAsync();

        return Ok(profiles);
    }

    private Guid GetUserId()
    {
        var s = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(s, out var id)) return id;
        
        _logger.LogError("No se pudo obtener el UserId del token");
        throw new UnauthorizedAccessException("Missing NameIdentifier");
    }
}
