namespace GestionTime.Api.Contracts.Catalog;

/// <summary>
/// Resultado paginado de clientes
/// </summary>
public sealed record ClientePagedResult
{
    public IReadOnlyList<ClienteDto> Items { get; init; } = Array.Empty<ClienteDto>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
