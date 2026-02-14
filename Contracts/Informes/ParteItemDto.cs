namespace GestionTime.Api.Contracts.Informes;

/// <summary>Item individual de parte de trabajo en respuesta de informes.</summary>
public class ParteItemDto
{
    public DateTime FechaTrabajo { get; set; }
    public string? HoraInicio { get; set; }
    public string? HoraFin { get; set; }
    public decimal? DuracionHoras { get; set; }
    public int? DuracionMin { get; set; }
    public string? Accion { get; set; }
    public string? Ticket { get; set; }
    public int? IdCliente { get; set; }
    public string? Tienda { get; set; }
    public int? IdGrupo { get; set; }
    public int? IdTipo { get; set; }
    public Guid IdUsuario { get; set; }
    public string? Estado { get; set; }
    public string? Tags { get; set; }
    public string? SemanaIso { get; set; }
    public int? Mes { get; set; }
    public int? Anio { get; set; }
    public string? AgenteNombre { get; set; }
    public string? AgenteEmail { get; set; }
    public string? ClienteNombre { get; set; }
    public string? GrupoNombre { get; set; }
    public string? TipoNombre { get; set; }
}
