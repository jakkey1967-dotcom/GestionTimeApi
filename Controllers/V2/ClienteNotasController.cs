using GestionTime.Api.Contracts.Catalog;
using GestionTime.Domain.Work;
using GestionTime.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GestionTime.Api.Controllers.V2;

/// <summary>Notas de cliente: globales (EDITOR/ADMIN) y personales (por usuario).</summary>
[ApiController]
[Route("api/v2/clientes/{clienteId:int}/notas")]
[Authorize]
[Produces("application/json")]
[Tags("ClienteNotas")]
[ApiExplorerSettings(GroupName = "v2")]
public class ClienteNotasController : ControllerBase
{
    private readonly GestionTimeDbContext _db;
    private readonly ILogger<ClienteNotasController> _logger;

    public ClienteNotasController(GestionTimeDbContext db, ILogger<ClienteNotasController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>Obtiene nota global + nota personal del usuario autenticado.</summary>
    [HttpGet]
    public async Task<ActionResult<ClienteNotasResponseDto>> GetNotas(int clienteId)
    {
        var userId = GetUserId();

        _logger.LogInformation("GetClienteNotas: clienteId={ClienteId}, userId={UserId}", clienteId, userId);

        // Verificar que el cliente existe
        var clienteExists = await _db.Clientes.AnyAsync(c => c.Id == clienteId);
        if (!clienteExists)
            return NotFound(new { message = $"Cliente {clienteId} no encontrado." });

        // Nota global (owner_user_id IS NULL)
        var globalNota = await _db.ClienteNotas
            .AsNoTracking()
            .Where(n => n.ClienteId == clienteId && n.OwnerUserId == null)
            .Select(n => new { n.Nota, n.UpdatedAt, n.UpdatedBy })
            .FirstOrDefaultAsync();

        // Nota personal del usuario autenticado
        var personalNota = await _db.ClienteNotas
            .AsNoTracking()
            .Where(n => n.ClienteId == clienteId && n.OwnerUserId == userId)
            .Select(n => new { n.Nota, n.UpdatedAt, n.UpdatedBy })
            .FirstOrDefaultAsync();

        // Resolver nombre de quien actualizó (si existe)
        string? globalUpdatedByName = null;
        string? personalUpdatedByName = null;

        if (globalNota?.UpdatedBy != null)
        {
            globalUpdatedByName = await _db.Users
                .Where(u => u.Id == globalNota.UpdatedBy)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync();
        }

        if (personalNota?.UpdatedBy != null)
        {
            personalUpdatedByName = await _db.Users
                .Where(u => u.Id == personalNota.UpdatedBy)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync();
        }

        var response = new ClienteNotasResponseDto
        {
            ClienteId = clienteId,
            Global = globalNota != null
                ? new ClienteNotaItemDto
                {
                    Text = globalNota.Nota,
                    UpdatedAt = globalNota.UpdatedAt,
                    UpdatedByName = globalUpdatedByName
                }
                : null,
            Personal = personalNota != null
                ? new ClienteNotaItemDto
                {
                    Text = personalNota.Nota,
                    UpdatedAt = personalNota.UpdatedAt,
                    UpdatedByName = personalUpdatedByName
                }
                : null
        };

        return Ok(response);
    }

    /// <summary>Upsert nota global. Solo EDITOR/ADMIN.</summary>
    [HttpPut("global")]
    public async Task<ActionResult<ClienteNotaItemDto>> SaveGlobal(int clienteId, [FromBody] ClienteNotaUpdateDto dto)
    {
        var userId = GetUserId();
        var userRole = GetUserRole();

        if (userRole != "ADMIN" && userRole != "EDITOR")
        {
            _logger.LogWarning("SaveGlobalNota: acceso denegado - userId={UserId}, role={Role}", userId, userRole);
            return Forbid();
        }

        _logger.LogInformation("SaveGlobalNota: clienteId={ClienteId}, userId={UserId}, role={Role}",
            clienteId, userId, userRole);

        var clienteExists = await _db.Clientes.AnyAsync(c => c.Id == clienteId);
        if (!clienteExists)
            return NotFound(new { message = $"Cliente {clienteId} no encontrado." });

        var nota = await _db.ClienteNotas
            .FirstOrDefaultAsync(n => n.ClienteId == clienteId && n.OwnerUserId == null);

        var now = DateTime.UtcNow;
        var text = dto.Text?.Trim() ?? string.Empty;

        if (nota == null)
        {
            nota = new ClienteNota
            {
                Id = Guid.NewGuid(),
                ClienteId = clienteId,
                OwnerUserId = null,
                Nota = text,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = userId,
                UpdatedBy = userId
            };
            _db.ClienteNotas.Add(nota);
        }
        else
        {
            nota.Nota = text;
            nota.UpdatedAt = now;
            nota.UpdatedBy = userId;
        }

        await _db.SaveChangesAsync();

        var updatedByName = await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync();

        _logger.LogInformation("SaveGlobalNota: guardada para clienteId={ClienteId}", clienteId);

        return Ok(new ClienteNotaItemDto
        {
            Text = nota.Nota,
            UpdatedAt = nota.UpdatedAt,
            UpdatedByName = updatedByName
        });
    }

    /// <summary>Upsert nota personal del usuario autenticado. Todos los roles.</summary>
    [HttpPut("personal")]
    public async Task<ActionResult<ClienteNotaItemDto>> SavePersonal(int clienteId, [FromBody] ClienteNotaUpdateDto dto)
    {
        var userId = GetUserId();

        _logger.LogInformation("SavePersonalNota: clienteId={ClienteId}, userId={UserId}", clienteId, userId);

        var clienteExists = await _db.Clientes.AnyAsync(c => c.Id == clienteId);
        if (!clienteExists)
            return NotFound(new { message = $"Cliente {clienteId} no encontrado." });

        var nota = await _db.ClienteNotas
            .FirstOrDefaultAsync(n => n.ClienteId == clienteId && n.OwnerUserId == userId);

        var now = DateTime.UtcNow;
        var text = dto.Text?.Trim() ?? string.Empty;

        if (nota == null)
        {
            nota = new ClienteNota
            {
                Id = Guid.NewGuid(),
                ClienteId = clienteId,
                OwnerUserId = userId,
                Nota = text,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = userId,
                UpdatedBy = userId
            };
            _db.ClienteNotas.Add(nota);
        }
        else
        {
            nota.Nota = text;
            nota.UpdatedAt = now;
            nota.UpdatedBy = userId;
        }

        await _db.SaveChangesAsync();

        var updatedByName = await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync();

        _logger.LogInformation("SavePersonalNota: guardada para clienteId={ClienteId}, userId={UserId}", clienteId, userId);

        return Ok(new ClienteNotaItemDto
        {
            Text = nota.Nota,
            UpdatedAt = nota.UpdatedAt,
            UpdatedByName = updatedByName
        });
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private string GetUserRole() =>
        User.FindFirstValue(ClaimTypes.Role) ?? "USER";
}
