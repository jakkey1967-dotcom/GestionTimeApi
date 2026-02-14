namespace GestionTime.Domain.Reports;

/// <summary>Entidad mapeada a la vista PostgreSQL pss_dvnx.v_partes_stats_full (solo lectura).</summary>
public class VPartesStatsFull
{
    public DateTime FechaTrabajo { get; set; }
    public TimeOnly? HoraInicio { get; set; }
    public TimeOnly? HoraFin { get; set; }
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
    public DateTime? FechaDia { get; set; }
    public int? SemanaIso { get; set; }
    public int? Mes { get; set; }
    public int? Anio { get; set; }
    public string? AgenteNombre { get; set; }
    public string? AgenteEmail { get; set; }
    public string? ClienteNombre { get; set; }
    public string? GrupoNombre { get; set; }
    public string? TipoNombre { get; set; }
    public decimal? DuracionHorasTs { get; set; }
    public decimal? DuracionMinTs { get; set; }
}
