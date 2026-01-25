using GestionTime.Domain.Freshdesk;
using GestionTime.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GestionTime.Infrastructure.Services.Freshdesk;

public class FreshdeskService
{
    private readonly FreshdeskClient _client;
    private readonly GestionTimeDbContext _db;
    private readonly ILogger<FreshdeskService> _logger;
    private static readonly TimeSpan AgentCacheExpiration = TimeSpan.FromHours(24);

    public FreshdeskService(
        FreshdeskClient client,
        GestionTimeDbContext db,
        ILogger<FreshdeskService> logger)
    {
        _client = client;
        _db = db;
        _logger = logger;
    }

    public async Task<long?> ResolveAgentIdByEmailAsync(Guid userId, string email, CancellationToken ct = default)
    {
        var cached = await _db.FreshdeskAgentMaps
            .Where(m => m.UserId == userId)
            .FirstOrDefaultAsync(ct);

        if (cached != null && DateTime.UtcNow - cached.SyncedAt < AgentCacheExpiration)
        {
            _logger.LogInformation("AgentId para {Email} encontrado en caché: {AgentId}", email, cached.AgentId);
            return cached.AgentId;
        }

        var agents = await _client.SearchAgentsByEmailAsync(email, ct);
        var agent = agents.FirstOrDefault(a => a.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

        if (agent == null)
        {
            _logger.LogWarning("No se encontró agente Freshdesk para email: {Email}", email);
            return null;
        }

        if (cached != null)
        {
            cached.AgentId = agent.Id;
            cached.SyncedAt = DateTime.UtcNow;
            _db.FreshdeskAgentMaps.Update(cached);
        }
        else
        {
            _db.FreshdeskAgentMaps.Add(new FreshdeskAgentMap
            {
                UserId = userId,
                Email = email,
                AgentId = agent.Id,
                SyncedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(ct);
        
        _logger.LogInformation("AgentId para {Email} resuelto y cacheado: {AgentId}", email, agent.Id);
        return agent.Id;
    }

    public async Task<List<FreshdeskTicketDto>> SuggestTicketsAsync(
        Guid userId,
        string userEmail,
        string? term,
        string scope,
        int limit,
        CancellationToken ct = default)
    {
        long? agentId = null;
        if (scope == "mine_or_unassigned" || scope == "mine")
        {
            agentId = await ResolveAgentIdByEmailAsync(userId, userEmail, ct);
            if (agentId == null && scope == "mine")
            {
                _logger.LogWarning("No se pudo resolver agentId para scope=mine");
                return new List<FreshdeskTicketDto>();
            }
        }

        var queryParts = new List<string>();

        if (scope == "mine_or_unassigned" && agentId.HasValue)
        {
            queryParts.Add($"(agent_id:{agentId.Value} OR agent_id:null)");
        }
        else if (scope == "mine" && agentId.HasValue)
        {
            queryParts.Add($"agent_id:{agentId.Value}");
        }
        else if (scope == "unassigned")
        {
            queryParts.Add("agent_id:null");
        }

        if (!string.IsNullOrWhiteSpace(term))
        {
            if (long.TryParse(term, out var ticketId))
            {
                queryParts.Add($"(id:{ticketId} OR subject:'{term}' OR description:'{term}')");
            }
            else
            {
                queryParts.Add($"(subject:'{term}' OR description:'{term}')");
            }
        }

        queryParts.Add("(status:2 OR status:3)");

        var query = string.Join(" AND ", queryParts);

        var result = await _client.SearchTicketsAsync(query, 1, ct);

        return result.Results.Take(limit).ToList();
    }

    public async Task<List<string>> SuggestTagsAsync(string? term, int limit, CancellationToken ct = default)
    {
        var query = _db.FreshdeskTags.AsQueryable();

        if (!string.IsNullOrWhiteSpace(term))
        {
            query = query.Where(t => EF.Functions.ILike(t.Name, $"{term}%"));
        }

        return await query
            .OrderByDescending(t => t.LastSeenAt)
            .Take(limit)
            .Select(t => t.Name)
            .ToListAsync(ct);
    }

    public async Task<FreshdeskSyncResult> SyncTagsFromFreshdeskAsync(
        string mode = "recent",
        int days = 30,
        int limit = 1000,
        CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = new FreshdeskSyncResult { StartedAt = DateTime.UtcNow };
        
        _logger.LogInformation("🔄 Iniciando sincronización de tags desde Freshdesk");
        _logger.LogInformation("   📊 Modo: {Mode}, Días: {Days}, Límite: {Limit}", mode, days, limit);

        try
        {
            // Construir query según modo
            string query;
            if (mode == "full")
            {
                query = "(status:2 OR status:3 OR status:4)"; // Open, Pending, Resolved
                _logger.LogInformation("   🌐 Modo FULL: buscando todos los tickets abiertos/pendientes/resueltos");
            }
            else // recent
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-days);
                var dateStr = cutoffDate.ToString("yyyy-MM-dd");
                query = $"updated_at:>'{dateStr}'";
                _logger.LogInformation("   📅 Modo RECENT: tickets actualizados desde {Date}", dateStr);
            }
            
            _logger.LogInformation("   📝 Query Freshdesk: {Query}", query);
            
            // Buscar tickets con paginación
            var tickets = await _client.SearchAllTicketsAsync(query, limit, ct);
            result.TicketsScanned = tickets.Count;
            
            _logger.LogInformation("   ✅ {Count} tickets encontrados", tickets.Count);

            var tagsToSync = new HashSet<string>();
            var now = DateTime.UtcNow;

            // Extraer tags de cada ticket
            foreach (var ticket in tickets)
            {
                var ticketDetail = await _client.GetTicketByIdAsync(ticket.Id, ct);
                if (ticketDetail?.Tags != null)
                {
                    foreach (var tag in ticketDetail.Tags)
                    {
                        if (!string.IsNullOrWhiteSpace(tag))
                        {
                            // Normalizar: trim + lowercase
                            var normalized = tag.Trim().ToLowerInvariant();
                            tagsToSync.Add(normalized);
                        }
                    }
                }
                
                // Delay entre requests para evitar rate limit
                if (tickets.Count > 100)
                    await Task.Delay(TimeSpan.FromMilliseconds(100), ct);
            }

            result.TagsFound = tagsToSync.Count;
            _logger.LogInformation("   🏷️  {Count} tags únicos encontrados", tagsToSync.Count);

            // Upsert en base de datos
            foreach (var tagName in tagsToSync)
            {
                var existing = await _db.FreshdeskTags.FindAsync(new object[] { tagName }, ct);
                if (existing != null)
                {
                    existing.LastSeenAt = now;
                    _db.FreshdeskTags.Update(existing);
                    result.TagsUpdated++;
                }
                else
                {
                    _db.FreshdeskTags.Add(new FreshdeskTag
                    {
                        Name = tagName,
                        Source = "freshdesk",
                        LastSeenAt = now
                    });
                    result.TagsInserted++;
                }
            }

            await _db.SaveChangesAsync(ct);
            
            sw.Stop();
            result.DurationMs = sw.ElapsedMilliseconds;
            result.CompletedAt = DateTime.UtcNow;
            
            _logger.LogInformation("✅ Sincronización completada en {Ms}ms", sw.ElapsedMilliseconds);
            _logger.LogInformation("   📊 Tickets: {Tickets}, Tags: {Tags} ({Inserted} nuevos, {Updated} actualizados)", 
                result.TicketsScanned, result.TagsFound, result.TagsInserted, result.TagsUpdated);

            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            result.DurationMs = sw.ElapsedMilliseconds;
            result.CompletedAt = DateTime.UtcNow;
            result.Error = ex.Message;
            
            _logger.LogError(ex, "❌ Error en sincronización de tags (duración: {Ms}ms)", sw.ElapsedMilliseconds);
            
            // NO hacer throw - devolver result con error para que el controller pueda mostrar métricas
            return result;
        }
    }
}

/// <summary>
/// Resultado de sincronización de tags con métricas detalladas
/// </summary>
public class FreshdeskSyncResult
{
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long DurationMs { get; set; }
    public int TicketsScanned { get; set; }
    public int TagsFound { get; set; }
    public int TagsInserted { get; set; }
    public int TagsUpdated { get; set; }
    public string? Error { get; set; }
    public bool Success => Error == null;
}

