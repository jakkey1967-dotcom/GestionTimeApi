namespace GestionTime.Domain.Work;

public sealed class Cliente
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
