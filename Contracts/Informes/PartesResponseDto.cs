namespace GestionTime.Api.Contracts.Informes;

/// <summary>Respuesta paginada del endpoint /api/v2/informes/partes.</summary>
public class PartesResponseDto
{
    public DateTime GeneratedAt { get; set; }
    public object? FiltersApplied { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<ParteItemDto> Items { get; set; } = new();
}
