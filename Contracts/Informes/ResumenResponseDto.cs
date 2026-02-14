namespace GestionTime.Api.Contracts.Informes;

/// <summary>Intervalo unificado después de eliminar solapes.</summary>
public class MergedIntervalDto
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public int Minutes { get; set; }
}

/// <summary>Gap (espacio) entre intervalos consecutivos.</summary>
public class GapDto
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public int Minutes { get; set; }
}

/// <summary>Resumen estadístico por día (opcional para scope week/range).</summary>
public class DailySummaryDto
{
    public DateTime Date { get; set; }
    public int PartsCount { get; set; }
    public int RecordedMinutes { get; set; }
    public int CoveredMinutes { get; set; }
    public int OverlapMinutes { get; set; }
}

/// <summary>Respuesta del endpoint /api/v2/informes/resumen con cálculo de solapes y gaps.</summary>
public class ResumenResponseDto
{
    public DateTime GeneratedAt { get; set; }
    public object? FiltersApplied { get; set; }
    public int PartsCount { get; set; }
    public int RecordedMinutes { get; set; }
    public int CoveredMinutes { get; set; }
    public int OverlapMinutes { get; set; }
    public List<MergedIntervalDto> MergedIntervals { get; set; } = new();
    public List<GapDto> Gaps { get; set; } = new();
    public DateTime? FirstStart { get; set; }
    public DateTime? LastEnd { get; set; }
    public List<DailySummaryDto>? ByDay { get; set; }
}
