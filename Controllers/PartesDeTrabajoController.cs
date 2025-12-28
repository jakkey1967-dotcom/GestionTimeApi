using GestionTime.Api.Contracts.Work;
using GestionTime.Domain.Work;
using GestionTime.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GestionTime.Api.Controllers;

[ApiController]
[Route("api/v1/partes")]
[Authorize]
public class PartesDeTrabajoController : ControllerBase
{
    private readonly GestionTimeDbContext db;
    private readonly ILogger<PartesDeTrabajoController> _logger;

    public PartesDeTrabajoController(GestionTimeDbContext db, ILogger<PartesDeTrabajoController> logger)
    {
        this.db = db;
        _logger = logger;
    }

    // GET /api/v1/partes?fecha=2025-12-15
    // GET /api/v1/partes?created_from=...&created_to=...
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] DateTime? fecha,
        [FromQuery] DateTime? created_from,
        [FromQuery] DateTime? created_to,
        [FromQuery] string? q,
        [FromQuery] int? estado)
    {
        var userId = GetUserId();
        _logger.LogDebug("Listando partes para usuario {UserId}. Fecha: {Fecha}, Query: {Query}, Estado: {Estado}", userId, fecha, q, estado);

        var baseQuery = db.PartesDeTrabajo
            .AsNoTracking()
            .Where(p => p.IdUsuario == userId);

        if (fecha.HasValue)
        {
            var d = fecha.Value.Date;
            baseQuery = baseQuery.Where(p => p.FechaTrabajo == d);
        }

        if (created_from.HasValue)
            baseQuery = baseQuery.Where(p => p.CreatedAt >= created_from.Value);

        if (created_to.HasValue)
            baseQuery = baseQuery.Where(p => p.CreatedAt < created_to.Value);

        if (!string.IsNullOrWhiteSpace(q))
            baseQuery = baseQuery.Where(p => p.Accion.Contains(q) || (p.Ticket != null && p.Ticket.Contains(q)));

        if (estado.HasValue)
            baseQuery = baseQuery.Where(p => p.Estado == estado.Value);

        var rows = await (
            from p in baseQuery
            join c in db.Clientes on p.IdCliente equals c.Id
            join u in db.Users on p.IdUsuario equals u.Id

            join g0 in db.Grupos on p.IdGrupo equals (int?)g0.IdGrupo into gj
            from g in gj.DefaultIfEmpty()

            join t0 in db.Tipos on p.IdTipo equals (int?)t0.IdTipo into tj
            from t in tj.DefaultIfEmpty()

            orderby p.FechaTrabajo descending, p.HoraInicio descending
            select new
            {
                p.Id,
                p.FechaTrabajo,
                p.HoraInicio,
                p.HoraFin,
                p.Ticket,
                p.Accion,
                p.Tienda,
                p.Estado,
                p.CreatedAt,
                p.UpdatedAt,
                p.IdCliente,
                p.IdGrupo,
                p.IdTipo,

                Cliente = (c.NombreComercial ?? c.Nombre),
                Grupo = g != null ? g.Nombre : null,
                Tipo = t != null ? t.Nombre : null,
                Tecnico = u.FullName
            }
        ).ToListAsync();

        var items = rows.Select(x => new
        {
            id = x.Id,
            fecha = x.FechaTrabajo,
            cliente = x.Cliente,
            id_cliente = x.IdCliente,
            tienda = x.Tienda,
            accion = x.Accion,
            horainicio = x.HoraInicio.ToString("HH:mm"),
            horafin = x.HoraFin.ToString("HH:mm"),
            duracion_min = (int)(x.HoraFin.ToTimeSpan() - x.HoraInicio.ToTimeSpan()).TotalMinutes,
            ticket = x.Ticket,
            grupo = x.Grupo,
            id_grupo = x.IdGrupo,
            tipo = x.Tipo,
            id_tipo = x.IdTipo,
            tecnico = x.Tecnico,
            estado = x.Estado,
            estado_nombre = EstadoParte.ObtenerNombre(x.Estado),
            created_at = x.CreatedAt,
            updated_at = x.UpdatedAt
        }).ToList();

        _logger.LogInformation("Usuario {UserId} listo {Count} partes de trabajo", userId, items.Count);

        return Ok(items);
    }

    // GET /api/v1/partes/estados - Devuelve los estados disponibles
    [HttpGet("estados")]
    public IActionResult GetEstados()
    {
        var estados = EstadoParte.TodosLosEstados.Select(e => new
        {
            id = e,
            nombre = EstadoParte.ObtenerNombre(e)
        });

        return Ok(estados);
    }

    // POST /api/v1/partes
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateParteRequest req)
    {
        var userId = GetUserId();
        _logger.LogInformation("Usuario {UserId} creando parte de trabajo para fecha {Fecha}", userId, req.fecha_trabajo);

        if (!TryParseTime(req.hora_inicio, out var hi))
        {
            _logger.LogWarning("Creacion fallida: hora_inicio invalida ({HoraInicio})", req.hora_inicio);
            return BadRequest(new { message = "hora_inicio invalida (usa HH:mm)" });
        }

        if (!TryParseTime(req.hora_fin, out var hf))
        {
            _logger.LogWarning("Creacion fallida: hora_fin invalida ({HoraFin})", req.hora_fin);
            return BadRequest(new { message = "hora_fin invalida (usa HH:mm)" });
        }

        if (hf < hi)
        {
            _logger.LogWarning("Creacion fallida: hora_fin ({HoraFin}) menor que hora_inicio ({HoraInicio})", hf, hi);
            return BadRequest(new { message = "hora_fin no puede ser menor que hora_inicio" });
        }

        var entity = new ParteDeTrabajo
        {
            FechaTrabajo = req.fecha_trabajo.Date,
            HoraInicio = hi,
            HoraFin = hf,
            IdCliente = req.id_cliente,
            Tienda = req.tienda,
            IdGrupo = req.id_grupo,
            IdTipo = req.id_tipo,
            Accion = req.accion.Trim(),
            Ticket = req.ticket,
            Estado = EstadoParte.Abierto,
            IdUsuario = userId
        };

        db.PartesDeTrabajo.Add(entity);
        await db.SaveChangesAsync();

        _logger.LogInformation("Parte de trabajo creado: {ParteId} por usuario {UserId}", entity.Id, userId);

        return Ok(new { id = entity.Id });
    }

    // PUT /api/v1/partes/{id}
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateParteRequest? req)
    {
        var userId = GetUserId();
        _logger.LogInformation("Usuario {UserId} actualizando parte {ParteId}", userId, id);

        // Validar que el body no sea null
        if (req is null)
        {
            _logger.LogWarning("Actualizacion fallida: body vacio o invalido");
            return BadRequest(new { message = "Body de la peticion invalido" });
        }

        _logger.LogDebug("Request recibido: fecha={Fecha}, hora_inicio={HoraInicio}, hora_fin={HoraFin}, id_cliente={Cliente}, accion={Accion}, estado={Estado}",
            req.fecha_trabajo, req.hora_inicio, req.hora_fin, req.id_cliente, req.accion, req.estado);

        var entity = await db.PartesDeTrabajo.SingleOrDefaultAsync(x => x.Id == id && x.IdUsuario == userId);
        if (entity is null)
        {
            _logger.LogWarning("Parte {ParteId} no encontrado o no pertenece al usuario {UserId}", id, userId);
            return NotFound();
        }

        // Verificar si el parte puede ser editado
        if (!entity.PuedeEditar)
        {
            _logger.LogWarning("Parte {ParteId} no puede ser editado. Estado actual: {Estado}", id, entity.EstadoNombre);
            return BadRequest(new { message = $"El parte no puede ser editado. Estado actual: {entity.EstadoNombre}" });
        }

        if (string.IsNullOrWhiteSpace(req.hora_inicio))
        {
            _logger.LogWarning("Actualizacion fallida: hora_inicio vacia");
            return BadRequest(new { message = "hora_inicio es requerida" });
        }

        if (!TryParseTime(req.hora_inicio, out var hi))
        {
            _logger.LogWarning("Actualizacion fallida: hora_inicio invalida ({HoraInicio})", req.hora_inicio);
            return BadRequest(new { message = "hora_inicio invalida (usa HH:mm)" });
        }

        if (string.IsNullOrWhiteSpace(req.hora_fin))
        {
            _logger.LogWarning("Actualizacion fallida: hora_fin vacia");
            return BadRequest(new { message = "hora_fin es requerida" });
        }

        if (!TryParseTime(req.hora_fin, out var hf))
        {
            _logger.LogWarning("Actualizacion fallida: hora_fin invalida ({HoraFin})", req.hora_fin);
            return BadRequest(new { message = "hora_fin invalida (usa HH:mm)" });
        }

        if (hf < hi)
        {
            _logger.LogWarning("Actualizacion fallida: hora_fin ({HoraFin}) menor que hora_inicio ({HoraInicio})", hf, hi);
            return BadRequest(new { message = "hora_fin no puede ser menor que hora_inicio" });
        }

        if (string.IsNullOrWhiteSpace(req.accion))
        {
            _logger.LogWarning("Actualizacion fallida: accion vacia");
            return BadRequest(new { message = "accion es requerida" });
        }

        entity.FechaTrabajo = req.fecha_trabajo.Date;
        entity.HoraInicio = hi;
        entity.HoraFin = hf;
        entity.IdCliente = req.id_cliente;
        entity.Tienda = req.tienda;
        entity.IdGrupo = req.id_grupo;
        entity.IdTipo = req.id_tipo;
        entity.Accion = req.accion.Trim();
        entity.Ticket = req.ticket;

        // Si se envia estado, validar y aplicar
        if (req.estado.HasValue)
        {
            if (!EstadoParte.EsValido(req.estado.Value))
            {
                _logger.LogWarning("Actualizacion fallida: estado invalido ({Estado})", req.estado);
                return BadRequest(new { message = "Estado invalido" });
            }
            entity.Estado = req.estado.Value;
        }

        entity.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        _logger.LogInformation("Parte {ParteId} actualizado exitosamente. Estado: {Estado}", id, entity.EstadoNombre);

        return Ok(new { message = "ok" });
    }

    // DELETE /api/v1/partes/{id} - Eliminar parte (solo si es editable)
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var userId = GetUserId();
        _logger.LogInformation("Usuario {UserId} eliminando parte {ParteId}", userId, id);

        var entity = await db.PartesDeTrabajo.SingleOrDefaultAsync(x => x.Id == id && x.IdUsuario == userId);
        if (entity is null)
        {
            _logger.LogWarning("Parte {ParteId} no encontrado o no pertenece al usuario {UserId}", id, userId);
            return NotFound();
        }

        // Verificar si el parte puede ser eliminado (solo estados editables)
        if (!entity.PuedeEditar)
        {
            _logger.LogWarning("Parte {ParteId} no puede ser eliminado. Estado actual: {Estado}", id, entity.EstadoNombre);
            return BadRequest(new { message = $"El parte no puede ser eliminado. Estado actual: {entity.EstadoNombre}" });
        }

        db.PartesDeTrabajo.Remove(entity);
        await db.SaveChangesAsync();

        _logger.LogInformation("Parte {ParteId} eliminado exitosamente", id);

        return Ok(new { message = "ok" });
    }

    // POST /api/v1/partes/{id}/pause
    [HttpPost("{id:long}/pause")]
    public async Task<IActionResult> Pause(long id)
    {
        return await ChangeEstadoInternal(id, EstadoParte.Pausado, "pausing");
    }

    // POST /api/v1/partes/{id}/resume
    [HttpPost("{id:long}/resume")]
    public async Task<IActionResult> Resume(long id)
    {
        var userId = GetUserId();
        var entity = await db.PartesDeTrabajo.SingleOrDefaultAsync(x => x.Id == id && x.IdUsuario == userId);
        
        if (entity is null)
        {
            _logger.LogWarning("Parte {ParteId} no encontrado o no pertenece al usuario {UserId}", id, userId);
            return NotFound();
        }

        if (entity.Estado != EstadoParte.Pausado)
        {
            _logger.LogWarning("Parte {ParteId} no esta pausado. Estado actual: {Estado}", id, entity.EstadoNombre);
            return BadRequest(new { message = $"Solo se puede reanudar un parte pausado. Estado actual: {entity.EstadoNombre}" });
        }

        entity.Estado = EstadoParte.Abierto;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        _logger.LogInformation("Parte {ParteId} reanudado exitosamente", id);
        return Ok(new { message = "ok", estado = entity.Estado, estado_nombre = entity.EstadoNombre });
    }

    // POST /api/v1/partes/{id}/close
    [HttpPost("{id:long}/close")]
    public async Task<IActionResult> Close(long id)
    {
        var userId = GetUserId();
        var entity = await db.PartesDeTrabajo.SingleOrDefaultAsync(x => x.Id == id && x.IdUsuario == userId);
        
        if (entity is null)
        {
            _logger.LogWarning("Parte {ParteId} no encontrado o no pertenece al usuario {UserId}", id, userId);
            return NotFound();
        }

        if (!EstadoParte.PuedeCerrar(entity.Estado))
        {
            _logger.LogWarning("Parte {ParteId} no puede ser cerrado. Estado actual: {Estado}", id, entity.EstadoNombre);
            return BadRequest(new { message = $"El parte no puede ser cerrado. Estado actual: {entity.EstadoNombre}" });
        }

        entity.Estado = EstadoParte.Cerrado;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        _logger.LogInformation("Parte {ParteId} cerrado exitosamente", id);
        return Ok(new { message = "ok", estado = entity.Estado, estado_nombre = entity.EstadoNombre });
    }

    // POST /api/v1/partes/{id}/cancel
    [HttpPost("{id:long}/cancel")]
    public async Task<IActionResult> Cancel(long id)
    {
        var userId = GetUserId();
        var entity = await db.PartesDeTrabajo.SingleOrDefaultAsync(x => x.Id == id && x.IdUsuario == userId);
        
        if (entity is null)
        {
            _logger.LogWarning("Parte {ParteId} no encontrado o no pertenece al usuario {UserId}", id, userId);
            return NotFound();
        }

        if (!EstadoParte.PuedeAnular(entity.Estado))
        {
            _logger.LogWarning("Parte {ParteId} no puede ser anulado. Estado actual: {Estado}", id, entity.EstadoNombre);
            return BadRequest(new { message = $"El parte no puede ser anulado. Estado actual: {entity.EstadoNombre}" });
        }

        entity.Estado = EstadoParte.Anulado;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        _logger.LogInformation("Parte {ParteId} anulado exitosamente", id);
        return Ok(new { message = "ok", estado = entity.Estado, estado_nombre = entity.EstadoNombre });
    }

    // POST /api/v1/partes/{id}/estado - Endpoint generico para cambiar estado
    public sealed record ChangeEstadoRequest(int estado);

    [HttpPost("{id:long}/estado")]
    public async Task<IActionResult> ChangeEstado(long id, [FromBody] ChangeEstadoRequest? req)
    {
        var userId = GetUserId();
        _logger.LogInformation("Usuario {UserId} cambiando estado de parte {ParteId}", userId, id);

        if (req is null)
        {
            _logger.LogWarning("Cambio de estado fallido: body vacio");
            return BadRequest(new { message = "Body de la peticion invalido" });
        }

        if (!EstadoParte.EsValido(req.estado))
        {
            _logger.LogWarning("Cambio de estado fallido: estado invalido ({Estado})", req.estado);
            return BadRequest(new { 
                message = "Estado invalido", 
                estados_validos = EstadoParte.TodosLosEstados.Select(e => new { id = e, nombre = EstadoParte.ObtenerNombre(e) })
            });
        }

        var entity = await db.PartesDeTrabajo.SingleOrDefaultAsync(x => x.Id == id && x.IdUsuario == userId);
        if (entity is null)
        {
            _logger.LogWarning("Parte {ParteId} no encontrado o no pertenece al usuario {UserId}", id, userId);
            return NotFound();
        }

        entity.Estado = req.estado;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        _logger.LogInformation("Parte {ParteId} cambio a estado {Estado} ({EstadoNombre})", id, req.estado, entity.EstadoNombre);

        return Ok(new { message = "ok", estado = entity.Estado, estado_nombre = entity.EstadoNombre });
    }

    private async Task<IActionResult> ChangeEstadoInternal(long id, int newEstado, string action)
    {
        var userId = GetUserId();
        _logger.LogInformation("Usuario {UserId} {Action} parte {ParteId}", userId, action, id);

        var entity = await db.PartesDeTrabajo.SingleOrDefaultAsync(x => x.Id == id && x.IdUsuario == userId);
        if (entity is null)
        {
            _logger.LogWarning("Parte {ParteId} no encontrado o no pertenece al usuario {UserId}", id, userId);
            return NotFound();
        }

        entity.Estado = newEstado;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        _logger.LogInformation("Parte {ParteId} {Action} exitosamente", id, action);

        return Ok(new { message = "ok", estado = entity.Estado, estado_nombre = entity.EstadoNombre });
    }

    private Guid GetUserId()
    {
        var s = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(s, out var id)) return id;
        
        _logger.LogError("No se pudo obtener el UserId del token");
        throw new UnauthorizedAccessException("Missing NameIdentifier");
    }

    private static bool TryParseTime(string s, out TimeOnly t)
        => TimeOnly.TryParseExact(s, "HH:mm", out t);
}

