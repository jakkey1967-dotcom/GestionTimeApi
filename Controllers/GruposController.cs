using GestionTime.Api.Contracts.Catalog;
using GestionTime.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionTime.Api.Controllers;

[ApiController]
[Route("api/v1/grupos")]
[Authorize]
public sealed class GruposController : ControllerBase
{
    private readonly GrupoService _grupoService;

    public GruposController(GrupoService grupoService)
    {
        _grupoService = grupoService;
    }

    /// <summary>
    /// Lista todos los grupos ordenados por nombre
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<GrupoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List()
    {
        var grupos = await _grupoService.ListAsync();
        return Ok(grupos);
    }

    /// <summary>
    /// Obtiene un grupo por ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(GrupoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id)
    {
        var grupo = await _grupoService.GetByIdAsync(id);
        if (grupo == null)
            return NotFound(new { message = "Grupo no encontrado" });

        return Ok(grupo);
    }

    /// <summary>
    /// Crea un nuevo grupo
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(GrupoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] GrupoCreateRequest request)
    {
        var (success, error, result) = await _grupoService.CreateAsync(request);

        if (!success)
        {
            // Si es error de duplicado, devolver 409
            if (error?.Contains("Ya existe") == true)
                return Conflict(new { message = error });

            return BadRequest(new { message = error });
        }

        return CreatedAtAction(nameof(Get), new { id = result!.Id }, result);
    }

    /// <summary>
    /// Actualiza un grupo existente
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(GrupoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, [FromBody] GrupoUpdateRequest? request)
    {
        // Debug logging detallado
        if (request == null)
        {
            return BadRequest(new { message = "El cuerpo de la petición no puede estar vacío" });
        }

        // Log para debug
        Console.WriteLine($"[DEBUG] GrupoUpdateRequest - ID: {id}, Nombre: '{request.Nombre}', Descripcion: '{request.Descripcion}'");

        var (success, error, result) = await _grupoService.UpdateAsync(id, request);

        if (!success)
        {
            Console.WriteLine($"[DEBUG] Update failed - Error: {error}");
            if (error == "Grupo no encontrado")
                return NotFound(new { message = error });

            // Si es error de duplicado, devolver 409
            if (error?.Contains("Ya existe") == true)
                return Conflict(new { message = error });

            return BadRequest(new { message = error });
        }

        return Ok(result);
    }

    /// <summary>
    /// Elimina un grupo
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, error) = await _grupoService.DeleteAsync(id);

        if (!success)
        {
            if (error == "Grupo no encontrado")
                return NotFound(new { message = error });

            // Si está en uso, devolver 409
            return Conflict(new { message = error });
        }

        return NoContent();
    }
}
