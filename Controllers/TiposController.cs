using GestionTime.Api.Contracts.Catalog;
using GestionTime.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionTime.Api.Controllers;

[ApiController]
[Route("api/v1/tipos")]
[Authorize]
public sealed class TiposController : ControllerBase
{
    private readonly TipoService _tipoService;

    public TiposController(TipoService tipoService)
    {
        _tipoService = tipoService;
    }

    /// <summary>
    /// Lista todos los tipos ordenados por nombre
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<TipoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List()
    {
        var tipos = await _tipoService.ListAsync();
        return Ok(tipos);
    }

    /// <summary>
    /// Obtiene un tipo por ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TipoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id)
    {
        var tipo = await _tipoService.GetByIdAsync(id);
        if (tipo == null)
            return NotFound(new { message = "Tipo no encontrado" });

        return Ok(tipo);
    }

    /// <summary>
    /// Crea un nuevo tipo
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TipoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] TipoCreateRequest? request)
    {
        // Debug logging detallado
        if (request == null)
        {
            return BadRequest(new { message = "El cuerpo de la petición no puede estar vacío" });
        }

        // Log para debug
        Console.WriteLine($"[DEBUG] TipoCreateRequest - Nombre: '{request.Nombre}', Descripcion: '{request.Descripcion}'");

        var (success, error, result) = await _tipoService.CreateAsync(request);

        if (!success)
        {
            Console.WriteLine($"[DEBUG] Create failed - Error: {error}");
            // Si es error de duplicado, devolver 409
            if (error?.Contains("Ya existe") == true)
                return Conflict(new { message = error });

            return BadRequest(new { message = error });
        }

        return CreatedAtAction(nameof(Get), new { id = result!.Id }, result);
    }

    /// <summary>
    /// Actualiza un tipo existente
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(TipoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, [FromBody] TipoUpdateRequest request)
    {
        // Debug logging
        if (request == null)
        {
            return BadRequest(new { message = "El cuerpo de la petición no puede estar vacío" });
        }

        var (success, error, result) = await _tipoService.UpdateAsync(id, request);

        if (!success)
        {
            if (error == "Tipo no encontrado")
                return NotFound(new { message = error });

            // Si es error de duplicado, devolver 409
            if (error?.Contains("Ya existe") == true)
                return Conflict(new { message = error });

            return BadRequest(new { message = error });
        }

        return Ok(result);
    }

    /// <summary>
    /// Elimina un tipo
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, error) = await _tipoService.DeleteAsync(id);

        if (!success)
        {
            if (error == "Tipo no encontrado")
                return NotFound(new { message = error });

            // Si está en uso, devolver 409
            return Conflict(new { message = error });
        }

        return NoContent();
    }
}
