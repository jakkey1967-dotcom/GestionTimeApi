namespace GestionTime.Api.Contracts.Informes;

/// <summary>Parámetros de consulta para obtener partes de trabajo (endpoint /api/v2/informes/partes).</summary>
public class PartesQueryDto
{
    /// <summary>Fecha específica (YYYY-MM-DD). Usar SOLO UNO de: date, weekIso, o from+to.</summary>
    public string? Date { get; set; }

    /// <summary>Semana ISO (YYYY-Www, ejemplo: 2026-W07). Usar SOLO UNO de: date, weekIso, o from+to.</summary>
    public string? WeekIso { get; set; }

    /// <summary>Fecha inicio del rango (YYYY-MM-DD). Debe usarse junto con 'to'.</summary>
    public string? From { get; set; }

    /// <summary>Fecha fin del rango (YYYY-MM-DD). Debe usarse junto con 'from'.</summary>
    public string? To { get; set; }

    /// <summary>Filtrar por ID de agente específico (UUID). USER solo puede ver propios datos.</summary>
    public Guid? AgentId { get; set; }

    /// <summary>Filtrar por múltiples IDs de agentes (UUID separados por comas). Solo EDITOR/ADMIN.</summary>
    public string? AgentIds { get; set; }

    /// <summary>Filtrar por ID de cliente.</summary>
    public int? ClientId { get; set; }

    /// <summary>Filtrar por ID de grupo.</summary>
    public int? GroupId { get; set; }

    /// <summary>Filtrar por ID de tipo.</summary>
    public int? TypeId { get; set; }

    /// <summary>Búsqueda de texto libre en: ticket, accion, tienda, cliente_nombre.</summary>
    public string? Q { get; set; }

    /// <summary>Número de página (comienza en 1).</summary>
    public int Page { get; set; } = 1;

    /// <summary>Tamaño de página (max 200).</summary>
    public int PageSize { get; set; } = 50;

    /// <summary>Orden: campo:dir,campo2:dir (ejemplo: fecha_trabajo:desc,hora_inicio:asc).</summary>
    public string? Sort { get; set; }
}

