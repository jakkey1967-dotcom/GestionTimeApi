using GestionTime.Api.Contracts.Informes;
using GestionTime.Domain.Reports;
using GestionTime.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace GestionTime.Api.Services;

/// <summary>Servicio de informes v2 (solo lectura) con cálculo de solapes y gaps.</summary>
public class InformesService
{
    private readonly GestionTimeDbContext _db;
    private readonly ILogger<InformesService> _logger;

    public InformesService(GestionTimeDbContext db, ILogger<InformesService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>Obtiene partes de trabajo con paginación, filtros y ordenamiento.</summary>
    public async Task<PartesResponseDto> GetPartesAsync(PartesQueryDto query, Guid currentUserId, string userRole)
    {
        var startTime = DateTime.UtcNow;
        
        // Validar y parsear filtros de fecha
        var (fromDate, toDate) = ParseDateFilters(query.Date, query.WeekIso, query.From, query.To);

        // Construir query base
        IQueryable<VPartesStatsFull> baseQuery = _db.Set<VPartesStatsFull>();

        // SIEMPRE filtrar por rango de fechas
        baseQuery = baseQuery.Where(p => p.FechaTrabajo >= fromDate && p.FechaTrabajo <= toDate);

        // Filtro por agente (con control de rol)
        var agentIds = ResolveAgentIds(query.AgentId, query.AgentIds, currentUserId, userRole);

        // Si es EDITOR/ADMIN y no especificó agentId, usar currentUserId por defecto
        if (!agentIds.Any() && (userRole == "EDITOR" || userRole == "ADMIN"))
        {
            agentIds.Add(currentUserId);
        }

        // SIEMPRE aplicar filtro de agente (ahora nunca estará vacío)
        baseQuery = baseQuery.Where(p => agentIds.Contains(p.IdUsuario));

        // Filtros opcionales
        if (query.ClientId.HasValue)
            baseQuery = baseQuery.Where(p => p.IdCliente == query.ClientId.Value);

        if (query.GroupId.HasValue)
            baseQuery = baseQuery.Where(p => p.IdGrupo == query.GroupId.Value);

        if (query.TypeId.HasValue)
            baseQuery = baseQuery.Where(p => p.IdTipo == query.TypeId.Value);

        // Búsqueda de texto (PostgreSQL ILike)
        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var searchTerm = $"%{query.Q}%";
            baseQuery = baseQuery.Where(p =>
                EF.Functions.ILike(p.Ticket ?? "", searchTerm) ||
                EF.Functions.ILike(p.Accion ?? "", searchTerm) ||
                EF.Functions.ILike(p.Tienda ?? "", searchTerm) ||
                EF.Functions.ILike(p.ClienteNombre ?? "", searchTerm)
            );
        }

        // Total ANTES de paginar
        var total = await baseQuery.CountAsync();

        // Ordenamiento
        baseQuery = ApplySort(baseQuery, query.Sort ?? "fecha_trabajo:desc,hora_inicio:asc");

        // Paginación
        var pageSize = Math.Clamp(query.PageSize, 1, 200);
        var page = Math.Max(1, query.Page);
        var skip = (page - 1) * pageSize;

        var items = await baseQuery.Skip(skip).Take(pageSize).ToListAsync();

        var duration = DateTime.UtcNow - startTime;
        _logger.LogInformation(
            "Informes/partes: user={UserId}, role={Role}, filters={@Filters}, total={Total}, duration={Duration}ms",
            currentUserId, userRole, new { query.Date, query.WeekIso, query.From, query.To, query.AgentId, query.AgentIds, query.ClientId, query.GroupId, query.TypeId, query.Q }, total, duration.TotalMilliseconds
        );

        return new PartesResponseDto
        {
            GeneratedAt = DateTime.UtcNow,
            FiltersApplied = new { query.Date, query.WeekIso, query.From, query.To, query.AgentId, query.AgentIds, query.ClientId, query.GroupId, query.TypeId, query.Q },
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items.Select(MapToParteItemDto).ToList()
        };
    }

    /// <summary>Obtiene resumen estadístico con cálculo de solapes y gaps usando sweep line.</summary>
    public async Task<ResumenResponseDto> GetResumenAsync(ResumenQueryDto query, Guid currentUserId, string userRole)
    {
        var startTime = DateTime.UtcNow;

        // Validar y parsear filtros de fecha
        var (fromDate, toDate) = ParseDateFiltersFromScope(query.Scope, query.Date, query.WeekIso, query.From, query.To);

        // Construir query base
        IQueryable<VPartesStatsFull> baseQuery = _db.Set<VPartesStatsFull>();

        // SIEMPRE filtrar por rango de fechas
        baseQuery = baseQuery.Where(p => p.FechaTrabajo >= fromDate && p.FechaTrabajo <= toDate);

        // Filtro por agente (con control de rol)
        var agentIds = ResolveAgentIds(query.AgentId, query.AgentIds, currentUserId, userRole);

        // Si es EDITOR/ADMIN y no especificó agentId, usar currentUserId por defecto
        if (!agentIds.Any() && (userRole == "EDITOR" || userRole == "ADMIN"))
        {
            agentIds.Add(currentUserId);
        }

        // SIEMPRE aplicar filtro de agente (ahora nunca estará vacío)
        baseQuery = baseQuery.Where(p => agentIds.Contains(p.IdUsuario));

        // Filtros opcionales
        if (query.ClientId.HasValue)
            baseQuery = baseQuery.Where(p => p.IdCliente == query.ClientId.Value);

        if (query.GroupId.HasValue)
            baseQuery = baseQuery.Where(p => p.IdGrupo == query.GroupId.Value);

        if (query.TypeId.HasValue)
            baseQuery = baseQuery.Where(p => p.IdTipo == query.TypeId.Value);

        var partes = await baseQuery.ToListAsync();

        // Calcular estadísticas con algoritmo sweep line
        var stats = CalculateStats(partes);

        // Si scope es week/range, calcular resumen por día
        List<DailySummaryDto>? byDay = null;
        if (query.Scope == "week" || query.Scope == "range")
        {
            byDay = CalculateDailySummaries(partes, fromDate, toDate);
        }

        var duration = DateTime.UtcNow - startTime;
        _logger.LogInformation(
            "Informes/resumen: user={UserId}, role={Role}, scope={Scope}, filters={@Filters}, parts={PartsCount}, duration={Duration}ms",
            currentUserId, userRole, query.Scope, new { query.Date, query.WeekIso, query.From, query.To, query.AgentId, query.AgentIds, query.ClientId, query.GroupId, query.TypeId }, partes.Count, duration.TotalMilliseconds
        );

        return new ResumenResponseDto
        {
            GeneratedAt = DateTime.UtcNow,
            FiltersApplied = new { query.Scope, query.Date, query.WeekIso, query.From, query.To, query.AgentId, query.AgentIds, query.ClientId, query.GroupId, query.TypeId },
            PartsCount = stats.PartsCount,
            RecordedMinutes = stats.RecordedMinutes,
            CoveredMinutes = stats.CoveredMinutes,
            OverlapMinutes = stats.OverlapMinutes,
            MergedIntervals = stats.MergedIntervals,
            Gaps = stats.Gaps,
            FirstStart = stats.FirstStart,
            LastEnd = stats.LastEnd,
            ByDay = byDay
        };
    }

    /// <summary>Algoritmo sweep line para calcular minutos cubiertos, solapes, intervalos unificados y gaps.</summary>
    private (int PartsCount, int RecordedMinutes, int CoveredMinutes, int OverlapMinutes, List<MergedIntervalDto> MergedIntervals, List<GapDto> Gaps, DateTime? FirstStart, DateTime? LastEnd) CalculateStats(List<VPartesStatsFull> partes)
    {
        if (!partes.Any())
            return (0, 0, 0, 0, new(), new(), null, null);

        var intervals = new List<(DateTime Start, DateTime End, int Minutes)>();

        foreach (var parte in partes)
        {
            if (parte.HoraInicio.HasValue && parte.HoraFin.HasValue)
            {
                var start = parte.FechaTrabajo.Add(parte.HoraInicio.Value.ToTimeSpan());
                var end = parte.FechaTrabajo.Add(parte.HoraFin.Value.ToTimeSpan());
                
                // Si hora_fin < hora_inicio, el parte cruza medianoche
                if (parte.HoraFin.Value < parte.HoraInicio.Value)
                {
                    end = end.AddDays(1);
                }

                var minutes = (int)(end - start).TotalMinutes;
                if (minutes > 0)
                {
                    intervals.Add((start, end, minutes));
                }
            }
        }

        if (!intervals.Any())
            return (partes.Count, 0, 0, 0, new(), new(), null, null);

        // Ordenar por start
        intervals = intervals.OrderBy(i => i.Start).ToList();

        var recordedMinutes = intervals.Sum(i => i.Minutes);
        var mergedIntervals = new List<MergedIntervalDto>();
        var gaps = new List<GapDto>();

        DateTime currentStart = intervals[0].Start;
        DateTime currentEnd = intervals[0].End;
        int overlapMinutes = 0;

        for (int i = 1; i < intervals.Count; i++)
        {
            var (start, end, _) = intervals[i];

            if (start <= currentEnd)
            {
                // Hay solape
                var overlapStart = start;
                var overlapEnd = end < currentEnd ? end : currentEnd;
                var overlap = (int)(overlapEnd - overlapStart).TotalMinutes;
                if (overlap > 0)
                {
                    overlapMinutes += overlap;
                }

                // Extender intervalo unificado
                if (end > currentEnd)
                {
                    currentEnd = end;
                }
            }
            else
            {
                // No hay solape, cerrar intervalo anterior
                mergedIntervals.Add(new MergedIntervalDto
                {
                    Start = currentStart,
                    End = currentEnd,
                    Minutes = (int)(currentEnd - currentStart).TotalMinutes
                });

                // Registrar gap
                var gapMinutes = (int)(start - currentEnd).TotalMinutes;
                if (gapMinutes > 0)
                {
                    gaps.Add(new GapDto
                    {
                        Start = currentEnd,
                        End = start,
                        Minutes = gapMinutes
                    });
                }

                // Iniciar nuevo intervalo
                currentStart = start;
                currentEnd = end;
            }
        }

        // Cerrar el último intervalo
        mergedIntervals.Add(new MergedIntervalDto
        {
            Start = currentStart,
            End = currentEnd,
            Minutes = (int)(currentEnd - currentStart).TotalMinutes
        });

        var coveredMinutes = mergedIntervals.Sum(m => m.Minutes);
        var firstStart = intervals.First().Start;
        var lastEnd = intervals.Last().End;

        return (partes.Count, recordedMinutes, coveredMinutes, overlapMinutes, mergedIntervals, gaps, firstStart, lastEnd);
    }

    /// <summary>Calcula resumen por día para scope week/range.</summary>
    private List<DailySummaryDto> CalculateDailySummaries(List<VPartesStatsFull> partes, DateTime fromDate, DateTime toDate)
    {
        var dailySummaries = new List<DailySummaryDto>();

        for (var date = fromDate; date <= toDate; date = date.AddDays(1))
        {
            var dayPartes = partes.Where(p => p.FechaTrabajo.Date == date.Date).ToList();
            var dayStats = CalculateStats(dayPartes);

            dailySummaries.Add(new DailySummaryDto
            {
                Date = date.Date,
                PartsCount = dayStats.PartsCount,
                RecordedMinutes = dayStats.RecordedMinutes,
                CoveredMinutes = dayStats.CoveredMinutes,
                OverlapMinutes = dayStats.OverlapMinutes
            });
        }

        return dailySummaries;
    }

    /// <summary>Resuelve los IDs de agentes aplicando control de roles.</summary>
    private List<Guid> ResolveAgentIds(Guid? agentId, string? agentIdsStr, Guid currentUserId, string userRole)
    {
        var agentIds = new List<Guid>();

        if (userRole == "USER")
        {
            // USER solo puede ver sus propios datos
            if (agentId.HasValue && agentId.Value != currentUserId)
            {
                throw new UnauthorizedAccessException("USER no puede consultar datos de otros agentes");
            }
            if (!string.IsNullOrWhiteSpace(agentIdsStr))
            {
                throw new UnauthorizedAccessException("USER no puede usar agentIds");
            }
            agentIds.Add(currentUserId);
        }
        else if (userRole == "EDITOR" || userRole == "ADMIN")
        {
            // EDITOR/ADMIN pueden ver cualquier agente
            if (agentId.HasValue)
            {
                agentIds.Add(agentId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(agentIdsStr))
            {
                var ids = agentIdsStr.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var id in ids)
                {
                    if (Guid.TryParse(id.Trim(), out var guid))
                    {
                        agentIds.Add(guid);
                    }
                }
            }
            // Si no se especifica ninguno, agentIds queda vacío (= todos)
        }

        return agentIds;
    }

    /// <summary>Parsea filtros de fecha para endpoint /partes.</summary>
    private (DateTime From, DateTime To) ParseDateFilters(string? date, string? weekIso, string? from, string? to)
    {
        if (!string.IsNullOrWhiteSpace(date))
        {
            if (!DateTime.TryParse(date, out var d))
                throw new ArgumentException("date inválido (formato: YYYY-MM-DD)");
            return (DateTime.SpecifyKind(d.Date, DateTimeKind.Utc), DateTime.SpecifyKind(d.Date, DateTimeKind.Utc));
        }

        if (!string.IsNullOrWhiteSpace(weekIso))
        {
            var (start, end) = ParseWeekIso(weekIso);
            return (start, end);
        }

        if (!string.IsNullOrWhiteSpace(from) && !string.IsNullOrWhiteSpace(to))
        {
            if (!DateTime.TryParse(from, out var f) || !DateTime.TryParse(to, out var t))
                throw new ArgumentException("from/to inválidos (formato: YYYY-MM-DD)");
            return (DateTime.SpecifyKind(f.Date, DateTimeKind.Utc), DateTime.SpecifyKind(t.Date, DateTimeKind.Utc));
        }

        throw new ArgumentException("Debe especificar date, weekIso, o from+to");
    }

    /// <summary>Parsea filtros de fecha desde scope para endpoint /resumen.</summary>
    private (DateTime From, DateTime To) ParseDateFiltersFromScope(string scope, string? date, string? weekIso, string? from, string? to)
    {
        return scope.ToLower() switch
        {
            "day" when !string.IsNullOrWhiteSpace(date) => ParseSingleDate(date),
            "week" when !string.IsNullOrWhiteSpace(weekIso) => ParseWeekIso(weekIso),
            "range" when !string.IsNullOrWhiteSpace(from) && !string.IsNullOrWhiteSpace(to) => ParseRange(from, to),
            _ => throw new ArgumentException($"Scope '{scope}' requiere parámetros date/weekIso/from+to correspondientes")
        };
    }

    private (DateTime, DateTime) ParseSingleDate(string date)
    {
        if (!DateTime.TryParse(date, out var d))
            throw new ArgumentException("date inválido (formato: YYYY-MM-DD)");
        return (DateTime.SpecifyKind(d.Date, DateTimeKind.Utc), DateTime.SpecifyKind(d.Date, DateTimeKind.Utc));
    }

    private (DateTime, DateTime) ParseRange(string from, string to)
    {
        if (!DateTime.TryParse(from, out var f) || !DateTime.TryParse(to, out var t))
            throw new ArgumentException("from/to inválidos (formato: YYYY-MM-DD)");
        return (DateTime.SpecifyKind(f.Date, DateTimeKind.Utc), DateTime.SpecifyKind(t.Date, DateTimeKind.Utc));
    }

    /// <summary>Parsea semana ISO (YYYY-Www) a rango de fechas.</summary>
    private (DateTime Start, DateTime End) ParseWeekIso(string weekIso)
    {
        if (!weekIso.Contains("-W"))
            throw new ArgumentException("weekIso debe tener formato YYYY-Www (ejemplo: 2026-W07)");

        var parts = weekIso.Split('-', 'W');
        if (parts.Length != 3 || !int.TryParse(parts[0], out var year) || !int.TryParse(parts[2], out var week))
            throw new ArgumentException("weekIso inválido (formato: YYYY-Www)");

        if (week < 1 || week > 53)
            throw new ArgumentException("Semana ISO debe estar entre 1 y 53");

        var jan1 = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var daysOffset = DayOfWeek.Monday - jan1.DayOfWeek;
        if (daysOffset > 3) daysOffset -= 7;
        var firstMonday = jan1.AddDays(daysOffset);
        var start = firstMonday.AddDays((week - 1) * 7);
        var end = start.AddDays(6);

        return (start, end);
    }

    /// <summary>Aplica ordenamiento dinámico (ejemplo: fecha_trabajo:desc,hora_inicio:asc).</summary>
    private IQueryable<VPartesStatsFull> ApplySort(IQueryable<VPartesStatsFull> query, string sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
            return query.OrderByDescending(p => p.FechaTrabajo).ThenBy(p => p.HoraInicio);

        var sortFields = sort.Split(',', StringSplitOptions.RemoveEmptyEntries);
        IOrderedQueryable<VPartesStatsFull>? orderedQuery = null;

        foreach (var field in sortFields)
        {
            var parts = field.Trim().Split(':');
            var fieldName = parts[0].Trim().ToLower();
            var direction = parts.Length > 1 ? parts[1].Trim().ToLower() : "asc";

            if (orderedQuery == null)
            {
                orderedQuery = ApplySortField(query, fieldName, direction, true);
            }
            else
            {
                orderedQuery = ApplySortField(orderedQuery, fieldName, direction, false);
            }
        }

        return orderedQuery ?? query;
    }

    private IOrderedQueryable<VPartesStatsFull> ApplySortField(IQueryable<VPartesStatsFull> query, string field, string direction, bool isFirst)
    {
        var isAsc = direction != "desc";

        return field switch
        {
            "fecha_trabajo" => isFirst
                ? (isAsc ? query.OrderBy(p => p.FechaTrabajo) : query.OrderByDescending(p => p.FechaTrabajo))
                : (isAsc ? ((IOrderedQueryable<VPartesStatsFull>)query).ThenBy(p => p.FechaTrabajo) : ((IOrderedQueryable<VPartesStatsFull>)query).ThenByDescending(p => p.FechaTrabajo)),
            "hora_inicio" => isFirst
                ? (isAsc ? query.OrderBy(p => p.HoraInicio) : query.OrderByDescending(p => p.HoraInicio))
                : (isAsc ? ((IOrderedQueryable<VPartesStatsFull>)query).ThenBy(p => p.HoraInicio) : ((IOrderedQueryable<VPartesStatsFull>)query).ThenByDescending(p => p.HoraInicio)),
            "hora_fin" => isFirst
                ? (isAsc ? query.OrderBy(p => p.HoraFin) : query.OrderByDescending(p => p.HoraFin))
                : (isAsc ? ((IOrderedQueryable<VPartesStatsFull>)query).ThenBy(p => p.HoraFin) : ((IOrderedQueryable<VPartesStatsFull>)query).ThenByDescending(p => p.HoraFin)),
            "duracion_min" => isFirst
                ? (isAsc ? query.OrderBy(p => p.DuracionMin) : query.OrderByDescending(p => p.DuracionMin))
                : (isAsc ? ((IOrderedQueryable<VPartesStatsFull>)query).ThenBy(p => p.DuracionMin) : ((IOrderedQueryable<VPartesStatsFull>)query).ThenByDescending(p => p.DuracionMin)),
            "agente_nombre" => isFirst
                ? (isAsc ? query.OrderBy(p => p.AgenteNombre) : query.OrderByDescending(p => p.AgenteNombre))
                : (isAsc ? ((IOrderedQueryable<VPartesStatsFull>)query).ThenBy(p => p.AgenteNombre) : ((IOrderedQueryable<VPartesStatsFull>)query).ThenByDescending(p => p.AgenteNombre)),
            "cliente_nombre" => isFirst
                ? (isAsc ? query.OrderBy(p => p.ClienteNombre) : query.OrderByDescending(p => p.ClienteNombre))
                : (isAsc ? ((IOrderedQueryable<VPartesStatsFull>)query).ThenBy(p => p.ClienteNombre) : ((IOrderedQueryable<VPartesStatsFull>)query).ThenByDescending(p => p.ClienteNombre)),
            _ => isFirst ? query.OrderByDescending(p => p.FechaTrabajo) : ((IOrderedQueryable<VPartesStatsFull>)query).ThenByDescending(p => p.FechaTrabajo)
        };
    }

    /// <summary>Mapea VPartesStatsFull a ParteItemDto.</summary>
    private ParteItemDto MapToParteItemDto(VPartesStatsFull parte)
    {
        return new ParteItemDto
        {
            FechaTrabajo = parte.FechaTrabajo,
            HoraInicio = parte.HoraInicio?.ToString("HH:mm"),
            HoraFin = parte.HoraFin?.ToString("HH:mm"),
            DuracionHoras = parte.DuracionHoras,
            DuracionMin = parte.DuracionMin,
            Accion = parte.Accion,
            Ticket = parte.Ticket,
            IdCliente = parte.IdCliente,
            Tienda = parte.Tienda,
            IdGrupo = parte.IdGrupo,
            IdTipo = parte.IdTipo,
            IdUsuario = parte.IdUsuario,
            Estado = parte.Estado,
            Tags = parte.Tags,
            SemanaIso = parte.SemanaIso?.ToString(),
            Mes = parte.Mes,
            Anio = parte.Anio,
            AgenteNombre = parte.AgenteNombre,
            AgenteEmail = parte.AgenteEmail,
            ClienteNombre = parte.ClienteNombre,
            GrupoNombre = parte.GrupoNombre,
            TipoNombre = parte.TipoNombre
        };
    }
}
