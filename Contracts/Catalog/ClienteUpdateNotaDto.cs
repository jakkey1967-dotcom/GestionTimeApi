namespace GestionTime.Api.Contracts.Catalog;

/// <summary>
/// DTO para actualizar solo la nota de un cliente (PATCH)
/// </summary>
public sealed class ClienteUpdateNotaDto
{
    public string? Nota { get; set; }
}
