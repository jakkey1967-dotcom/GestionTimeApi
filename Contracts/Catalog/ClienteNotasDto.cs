namespace GestionTime.Api.Contracts.Catalog;

/// <summary>Respuesta GET /api/v2/clientes/{id}/notas.</summary>
public sealed class ClienteNotasResponseDto
{
    public int ClienteId { get; set; }
    public ClienteNotaItemDto? Global { get; set; }
    public ClienteNotaItemDto? Personal { get; set; }
}

/// <summary>Detalle de una nota (global o personal).</summary>
public sealed class ClienteNotaItemDto
{
    public string Text { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedByName { get; set; }
}

/// <summary>Request PUT /api/v2/clientes/{id}/notas/global o /personal.</summary>
public sealed class ClienteNotaUpdateDto
{
    public string? Text { get; set; }
}
