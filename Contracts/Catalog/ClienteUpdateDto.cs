using System.ComponentModel.DataAnnotations;

namespace GestionTime.Api.Contracts.Catalog;

/// <summary>
/// DTO para actualizar un cliente existente (PUT)
/// </summary>
public sealed class ClienteUpdateDto
{
    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
    public string? Nombre { get; set; }

    public int? IdPuntoop { get; set; }

    public int? LocalNum { get; set; }

    [StringLength(200, ErrorMessage = "El nombre comercial no puede exceder 200 caracteres")]
    public string? NombreComercial { get; set; }

    [StringLength(100, ErrorMessage = "La provincia no puede exceder 100 caracteres")]
    public string? Provincia { get; set; }

    public DateTime? DataUpdate { get; set; }

    public string? DataHtml { get; set; }

    public string? Nota { get; set; }
}
