using GestionTime.Api.Contracts.Catalog;
using GestionTime.Domain.Work;
using GestionTime.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionTime.Api.Controllers;

/// <summary>
/// CRUD completo para clientes - Ruta: /api/v1/clientes
/// </summary>
[ApiController]
[Route("api/v1/clientes")]
[Authorize]
[Produces("application/json")]
[Tags("Clientes")]
public class ClientesController : ControllerBase
{
    private readonly GestionTimeDbContext _db;
    private readonly ILogger<ClientesController> _logger;

    public ClientesController(GestionTimeDbContext db, ILogger<ClientesController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Obtener lista de clientes con filtros y paginación
    /// </summary>
    /// <param name="q">Búsqueda de texto en nombre, nombre comercial y provincia</param>
    /// <param name="id_puntoop">Filtrar por id_puntoop exacto</param>
    /// <param name="local_num">Filtrar por local_num exacto</param>
    /// <param name="provincia">Filtrar por provincia exacta</param>
    /// <param name="hasNota">Filtrar por presencia de nota (true: con nota, false: sin nota)</param>
    /// <param name="page">Número de página (por defecto 1)</param>
    /// <param name="size">Tamaño de página (por defecto 50, máximo 100)</param>
    /// <returns>Lista paginada de clientes</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ClientePagedResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<ClientePagedResult>> GetClientes(
        [FromQuery] string? q,
        [FromQuery] int? id_puntoop,
        [FromQuery] int? local_num,
        [FromQuery] string? provincia,
        [FromQuery] bool? hasNota,
        [FromQuery] int page = 1,
        [FromQuery] int size = 50)
    {
        page = Math.Max(1, page);
        size = Math.Clamp(size, 1, 100);

        _logger.LogDebug(
            "GetClientes: q={Q}, id_puntoop={IdPuntoop}, local_num={LocalNum}, provincia={Provincia}, hasNota={HasNota}, page={Page}, size={Size}",
            q, id_puntoop, local_num, provincia, hasNota, page, size);

        var query = _db.Clientes.AsNoTracking();

        // Filtro de búsqueda de texto
        if (!string.IsNullOrWhiteSpace(q))
        {
            var searchTerm = q.Trim();
            query = query.Where(c =>
                (c.Nombre != null && EF.Functions.ILike(c.Nombre, $"%{searchTerm}%")) ||
                (c.NombreComercial != null && EF.Functions.ILike(c.NombreComercial, $"%{searchTerm}%")) ||
                (c.Provincia != null && EF.Functions.ILike(c.Provincia, $"%{searchTerm}%")));
        }

        // Filtros exactos
        if (id_puntoop.HasValue)
        {
            query = query.Where(c => c.IdPuntoop == id_puntoop.Value);
        }

        if (local_num.HasValue)
        {
            query = query.Where(c => c.LocalNum == local_num.Value);
        }

        if (!string.IsNullOrWhiteSpace(provincia))
        {
            var provinciaTrim = provincia.Trim();
            query = query.Where(c => c.Provincia == provinciaTrim);
        }

        // Filtro de presencia de nota
        if (hasNota.HasValue)
        {
            if (hasNota.Value)
            {
                query = query.Where(c => c.Nota != null && c.Nota != "");
            }
            else
            {
                query = query.Where(c => c.Nota == null || c.Nota == "");
            }
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(c => c.Id)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(c => new ClienteDto
            {
                Id = c.Id,
                Nombre = c.Nombre,
                IdPuntoop = c.IdPuntoop,
                LocalNum = c.LocalNum,
                NombreComercial = c.NombreComercial,
                Provincia = c.Provincia,
                DataUpdate = c.DataUpdate,
                DataHtml = c.DataHtml,
                Nota = c.Nota
            })
            .ToListAsync();

        _logger.LogInformation("GetClientes: {Count} clientes encontrados de {Total} total", items.Count, totalCount);

        return Ok(new ClientePagedResult
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = size
        });
    }

    /// <summary>
    /// Obtener un cliente por su ID
    /// </summary>
    /// <param name="id">ID del cliente</param>
    /// <returns>Cliente encontrado</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClienteDto>> GetCliente(int id)
    {
        _logger.LogDebug("GetCliente: id={Id}", id);

        var cliente = await _db.Clientes
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new ClienteDto
            {
                Id = c.Id,
                Nombre = c.Nombre,
                IdPuntoop = c.IdPuntoop,
                LocalNum = c.LocalNum,
                NombreComercial = c.NombreComercial,
                Provincia = c.Provincia,
                DataUpdate = c.DataUpdate,
                DataHtml = c.DataHtml,
                Nota = c.Nota
            })
            .FirstOrDefaultAsync();

        if (cliente == null)
        {
            _logger.LogWarning("GetCliente: cliente con id={Id} no encontrado", id);
            return NotFound(new { message = $"Cliente con id {id} no encontrado" });
        }

        _logger.LogDebug("GetCliente: cliente id={Id} encontrado", id);
        return Ok(cliente);
    }

    /// <summary>
    /// Crear un nuevo cliente
    /// </summary>
    /// <param name="dto">Datos del cliente a crear</param>
    /// <returns>Cliente creado con su ID asignado</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ClienteDto>> CreateCliente([FromBody] ClienteCreateDto dto)
    {
        _logger.LogInformation("CreateCliente: Creando cliente con nombre={Nombre}", dto.Nombre);

        var cliente = new Cliente
        {
            Nombre = dto.Nombre?.Trim(),
            IdPuntoop = dto.IdPuntoop,
            LocalNum = dto.LocalNum,
            NombreComercial = string.IsNullOrWhiteSpace(dto.NombreComercial) ? null : dto.NombreComercial.Trim(),
            Provincia = string.IsNullOrWhiteSpace(dto.Provincia) ? null : dto.Provincia.Trim(),
            DataUpdate = dto.DataUpdate ?? DateTime.UtcNow,
            DataHtml = dto.DataHtml,
            Nota = string.IsNullOrWhiteSpace(dto.Nota) ? null : dto.Nota.Trim()
        };

        _db.Clientes.Add(cliente);
        await _db.SaveChangesAsync();

        _logger.LogInformation("CreateCliente: Cliente creado con id={Id}, nombre={Nombre}", cliente.Id, cliente.Nombre);

        var result = new ClienteDto
        {
            Id = cliente.Id,
            Nombre = cliente.Nombre,
            IdPuntoop = cliente.IdPuntoop,
            LocalNum = cliente.LocalNum,
            NombreComercial = cliente.NombreComercial,
            Provincia = cliente.Provincia,
            DataUpdate = cliente.DataUpdate,
            DataHtml = cliente.DataHtml,
            Nota = cliente.Nota
        };

        return CreatedAtAction(nameof(GetCliente), new { id = cliente.Id }, result);
    }

    /// <summary>
    /// Actualizar un cliente existente (reemplazo completo)
    /// </summary>
    /// <param name="id">ID del cliente a actualizar</param>
    /// <param name="dto">Nuevos datos del cliente</param>
    /// <returns>Cliente actualizado</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ClienteDto>> UpdateCliente(int id, [FromBody] ClienteUpdateDto dto)
    {
        _logger.LogInformation("UpdateCliente: Actualizando cliente id={Id}", id);

        var cliente = await _db.Clientes.FindAsync(id);

        if (cliente == null)
        {
            _logger.LogWarning("UpdateCliente: cliente con id={Id} no encontrado", id);
            return NotFound(new { message = $"Cliente con id {id} no encontrado" });
        }

        cliente.Nombre = dto.Nombre?.Trim();
        cliente.IdPuntoop = dto.IdPuntoop;
        cliente.LocalNum = dto.LocalNum;
        cliente.NombreComercial = string.IsNullOrWhiteSpace(dto.NombreComercial) ? null : dto.NombreComercial.Trim();
        cliente.Provincia = string.IsNullOrWhiteSpace(dto.Provincia) ? null : dto.Provincia.Trim();
        cliente.DataUpdate = dto.DataUpdate ?? DateTime.UtcNow;
        cliente.DataHtml = dto.DataHtml;
        cliente.Nota = string.IsNullOrWhiteSpace(dto.Nota) ? null : dto.Nota.Trim();

        await _db.SaveChangesAsync();

        _logger.LogInformation("UpdateCliente: Cliente id={Id} actualizado correctamente", id);

        var result = new ClienteDto
        {
            Id = cliente.Id,
            Nombre = cliente.Nombre,
            IdPuntoop = cliente.IdPuntoop,
            LocalNum = cliente.LocalNum,
            NombreComercial = cliente.NombreComercial,
            Provincia = cliente.Provincia,
            DataUpdate = cliente.DataUpdate,
            DataHtml = cliente.DataHtml,
            Nota = cliente.Nota
        };

        return Ok(result);
    }

    /// <summary>
    /// Actualizar solo la nota de un cliente
    /// </summary>
    /// <param name="id">ID del cliente</param>
    /// <param name="dto">Nueva nota</param>
    /// <returns>Cliente con la nota actualizada</returns>
    [HttpPatch("{id:int}/nota")]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClienteDto>> UpdateClienteNota(int id, [FromBody] ClienteUpdateNotaDto dto)
    {
        _logger.LogInformation("UpdateClienteNota: Actualizando nota de cliente id={Id}", id);

        var cliente = await _db.Clientes.FindAsync(id);

        if (cliente == null)
        {
            _logger.LogWarning("UpdateClienteNota: cliente con id={Id} no encontrado", id);
            return NotFound(new { message = $"Cliente con id {id} no encontrado" });
        }

        cliente.Nota = string.IsNullOrWhiteSpace(dto.Nota) ? null : dto.Nota.Trim();

        await _db.SaveChangesAsync();

        _logger.LogInformation("UpdateClienteNota: Nota de cliente id={Id} actualizada correctamente", id);

        var result = new ClienteDto
        {
            Id = cliente.Id,
            Nombre = cliente.Nombre,
            IdPuntoop = cliente.IdPuntoop,
            LocalNum = cliente.LocalNum,
            NombreComercial = cliente.NombreComercial,
            Provincia = cliente.Provincia,
            DataUpdate = cliente.DataUpdate,
            DataHtml = cliente.DataHtml,
            Nota = cliente.Nota
        };

        return Ok(result);
    }

    /// <summary>
    /// Eliminar un cliente
    /// </summary>
    /// <param name="id">ID del cliente a eliminar</param>
    /// <returns>204 No Content si se eliminó correctamente</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCliente(int id)
    {
        _logger.LogInformation("DeleteCliente: Intentando eliminar cliente id={Id}", id);

        var cliente = await _db.Clientes.FindAsync(id);

        if (cliente == null)
        {
            _logger.LogWarning("DeleteCliente: cliente con id={Id} no encontrado", id);
            return NotFound(new { message = $"Cliente con id {id} no encontrado" });
        }

        try
        {
            _db.Clientes.Remove(cliente);
            await _db.SaveChangesAsync();

            _logger.LogInformation("DeleteCliente: Cliente id={Id} eliminado correctamente", id);
            return NoContent();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "DeleteCliente: Error al eliminar cliente id={Id}. Posible violación de clave foránea", id);
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "No se puede eliminar el cliente",
                Detail = "El cliente está referenciado por otros registros en el sistema y no puede ser eliminado. Primero debe eliminar las referencias asociadas.",
                Instance = $"/api/v1/clientes/{id}"
            });
        }
    }
}
