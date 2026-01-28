using GestionTime.Api.Contracts.Work;
using GestionTime.Domain.Work;
using GestionTime.Domain.Freshdesk;
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
    // GET /api/v1/partes?fechaInicio=2025-01-01&fechaFin=2025-01-31
    // GET /api/v1/partes?created_from=...&created_to=...
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] DateTime? fecha,
        [FromQuery] DateTime? fechaInicio,
        [FromQuery] DateTime? fechaFin,
        [FromQuery] DateTime? created_from,
        [FromQuery] DateTime? created_to,
        [FromQuery] string? q,
        [FromQuery] int? estado)
    {
        var userId = GetUserId();
        _logger.LogDebug("Listando partes para usuario {UserId}. Fecha: {Fecha}, FechaInicio: {FechaInicio}, FechaFin: {FechaFin}, Query: {Query}, Estado: {Estado}", 
            userId, fecha, fechaInicio, fechaFin, q, estado);

        var baseQuery = db.PartesDeTrabajo
            .AsNoTracking()
            .Where(p => p.IdUsuario == userId);

        // Filtro por fecha única
        if (fecha.HasValue)
        {
            var d = DateTime.SpecifyKind(fecha.Value.Date, DateTimeKind.Utc);
            baseQuery = baseQuery.Where(p => p.FechaTrabajo == d);
        }

        // Filtro por rango de fechas
        if (fechaInicio.HasValue)
        {
            var fechaInicioDate = DateTime.SpecifyKind(fechaInicio.Value.Date, DateTimeKind.Utc);
            baseQuery = baseQuery.Where(p => p.FechaTrabajo >= fechaInicioDate);
            _logger.LogDebug("Aplicando filtro fechaInicio >= {FechaInicio}", fechaInicioDate);
        }

        if (fechaFin.HasValue)
        {
            var fechaFinDate = DateTime.SpecifyKind(fechaFin.Value.Date, DateTimeKind.Utc);
            baseQuery = baseQuery.Where(p => p.FechaTrabajo <= fechaFinDate);
            _logger.LogDebug("Aplicando filtro fechaFin <= {FechaFin}", fechaFinDate);
        }

        // Filtros por fecha de creación (convertir a UTC)
        if (created_from.HasValue)
        {
            var createdFromUtc = created_from.Value.Kind == DateTimeKind.Unspecified 
                ? DateTime.SpecifyKind(created_from.Value, DateTimeKind.Utc) 
                : created_from.Value.ToUniversalTime();
            baseQuery = baseQuery.Where(p => p.CreatedAt >= createdFromUtc);
        }

        if (created_to.HasValue)
        {
            var createdToUtc = created_to.Value.Kind == DateTimeKind.Unspecified 
                ? DateTime.SpecifyKind(created_to.Value, DateTimeKind.Utc) 
                : created_to.Value.ToUniversalTime();
            baseQuery = baseQuery.Where(p => p.CreatedAt < createdToUtc);
        }

        // Filtro por búsqueda de texto
        if (!string.IsNullOrWhiteSpace(q))
            baseQuery = baseQuery.Where(p => p.Accion.Contains(q) || (p.Ticket != null && p.Ticket.Contains(q)));

        // Filtro por estado
        if (estado.HasValue)
            baseQuery = baseQuery.Where(p => p.Estado == estado.Value);

        List<dynamic> items;
        
        try
        {
            var rows = await (
                from p in baseQuery
                join c in db.Clientes on p.IdCliente equals c.Id
                join u in db.Users on p.IdUsuario equals u.Id

                join g0 in db.Grupos on p.IdGrupo equals (int?)g0.IdGrupo into gj
                from g in gj.DefaultIfEmpty()

                join t0 in db.Tipos on p.IdTipo equals (int?)t0.IdTipo into tj
                from t in tj.DefaultIfEmpty()

                // ✅ Ordenar de más nuevo a más antiguo (fecha descendente, luego hora descendente)
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
                    Tecnico = u.FullName,
                    Tags = p.ParteTags.Select(pt => pt.TagName).ToList()
                }
            ).ToListAsync();

            items = rows.Select(x => new
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
                updated_at = x.UpdatedAt,
                tags = x.Tags.OrderBy(t => t).ToArray()
            } as dynamic).ToList();
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P01") // UndefinedTable
        {
            _logger.LogWarning("Tags deshabilitadas en GET: tabla pss_dvnx.parte_tags no existe. Devolviendo partes sin tags");
            
            // Query alternativa sin tags
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

            items = rows.Select(x => new
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
                updated_at = x.UpdatedAt,
                tags = Array.Empty<string>() // ← Sin tags
            } as dynamic).ToList();
        }

        _logger.LogInformation("Usuario {UserId} listó {Count} partes de trabajo", userId, items.Count);

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

        // ✅ Asegurar que la fecha tenga Kind=Utc
        var fechaTrabajoUtc = req.fecha_trabajo.Kind == DateTimeKind.Unspecified 
            ? DateTime.SpecifyKind(req.fecha_trabajo.Date, DateTimeKind.Utc) 
            : req.fecha_trabajo.Date.ToUniversalTime();

        var entity = new ParteDeTrabajo
        {
            FechaTrabajo = fechaTrabajoUtc,
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
        await db.SaveChangesAsync(); // Guardar para obtener el ID

        // Procesar tags si se enviaron
        if (req.tags != null)
        {
            await SyncParteTagsAsync(entity.Id, req.tags);
        }

        _logger.LogInformation("Parte de trabajo creado: {ParteId} por usuario {UserId} con {TagCount} tags", 
            entity.Id, userId, req.tags?.Length ?? 0);

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

        // ✅ Asegurar que la fecha tenga Kind=Utc
        var fechaTrabajoUtc = req.fecha_trabajo.Kind == DateTimeKind.Unspecified 
            ? DateTime.SpecifyKind(req.fecha_trabajo.Date, DateTimeKind.Utc) 
            : req.fecha_trabajo.Date.ToUniversalTime();

        entity.FechaTrabajo = fechaTrabajoUtc;
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
        
        // Procesar tags si se enviaron (null = sin cambios, [] = vaciar)
        if (req.tags != null)
        {
            await SyncParteTagsAsync(id, req.tags);
            _logger.LogInformation("Tags actualizadas para parte {ParteId}: {TagCount} tags", id, req.tags.Length);
        }

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
    
    /// <summary>
    /// PUT /api/v1/partes/{id}/tags - Reemplazar tags de un parte
    /// </summary>
    [HttpPut("{id:long}/tags")]
    public async Task<IActionResult> UpdateTags(long id, [FromBody] UpdateTagsRequest req)
    {
        var userId = GetUserId();
        _logger.LogInformation("Usuario {UserId} actualizando tags del parte {ParteId}", userId, id);
        
        if (req == null || req.tags == null)
        {
            return BadRequest(new { message = "Body inválido. Se requiere { tags: [...] }" });
        }
        
        var entity = await db.PartesDeTrabajo
            .Include(p => p.ParteTags)
            .SingleOrDefaultAsync(x => x.Id == id && x.IdUsuario == userId);
        
        if (entity is null)
        {
            _logger.LogWarning("Parte {ParteId} no encontrado o no pertenece al usuario {UserId}", id, userId);
            return NotFound(new { message = "Parte no encontrado" });
        }
        
        await SyncParteTagsAsync(id, req.tags);
        
        _logger.LogInformation("Tags actualizadas para parte {ParteId}: {TagCount} tags", id, req.tags.Length);
        
        // Recargar con tags actualizadas
        var updated = await db.PartesDeTrabajo
            .Include(p => p.ParteTags)
            .FirstAsync(p => p.Id == id);
        
        return Ok(new
        {
            message = "ok",
            parte_id = id,
            tags = updated.ParteTags.Select(pt => pt.TagName).OrderBy(t => t).ToArray()
        });
    }
    
    /// <summary>
    /// Sincroniza tags de un parte (replace completo)
    /// ROBUSTO: Si las tablas no existen, solo loguea warning y continúa sin romper
    /// </summary>
    private async Task SyncParteTagsAsync(long parteId, string[] tags)
    {
        try
        {
            var now = DateTime.UtcNow; // FreshdeskTag usa DateTime
            
            // Normalizar y validar tags
            var normalizedTags = tags
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim().ToLowerInvariant())
                .Where(t => t.Length <= 100) // Límite de freshdesk_tags
                .Distinct()
                .Take(20) // Límite máximo de 20 tags por parte
                .ToList();
            
            // Si no hay tags, limpiar todo
            if (!normalizedTags.Any())
            {
                var existingTags = await db.ParteTags
                    .Where(pt => pt.ParteId == parteId)
                    .ToListAsync();
                
                if (existingTags.Any())
                {
                    db.ParteTags.RemoveRange(existingTags);
                    await db.SaveChangesAsync();
                    _logger.LogDebug("Todas las tags eliminadas del parte {ParteId}", parteId);
                }
                return;
            }
            
            // Obtener tags actuales del parte
            var currentTags = await db.ParteTags
                .Where(pt => pt.ParteId == parteId)
                .Select(pt => pt.TagName)
                .ToListAsync();
            
            var currentSet = currentTags.ToHashSet();
            var newSet = normalizedTags.ToHashSet();
            
            // Tags a eliminar
            var tagsToRemove = currentSet.Except(newSet).ToList();
            if (tagsToRemove.Any())
            {
                var toRemove = db.ParteTags.Where(pt => pt.ParteId == parteId && tagsToRemove.Contains(pt.TagName));
                db.ParteTags.RemoveRange(toRemove);
            }
            
            // Tags a agregar
            var tagsToAdd = newSet.Except(currentSet).ToList();
            foreach (var tagName in tagsToAdd)
            {
                // Upsert en catálogo de tags (freshdesk_tags)
                var tag = await db.FreshdeskTags.FindAsync(tagName);
                if (tag == null)
                {
                    db.FreshdeskTags.Add(new FreshdeskTag
                    {
                        Name = tagName,
                        Source = "local", // Origen: partes de trabajo
                        LastSeenAt = now
                    });
                }
                else
                {
                    tag.LastSeenAt = now;
                    // Si era de freshdesk, ahora es 'both'
                    if (tag.Source == "freshdesk")
                    {
                        tag.Source = "both";
                    }
                }
                
                // Crear relación parte-tag
                db.ParteTags.Add(new ParteTag
                {
                    ParteId = parteId,
                    TagName = tagName
                });
            }
            
            // Actualizar last_seen_at de tags que ya estaban y siguen
            var tagsToUpdate = currentSet.Intersect(newSet).ToList();
            if (tagsToUpdate.Any())
            {
                var tagsEntities = await db.FreshdeskTags.Where(t => tagsToUpdate.Contains(t.Name)).ToListAsync();
                foreach (var tag in tagsEntities)
                {
                    tag.LastSeenAt = now;
                }
            }
            
            await db.SaveChangesAsync();
            
            _logger.LogDebug("Tags sync para parte {ParteId}: +{Add} -{Remove} ={Keep}", 
                parteId, tagsToAdd.Count, tagsToRemove.Count, tagsToUpdate.Count);
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P01") // UndefinedTable
        {
            _logger.LogWarning("Tags deshabilitadas: tabla pss_dvnx.parte_tags o pss_dvnx.freshdesk_tags no existe. " +
                "Los tags no serán sincronizados. Error: {Message}", ex.Message);
            // NO lanzar excepción - continuar sin romper el endpoint
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al sincronizar tags para parte {ParteId}. Tags no se actualizaron pero el parte se guardó correctamente", parteId);
            // NO lanzar excepción - el parte ya se guardó, solo falló el sync de tags
        }
    }
}

public sealed record UpdateTagsRequest(string[] tags);


