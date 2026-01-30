namespace GestionTime.Api.Contracts.Catalog;

/// <summary>
/// DTO completo de cliente (para GET)
/// </summary>
public sealed class ClienteDto
{
    public int Id { get; set; }
    public string? Nombre { get; set; }
    public int? IdPuntoop { get; set; }
    public int? LocalNum { get; set; }
    public string? NombreComercial { get; set; }
    public string? Provincia { get; set; }
    public DateTime DataUpdate { get; set; }
    public string? DataHtml { get; set; }
    public string? Nota { get; set; }
}
