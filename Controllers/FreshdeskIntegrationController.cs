using GestionTime.Api.Contracts.Integrations;
using GestionTime.Infrastructure.Persistence;
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
    private readonly ILogger<FreshdeskIntegrationController> _logger;

    public FreshdeskIntegrationController(
        GestionTimeDbContext db,
        ILogger<FreshdeskIntegrationController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>Sincronización manual de tags de Freshdesk desde la vista de tickets cacheados.</summary>
    /// <remarks>
    /// Ejecuta un UPSERT en la tabla pss_dvnx.freshdesk_tags usando los tags extraídos de v_freshdesk_ticket_full.
    /// 
    /// - Normaliza tags (lower/trim)
    /// - Evita duplicados (clave: name)
    /// - Actualiza last_seen_at solo si el nuevo valor es más reciente
    /// - Fuente: 'ticket_cache'
    /// 
    /// Requiere rol ADMIN.
    /// </remarks>
    [HttpPost("tags")]
    public async Task<ActionResult<FreshdeskTagsSyncResponse>> SyncTags(CancellationToken ct)
    {
        _logger.LogInformation("🔄 Iniciando sincronización manual de tags de Freshdesk...");
        
        try
        {
            var syncStarted = DateTime.UtcNow;
            
            // SQL de UPSERT (SIN punto y coma final - EF Core lo añade automáticamente)
            // Extrae tags desde freshdesk_ticket_header.tags (JSONB)
            var upsertSql = @"
insert into pss_dvnx.freshdesk_tags (name, source, last_seen_at)
select
  left(lower(trim(tag_text)), 100) as name,
  'ticket_cache' as source,
  max(h.updated_at) as last_seen_at
from pss_dvnx.freshdesk_ticket_header h
cross join lateral jsonb_array_elements_text(
  coalesce(h.tags, '[]'::jsonb)
) as tag_text
where tag_text is not null
  and trim(tag_text) <> ''
group by left(lower(trim(tag_text)), 100)
on conflict (name) do update
set
  source = excluded.source,
  last_seen_at = greatest(pss_dvnx.freshdesk_tags.last_seen_at, excluded.last_seen_at)";

            // Ejecutar UPSERT
            var rowsAffected = await _db.Database.ExecuteSqlRawAsync(upsertSql, ct);
            
            _logger.LogInformation("✅ UPSERT completado: {RowsAffected} filas afectadas", rowsAffected);
            
            // Obtener total de tags después del sync
            var totalTagsSql = "SELECT count(*) FROM pss_dvnx.freshdesk_tags";
            var totalTags = await _db.Database.SqlQueryRaw<int>(totalTagsSql).FirstOrDefaultAsync(ct);
            
            _logger.LogInformation("📊 Total de tags en tabla: {TotalTags}", totalTags);
            
            var response = new FreshdeskTagsSyncResponse
            {
                Success = true,
                Message = $"Sincronización completada exitosamente. {rowsAffected} tags procesados.",
                RowsAffected = rowsAffected,
                TotalTags = totalTags,
                SyncedAt = syncStarted
            };
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error durante la sincronización de tags");
            
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
}
