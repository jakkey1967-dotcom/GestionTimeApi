namespace GestionTime.Api.Contracts.Catalog;

public sealed class GrupoDto
{
    public int Id { get; set; }
    public string? Nombre { get; set; }
    public string? Descripcion { get; set; }
}

public sealed class GrupoCreateRequest
{
    public string? Nombre { get; set; }
    public string? Descripcion { get; set; }
}

public sealed class GrupoUpdateRequest
{
    public string? Nombre { get; set; }
    public string? Descripcion { get; set; }
}
