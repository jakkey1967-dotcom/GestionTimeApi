using GestionTime.Api.Contracts.Catalog;
using GestionTime.Domain.Work;
using GestionTime.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestionTime.Api.Services;

public sealed class GrupoService
{
    private readonly GestionTimeDbContext _db;
    private readonly ILogger<GrupoService> _logger;

    public GrupoService(GestionTimeDbContext db, ILogger<GrupoService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<GrupoDto>> ListAsync()
    {
        var grupos = await _db.Grupos
            .OrderBy(g => g.Nombre)
            .Select(g => new GrupoDto { Id = g.IdGrupo, Nombre = g.Nombre, Descripcion = g.Descripcion })
            .ToListAsync();

        return grupos;
    }

    public async Task<GrupoDto?> GetByIdAsync(int id)
    {
        var grupo = await _db.Grupos.FindAsync(id);
        if (grupo == null) return null;

        return new GrupoDto 
        { 
            Id = grupo.IdGrupo, 
            Nombre = grupo.Nombre, 
            Descripcion = grupo.Descripcion 
        };
    }

    public async Task<(bool success, string? error, GrupoDto? result)> CreateAsync(GrupoCreateRequest request)
    {
        // Validar
        var nombre = request.Nombre?.Trim();
        if (string.IsNullOrEmpty(nombre))
            return (false, "El nombre es requerido", null);

        if (nombre.Length > 120)
            return (false, "El nombre no puede exceder 120 caracteres", null);

        var descripcion = request.Descripcion?.Trim();
        if (descripcion?.Length > 500)
            return (false, "La descripción no puede exceder 500 caracteres", null);

        // Verificar duplicado
        var exists = await _db.Grupos.AnyAsync(g => g.Nombre == nombre);
        if (exists)
            return (false, "Ya existe un grupo con ese nombre", null);

        // Crear
        var grupo = new Grupo
        {
            Nombre = nombre,
            Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion
        };

        _db.Grupos.Add(grupo);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created grupo id={Id}, nombre={Nombre}", grupo.IdGrupo, grupo.Nombre);

        return (true, null, new GrupoDto { Id = grupo.IdGrupo, Nombre = grupo.Nombre, Descripcion = grupo.Descripcion });
    }

    public async Task<(bool success, string? error, GrupoDto? result)> UpdateAsync(int id, GrupoUpdateRequest request)
    {
        var grupo = await _db.Grupos.FindAsync(id);
        if (grupo == null)
            return (false, "Grupo no encontrado", null);

        // Validar
        var nombre = request.Nombre?.Trim();
        if (string.IsNullOrEmpty(nombre))
            return (false, "El nombre es requerido", null);

        if (nombre.Length > 120)
            return (false, "El nombre no puede exceder 120 caracteres", null);

        var descripcion = request.Descripcion?.Trim();
        if (descripcion?.Length > 500)
            return (false, "La descripción no puede exceder 500 caracteres", null);

        // Verificar duplicado (excluyendo el actual)
        var exists = await _db.Grupos.AnyAsync(g => g.Nombre == nombre && g.IdGrupo != id);
        if (exists)
            return (false, "Ya existe otro grupo con ese nombre", null);

        // Actualizar
        grupo.Nombre = nombre;
        grupo.Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Updated grupo id={Id}, nombre={Nombre}", grupo.IdGrupo, grupo.Nombre);

        return (true, null, new GrupoDto { Id = grupo.IdGrupo, Nombre = grupo.Nombre, Descripcion = grupo.Descripcion });
    }

    public async Task<(bool success, string? error)> DeleteAsync(int id)
    {
        var grupo = await _db.Grupos.FindAsync(id);
        if (grupo == null)
            return (false, "Grupo no encontrado");

        // Verificar si está en uso
        var inUse = await _db.PartesDeTrabajo.AnyAsync(p => p.IdGrupo == id);
        if (inUse)
        {
            _logger.LogWarning("Delete grupo id={Id} blocked: en uso por partes de trabajo", id);
            return (false, "No se puede borrar: hay partes de trabajo que usan este grupo");
        }

        _db.Grupos.Remove(grupo);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted grupo id={Id}, nombre={Nombre}", id, grupo.Nombre);

        return (true, null);
    }
}
