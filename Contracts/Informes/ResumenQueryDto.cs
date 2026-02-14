namespace GestionTime.Api.Contracts.Informes;

/// <summary>Parámetros de consulta para obtener resumen de partes (endpoint /api/v2/informes/resumen).</summary>
public class ResumenQueryDto
{
    /// <summary>Alcance temporal: day, week, range.</summary>
    public string Scope { get; set; } = "day";

    /// <summary>Fecha específica (YYYY-MM-DD) para scope=day.</summary>
    public string? Date { get; set; }

    /// <summary>Semana ISO (YYYY-Www) para scope=week.</summary>
    public string? WeekIso { get; set; }

    /// <summary>Fecha inicio del rango (YYYY-MM-DD) para scope=range.</summary>
    public string? From { get; set; }

    /// <summary>Fecha fin del rango (YYYY-MM-DD) para scope=range.</summary>
    public string? To { get; set; }

    /// <summary>Filtrar por ID de agente específico (UUID).</summary>
    public Guid? AgentId { get; set; }

    /// <summary>Filtrar por múltiples IDs de agentes (UUID separados por comas). Solo EDITOR/ADMIN.</summary>
    public string? AgentIds { get; set; }

    /// <summary>Filtrar por ID de cliente.</summary>
    public int? ClientId { get; set; }

    /// <summary>Filtrar por ID de grupo.</summary>
    public int? GroupId { get; set; }

    /// <summary>Filtrar por ID de tipo.</summary>
    public int? TypeId { get; set; }
}
