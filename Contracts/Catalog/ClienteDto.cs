namespace GestionTime.Api.Contracts.Catalog;

/// <summary>
/// DTO completo de cliente (para GET)
/// </summary>
public sealed record ClienteDto
{
    public int Id { get; init; }
    public string? Nombre { get; init; }
    public int? IdPuntoop { get; init; }
    public int? LocalNum { get; init; }
    public string? NombreComercial { get; init; }
    public string? Provincia { get; init; }
    public DateTime DataUpdate { get; init; }
    public string? DataHtml { get; init; }
    public string? Nota { get; init; }
}
