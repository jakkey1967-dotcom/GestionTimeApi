using System.ComponentModel.DataAnnotations;

namespace GestionTime.Api.Contracts.Catalog;

/// <summary>
/// DTO para crear un nuevo cliente (POST)
/// </summary>
public sealed record ClienteCreateDto
{
    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
    public string Nombre { get; init; } = string.Empty;

    public int? IdPuntoop { get; init; }

    public int? LocalNum { get; init; }

    [StringLength(200, ErrorMessage = "El nombre comercial no puede exceder 200 caracteres")]
    public string? NombreComercial { get; init; }

    [StringLength(100, ErrorMessage = "La provincia no puede exceder 100 caracteres")]
    public string? Provincia { get; init; }

    public DateTime? DataUpdate { get; init; }

    public string? DataHtml { get; init; }

    public string? Nota { get; init; }
}
