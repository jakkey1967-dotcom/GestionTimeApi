using GestionTime.Api.Contracts.Integrations;
using GestionTime.Domain.Freshdesk;
using GestionTime.Infrastructure.Persistence;
using GestionTime.Infrastructure.Services.Freshdesk;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionTime.Api.Controllers;

/// <summary>Endpoints de integración con servicios externos (Freshdesk, etc.).</summary>
[ApiController]
[Route("api/v1/integrations/freshdesk/sync")]
[Authorize(Roles = "ADMIN")] // Solo administradores pueden ejecutar sincronizaciones manuales
public class FreshdeskIntegrationController : ControllerBase
{
    private readonly GestionTimeDbContext _db;
    private readonly FreshdeskClient _freshdeskClient;
    private readonly ILogger<FreshdeskIntegrationController> _logger;

    public FreshdeskIntegrationController(
        GestionTimeDbContext db,
        FreshdeskClient freshdeskClient,
        ILogger<FreshdeskIntegrationController> logger)
    {
        _db = db;
        _freshdeskClient = freshdeskClient;
        _logger = logger;
    }

    /// <summary>Sincronización manual de tags de Freshdesk desde la API REST (con paginación).</summary>
    /// <remarks>
    /// ⚠️ Este endpoint llama DIRECTAMENTE a la API de Freshdesk (NO usa cache local).
    /// 
    /// Lógica idéntica al script PowerShell:
    /// 1. Llama a GET /api/v2/tickets?per_page=100&amp;updated_since={since}&amp;order_by=updated_at&amp;order_type=asc
    /// 2. Pagina hasta 300 páginas (límite de Freshdesk)
    /// 3. Corta cuando updated_at >= until
    /// 4. Extrae todos los tags de cada ticket
    /// 5. Hace UPSERT en pss_dvnx.freshdesk_tags
    /// 
    /// Parámetros opcionales:
    /// - since: Fecha inicio (por defecto: 2024-01-01)
    /// - until: Fecha fin (por defecto: 2025-01-01)
    /// 
    /// Requiere rol ADMIN.
    /// </remarks>
    [HttpPost("tags")]
    public async Task<ActionResult<FreshdeskTagsSyncResponse>> SyncTags(
        [FromQuery] DateTime? since = null, 
        [FromQuery] DateTime? until = null,
        CancellationToken ct = default)
    {
        // Valores por defecto: solo 2024 (igual que script PowerShell)
        var sinceDate = since ?? new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var untilDate = until ?? new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        
        _logger.LogInformation("🔄 Iniciando sync de tags desde API Freshdesk (desde {Since} hasta {Until})...", 
            sinceDate.ToString("yyyy-MM-dd"), untilDate.ToString("yyyy-MM-dd"));
        
        try
        {
            var syncStarted = DateTime.UtcNow;
            var tagsSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var totalTickets = 0;
            var taggedTickets = 0;
            const int MAX_PAGES = 300; // Límite de Freshdesk para endpoint /tickets
            const int PER_PAGE = 100;
            
            // Construir URL base (igual que PowerShell)
            var baseUrl = $"api/v2/tickets?per_page={PER_PAGE}" +
                         $"&updated_since={sinceDate:yyyy-MM-ddTHH:mm:ssZ}" +
                         $"&order_by=updated_at&order_type=asc";
            
            _logger.LogInformation("📡 URL base: {BaseUrl}", baseUrl);
            
            bool stop = false;
            
            for (int page = 1; page <= MAX_PAGES && !stop; page++)
            {
                var url = $"{baseUrl}&page={page}";
                
                _logger.LogDebug("   Página {Page}: {Url}", page, url);
                
                // Llamar a la API de Freshdesk
                var tickets = await _freshdeskClient.GetAsync<List<FreshdeskTicketListDto>>(url, ct);
                
                if (tickets == null || tickets.Count == 0)
                {
                    _logger.LogInformation("   Página {Page}: sin resultados, fin de paginación", page);
                    break;
                }
                
                foreach (var ticket in tickets)
                {
                    // Corte por fecha (igual que PowerShell)
                    if (ticket.UpdatedAt >= untilDate)
                    {
                        _logger.LogInformation("   ⏹️ Ticket {Id} tiene updated_at >= {Until}, cortando...", 
                            ticket.Id, untilDate);
                        stop = true;
                        break;
                    }
                    
                    totalTickets++;
                    
                    if (ticket.Tags != null && ticket.Tags.Count > 0)
                    {
                        taggedTickets++;
                        foreach (var tag in ticket.Tags)
                        {
                            var clean = tag?.Trim().ToLower();
                            if (!string.IsNullOrEmpty(clean))
                            {
                                tagsSet.Add(clean);
                            }
                        }
                    }
                }
                
                _logger.LogInformation("page={Page} -> leídos={Total} | conTags={Tagged} | tagsUnicos={Unique}", 
                    page, totalTickets, taggedTickets, tagsSet.Count);
                
                if (tickets.Count < PER_PAGE)
                {
                    _logger.LogInformation("   Última página (< {PerPage} resultados)", PER_PAGE);
                    break;
                }
                
                // Rate limiting: delay entre páginas
                if (page < MAX_PAGES && !stop)
                    await Task.Delay(200, ct);
            }
            
            _logger.LogInformation("✅ RESUMEN:");
            _logger.LogInformation("   Tickets leídos: {Total}", totalTickets);
            _logger.LogInformation("   Tickets con tags: {Tagged}", taggedTickets);
            _logger.LogInformation("   Tags únicos: {Unique}", tagsSet.Count);
            
            // Insertar/actualizar tags en la base de datos
            int rowsAffected = 0;
            if (tagsSet.Count > 0)
            {
                var now = DateTime.UtcNow;
                foreach (var tagName in tagsSet)
                {
                    var existing = await _db.FreshdeskTags.FindAsync(new object[] { tagName }, ct);
                    if (existing == null)
                    {
                        _db.FreshdeskTags.Add(new Domain.Freshdesk.FreshdeskTag
                        {
                            Name = tagName,
                            Source = "freshdesk_api",
                            LastSeenAt = now
                        });
                        rowsAffected++;
                    }
                    else if (existing.LastSeenAt < now)
                    {
                        existing.LastSeenAt = now;
                        existing.Source = "freshdesk_api";
                        rowsAffected++;
                    }
                }
                
                await _db.SaveChangesAsync(ct);
            }
            
            var totalTagsInDb = await _db.FreshdeskTags.CountAsync(ct);
            
            var response = new FreshdeskTagsSyncResponse
            {
                Success = true,
                Message = $"Sync desde API completado. {totalTickets} tickets leídos, {tagsSet.Count} tags únicos encontrados.",
                RowsAffected = rowsAffected,
                TotalTags = totalTagsInDb,
                SyncedAt = syncStarted
            };
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error durante sync de tags desde API Freshdesk");
            
            return StatusCode(500, new FreshdeskTagsSyncResponse
            {
                Success = false,
                Message = $"Error durante la sincronización: {ex.Message}",
                RowsAffected = 0,
                TotalTags = 0,
                SyncedAt = DateTime.UtcNow
            });
        }
    }

    /// <summary>Diagnóstico de tags: muestra estadísticas de tickets y tags en la tabla cache.</summary>
    [HttpGet("tags/diagnostics")]
    public async Task<ActionResult> GetTagsDiagnostics(CancellationToken ct)
    {
        try
        {
            // Total de tickets en cache
            var totalTicketsSql = "SELECT COUNT(*) FROM pss_dvnx.freshdesk_ticket_header";
            var totalTickets = await _db.Database.SqlQuery<long>($"{totalTicketsSql}").FirstOrDefaultAsync(ct);

            // Tickets con tags (tags IS NOT NULL y no es array vacío)
            var ticketsWithTagsSql = @"
                SELECT COUNT(*) 
                FROM pss_dvnx.freshdesk_ticket_header 
                WHERE tags IS NOT NULL 
                  AND jsonb_array_length(tags) > 0";
            var ticketsWithTags = await _db.Database.SqlQuery<long>($"{ticketsWithTagsSql}").FirstOrDefaultAsync(ct);

            // Tags únicos en toda la tabla
            var uniqueTagsSql = @"
                SELECT COUNT(DISTINCT lower(trim(tag_text))) 
                FROM pss_dvnx.freshdesk_ticket_header h
                CROSS JOIN LATERAL jsonb_array_elements_text(coalesce(h.tags, '[]'::jsonb)) as tag_text
                WHERE trim(tag_text) <> ''";
            var uniqueTags = await _db.Database.SqlQuery<long>($"{uniqueTagsSql}").FirstOrDefaultAsync(ct);

            // Tickets de 2024 (filtro del script PS)
            var tickets2024Sql = @"
                SELECT COUNT(*) 
                FROM pss_dvnx.freshdesk_ticket_header 
                WHERE updated_at >= '2024-01-01T00:00:00Z'::timestamptz
                  AND updated_at < '2025-01-01T00:00:00Z'::timestamptz";
            var tickets2024 = await _db.Database.SqlQuery<long>($"{tickets2024Sql}").FirstOrDefaultAsync(ct);

            // Tickets de 2024 CON tags
            var tickets2024WithTagsSql = @"
                SELECT COUNT(*) 
                FROM pss_dvnx.freshdesk_ticket_header 
                WHERE updated_at >= '2024-01-01T00:00:00Z'::timestamptz
                  AND updated_at < '2025-01-01T00:00:00Z'::timestamptz
                  AND tags IS NOT NULL 
                  AND jsonb_array_length(tags) > 0";
            var tickets2024WithTags = await _db.Database.SqlQuery<long>($"{tickets2024WithTagsSql}").FirstOrDefaultAsync(ct);

            // Tags únicos de 2024 (filtro del script PS)
            var uniqueTags2024Sql = @"
                SELECT COUNT(DISTINCT lower(trim(tag_text))) 
                FROM pss_dvnx.freshdesk_ticket_header h
                CROSS JOIN LATERAL jsonb_array_elements_text(coalesce(h.tags, '[]'::jsonb)) as tag_text
                WHERE trim(tag_text) <> ''
                  AND h.updated_at >= '2024-01-01T00:00:00Z'::timestamptz
                  AND h.updated_at < '2025-01-01T00:00:00Z'::timestamptz";
            var uniqueTags2024 = await _db.Database.SqlQuery<long>($"{uniqueTags2024Sql}").FirstOrDefaultAsync(ct);

            // Rango de fechas en la tabla
            var dateRangeSql = @"
                SELECT 
                    MIN(updated_at) as min_date,
                    MAX(updated_at) as max_date
                FROM pss_dvnx.freshdesk_ticket_header";
            
            using var conn = _db.Database.GetDbConnection();
            await conn.OpenAsync(ct);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = dateRangeSql;
            using var reader = await cmd.ExecuteReaderAsync(ct);
            
            DateTime? minDate = null;
            DateTime? maxDate = null;
            if (await reader.ReadAsync(ct))
            {
                minDate = reader.IsDBNull(0) ? null : reader.GetDateTime(0);
                maxDate = reader.IsDBNull(1) ? null : reader.GetDateTime(1);
            }

            var result = new
            {
                TotalTicketsInCache = totalTickets,
                TicketsWithTags = ticketsWithTags,
                UniqueTagsAllTime = uniqueTags,
                
                Tickets2024 = tickets2024,
                Tickets2024WithTags = tickets2024WithTags,
                UniqueTags2024 = uniqueTags2024,
                
                DateRange = new
                {
                    Min = minDate,
                    Max = maxDate
                },
                
                Comparison = new
                {
                    PowerShellScriptExpected = "Verificar contra tu script PS",
                    Note = "Si UniqueTags2024 no coincide con tu script, falta sincronizar tickets desde Freshdesk API"
                }
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en diagnóstico de tags");
            return StatusCode(500, new { Error = ex.Message, Details = ex.ToString() });
        }
    }
}
