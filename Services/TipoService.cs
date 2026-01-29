using GestionTime.Api.Contracts.Catalog;
using GestionTime.Domain.Work;
using GestionTime.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestionTime.Api.Services;

public sealed class TipoService
{
    private readonly GestionTimeDbContext _db;
    private readonly ILogger<TipoService> _logger;

    public TipoService(GestionTimeDbContext db, ILogger<TipoService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<TipoDto>> ListAsync()
    {
        var tipos = await _db.Tipos
            .OrderBy(t => t.Nombre)
            .Select(t => new TipoDto { Id = t.IdTipo, Nombre = t.Nombre, Descripcion = t.Descripcion })
            .ToListAsync();

        return tipos;
    }

    public async Task<TipoDto?> GetByIdAsync(int id)
    {
        var tipo = await _db.Tipos.FindAsync(id);
        if (tipo == null) return null;

        return new TipoDto { Id = tipo.IdTipo, Nombre = tipo.Nombre, Descripcion = tipo.Descripcion };
    }

    public async Task<(bool success, string? error, TipoDto? result)> CreateAsync(TipoCreateRequest request)
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
        var exists = await _db.Tipos.AnyAsync(t => t.Nombre == nombre);
        if (exists)
            return (false, "Ya existe un tipo con ese nombre", null);

        // Crear
        var tipo = new Tipo
        {
            Nombre = nombre,
            Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion
        };

        _db.Tipos.Add(tipo);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created tipo id={Id}, nombre={Nombre}", tipo.IdTipo, tipo.Nombre);

        return (true, null, new TipoDto 
        { 
            Id = tipo.IdTipo, 
            Nombre = tipo.Nombre, 
            Descripcion = tipo.Descripcion 
        });
    }

    public async Task<(bool success, string? error, TipoDto? result)> UpdateAsync(int id, TipoUpdateRequest request)
    {
        var tipo = await _db.Tipos.FindAsync(id);
        if (tipo == null)
            return (false, "Tipo no encontrado", null);

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
        var exists = await _db.Tipos.AnyAsync(t => t.Nombre == nombre && t.IdTipo != id);
        if (exists)
            return (false, "Ya existe otro tipo con ese nombre", null);

        // Actualizar
        tipo.Nombre = nombre;
        tipo.Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Updated tipo id={Id}, nombre={Nombre}", tipo.IdTipo, tipo.Nombre);

        return (true, null, new TipoDto { Id = tipo.IdTipo, Nombre = tipo.Nombre, Descripcion = tipo.Descripcion });
    }

    public async Task<(bool success, string? error)> DeleteAsync(int id)
    {
        var tipo = await _db.Tipos.FindAsync(id);
        if (tipo == null)
            return (false, "Tipo no encontrado");

        // Verificar si está en uso
        var inUse = await _db.PartesDeTrabajo.AnyAsync(p => p.IdTipo == id);
        if (inUse)
        {
            _logger.LogWarning("Delete tipo id={Id} blocked: en uso por partes de trabajo", id);
            return (false, "No se puede borrar: hay partes de trabajo que usan este tipo");
        }

        _db.Tipos.Remove(tipo);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted tipo id={Id}, nombre={Nombre}", id, tipo.Nombre);

        return (true, null);
    }
}
