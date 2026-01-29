namespace GestionTime.Api.Contracts.Catalog;

public sealed class TipoDto
{
    public int Id { get; set; }
    public string? Nombre { get; set; }
    public string? Descripcion { get; set; }
}

public sealed class TipoCreateRequest
{
    public string? Nombre { get; set; }
    public string? Descripcion { get; set; }
}

public sealed class TipoUpdateRequest
{
    public string? Nombre { get; set; }
    public string? Descripcion { get; set; }
}
