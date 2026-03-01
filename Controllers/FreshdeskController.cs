using GestionTime.Api.Services;
using GestionTime.Domain.Freshdesk;
using GestionTime.Infrastructure.Services.Freshdesk;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GestionTime.Api.Controllers;

[ApiController]
[Route("api/v1/freshdesk")]
public class FreshdeskController : ControllerBase
{
    private readonly FreshdeskService _freshdeskService;
    private readonly FreshdeskClient _freshdeskClient;
    private readonly FreshdeskTicketHeaderSyncService _ticketHeaderSyncService;
    private readonly FreshdeskCompaniesSyncService _companiesSyncService;
    private readonly FreshdeskAgentMeSyncService _agentMeSyncService;
    private readonly FreshdeskAgentsSyncService _agentsSyncService;
    private readonly FreshdeskTicketSuggestService _ticketSuggestService;
    private readonly ILogger<FreshdeskController> _logger;

    public FreshdeskController(
        FreshdeskService freshdeskService,
        FreshdeskClient freshdeskClient,
        FreshdeskTicketHeaderSyncService ticketHeaderSyncService,
        FreshdeskCompaniesSyncService companiesSyncService,
        FreshdeskAgentMeSyncService agentMeSyncService,
        FreshdeskAgentsSyncService agentsSyncService,
        FreshdeskTicketSuggestService ticketSuggestService,
        ILogger<FreshdeskController> logger)
    {
        _freshdeskService = freshdeskService;
        _freshdeskClient = freshdeskClient;
        _ticketHeaderSyncService = ticketHeaderSyncService;
        _companiesSyncService = companiesSyncService;
        _agentMeSyncService = agentMeSyncService;
        _agentsSyncService = agentsSyncService;
        _ticketSuggestService = ticketSuggestService;
        _logger = logger;
    }
    
    /// <summary>
    /// 🏓 Ping a Freshdesk - Verifica conexión y credenciales (PÚBLICO - no requiere login)
    /// </summary>
    [HttpGet("ping")]
    [AllowAnonymous]
    public async Task<IActionResult> Ping(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("🏓 Endpoint /ping llamado");
            
            var (success, statusCode, agentEmail, error) = await _freshdeskClient.PingAsync(ct);
            
            if (success)
            {
                return Ok(new
                {
                    ok = true,
                    status = statusCode,
                    message = "✅ Conexión exitosa con Freshdesk",
                    agent = agentEmail,
                    timestamp = DateTime.UtcNow
                });
            }
            else
            {
                return Ok(new
                {
                    ok = false,
                    status = statusCode,
                    message = "❌ Error al conectar con Freshdesk",
                    error = error,
                    timestamp = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error inesperado en ping");
            return StatusCode(500, new
            {
                ok = false,
                status = 500,
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }
    
    /// <summary>
    /// 🎯 Buscar tickets desde la VISTA local (v_freshdesk_ticket_company_min) - Para Desktop
    /// GET /api/v1/freshdesk/tickets/search-from-view?agentId=&amp;ticket=&amp;customer=&amp;limit=10
    /// </summary>
    /// <param name="agentId">ID del agente asignado (opcional)</param>
    /// <param name="ticket">Prefijo del ticket ID (opcional)</param>
    /// <param name="customer">Parte del nombre del cliente (opcional)</param>
    /// <param name="limit">Límite de resultados (default 10, max 50)</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpGet("tickets/search-from-view")]
    [Authorize]
    public async Task<IActionResult> SearchTicketsFromView(
        [FromQuery] long? agentId,
        [FromQuery] string? ticket,
        [FromQuery] string? customer,
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
    {
        try
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            
            _logger.LogInformation("🎯 SearchTicketsFromView - User: {Email}, agentId: {AgentId}, ticket: {Ticket}, customer: {Customer}, limit: {Limit}",
                userEmail ?? "unknown",
                agentId?.ToString() ?? "null",
                ticket ?? "null",
                customer ?? "null",
                limit);

            // Llamar al servicio que consulta la vista SQL
            var results = await _ticketSuggestService.SuggestAsync(
                agentId: agentId,
                ticket: ticket,
                customer: customer,
                limit: limit,
                ct: ct);

            return Ok(new
            {
                success = true,
                count = results.Count,
                tickets = results
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al buscar tickets desde vista");
            return StatusCode(500, new
            {
                success = false,
                message = "Error al buscar tickets desde base de datos local",
                error = ex.Message
            });
        }
    }
    
    /// <summary>
    /// ⚠️ DEPRECADO: Usar /api/v1/tags/suggest en su lugar
    /// </summary>
    [HttpGet("tags/suggest")]
    [Obsolete("Este endpoint está deprecado. Use /api/v1/tags/suggest")]
    [ApiExplorerSettings(IgnoreApi = true)] // Ocultar de Swagger
    public IActionResult SuggestTagsDeprecated()
    {
        _logger.LogWarning("⚠️ Endpoint deprecado /api/v1/freshdesk/tags/suggest fue llamado");
        
        return StatusCode(410, new
        {
            success = false,
            message = "Este endpoint está deprecado. Use /api/v1/tags/suggest en su lugar.",
            deprecatedSince = "2026-02-01",
            newEndpoint = "/api/v1/tags/suggest"
        });
    }
    
    
    /// <summary>
    /// ⚠️ DEPRECADO: Usar POST /api/v1/integrations/freshdesk/sync/tags en su lugar
    /// </summary>
    [HttpPost("tags/sync")]
    [Obsolete("Este endpoint está deprecado. Use POST /api/v1/integrations/freshdesk/sync/tags")]
    [ApiExplorerSettings(IgnoreApi = true)] // Ocultar de Swagger
    public IActionResult SyncTagsDeprecated()
    {
        _logger.LogWarning("⚠️ Endpoint deprecado /api/v1/freshdesk/tags/sync fue llamado");
        
        return StatusCode(410, new
        {
            success = false,
            message = "Este endpoint está deprecado. Use POST /api/v1/integrations/freshdesk/sync/tags en su lugar.",
            deprecatedSince = "2026-02-01",
            newEndpoint = "/api/v1/integrations/freshdesk/sync/tags",
            reason = "El nuevo endpoint usa UPSERT directo a la vista de tickets cacheados en lugar de consultar la API de Freshdesk."
        });
    }
    
    // NOTA: El código antiguo de SyncTags fue removido completamente.
    // La nueva implementación está en FreshdeskIntegrationController.
    // Endpoint nuevo: POST /api/v1/integrations/freshdesk/sync/tags
    
    /// <summary>
    /// 🔄 Sincronizar ticket headers desde Freshdesk - Solo ADMIN
    /// POST /api/v1/integrations/freshdesk/sync/ticket-headers?full=true
    /// </summary>
    /// <param name="full">True para sincronización completa, False para incremental</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpPost("/api/v1/integrations/freshdesk/sync/ticket-headers")]
    [Authorize(Roles = "Admin,ADMIN")]
    public async Task<IActionResult> SyncTicketHeaders(
        [FromQuery] bool full = false,
        CancellationToken ct = default)
    {
        try
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            
            _logger.LogInformation("🔄 Iniciando sincronización MANUAL de ticket headers");
            _logger.LogInformation("   👤 Usuario: {Email}", userEmail);
            _logger.LogInformation("   📊 Modo: {Mode}", full ? "FULL" : "INCREMENTAL");

            var result = await _ticketHeaderSyncService.SyncAsync(full, ct);

            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    message = $"✅ Sincronización completada en {result.DurationMs}ms",
                    mode = result.Mode,
                    metrics = new
                    {
                        ticketsScanned = result.TicketsScanned,
                        ticketsUpserted = result.TagsInserted,
                        durationMs = result.DurationMs
                    },
                    startedAt = result.StartedAt,
                    completedAt = result.CompletedAt
                });
            }
            else
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "❌ Error en sincronización",
                    error = result.Error,
                    metrics = new
                    {
                        ticketsScanned = result.TicketsScanned,
                        durationMs = result.DurationMs
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al sincronizar ticket headers");
            return StatusCode(500, new
            {
                success = false,
                message = "Error al sincronizar ticket headers desde Freshdesk",
                error = ex.Message
            });
        }
    }
    
    /// <summary>
    /// 📊 Obtener estado de sincronización de ticket headers
    /// GET /api/v1/integrations/freshdesk/sync/status
    /// </summary>
    [HttpGet("/api/v1/integrations/freshdesk/sync/status")]
    [Authorize]
    public async Task<IActionResult> GetSyncStatus(CancellationToken ct = default)
    {
        try
        {
            var state = await _ticketHeaderSyncService.GetStatusAsync(ct);

            if (state == null)
            {
                return Ok(new
                {
                    success = true,
                    message = "No se ha ejecutado ninguna sincronización aún",
                    state = (object?)null
                });
            }

            return Ok(new
            {
                success = true,
                state = new
                {
                    scope = state.Scope,
                    lastSyncAt = state.LastSyncAt,
                    lastResultCount = state.LastResultCount,
                    lastMaxUpdatedAt = state.LastMaxUpdatedAt,
                    lastUpdatedSince = state.LastUpdatedSince,
                    lastError = state.LastError
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al obtener estado de sincronización");
            return StatusCode(500, new
            {
                success = false,
                message = "Error al obtener estado de sincronización",
                error = ex.Message
            });
        }
    }
    
    /// <summary>
    /// 🏢 Sincronizar companies desde Freshdesk - Solo ADMIN
    /// POST /api/v1/integrations/freshdesk/sync/companies
    /// </summary>
    [HttpPost("/api/v1/integrations/freshdesk/sync/companies")]
    [Authorize(Roles = "Admin,ADMIN")]
    public async Task<IActionResult> SyncCompanies(CancellationToken ct = default)
    {
        try
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            
            _logger.LogInformation("🏢 Iniciando sincronización MANUAL de companies");
            _logger.LogInformation("   👤 Usuario: {Email}", userEmail);

            var result = await _companiesSyncService.SyncAllAsync(ct);

            return Ok(new
            {
                success = true,
                message = $"✅ Sincronización completada en {result.DurationMs}ms",
                pagesFetched = result.PagesFetched,
                companiesUpserted = result.CompaniesUpserted,
                durationMs = result.DurationMs,
                sampleFirst3 = result.SampleFirst3.Select(s => new
                {
                    company_id = s.CompanyId,
                    name = s.Name
                }).ToList(),
                startedAt = result.StartedAt,
                completedAt = result.CompletedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al sincronizar companies");
            return StatusCode(500, new
            {
                success = false,
                message = "Error al sincronizar companies desde Freshdesk",
                error = ex.Message
            });
        }
    }
    
    /// <summary>
    /// 📊 Obtener estado de sincronización de companies
    /// GET /api/v1/integrations/freshdesk/sync/companies/status
    /// </summary>
    [HttpGet("/api/v1/integrations/freshdesk/sync/companies/status")]
    [Authorize]
    public async Task<IActionResult> GetCompaniesStatus(CancellationToken ct = default)
    {
        try
        {
            var status = await _companiesSyncService.GetStatusAsync(ct);

            return Ok(new
            {
                success = true,
                totalCompanies = status.TotalCompanies,
                maxUpdatedAt = status.MaxUpdatedAt,
                maxSyncedAt = status.MaxSyncedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al obtener status de companies");
            return StatusCode(500, new
            {
                success = false,
                message = "Error al obtener status de companies",
                error = ex.Message
            });
        }
    }
    
    /// <summary>
    /// 👤 Sincronizar agente actual (me) desde Freshdesk - Requiere autenticación
    /// POST /api/v1/integrations/freshdesk/agent-me/sync
    /// </summary>
    [HttpPost("/api/v1/integrations/freshdesk/agent-me/sync")]
    [Authorize]
    public async Task<IActionResult> SyncAgentMe(CancellationToken ct = default)
    {
        try
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            
            _logger.LogInformation("👤 Iniciando sincronización del agente actual (me)");
            _logger.LogInformation("   👤 Usuario: {Email}", userEmail);

            var result = await _agentMeSyncService.SyncAsync(ct);

            return Ok(new
            {
                success = result.Success,
                agent_id = result.AgentId,
                agent_email = result.AgentEmail,
                freshdesk_updated_at = result.FreshdeskUpdatedAt,
                synced_at = result.CompletedAt,
                durationMs = result.DurationMs
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al sincronizar agente actual");
            return StatusCode(500, new
            {
                success = false,
                message = "Error al sincronizar agente actual desde Freshdesk",
                error = ex.Message
            });
        }
    }
    
    /// <summary>
    /// 👤 Obtener agente actual desde cache - Requiere autenticación
    /// GET /api/v1/integrations/freshdesk/agent-me
    /// </summary>
    [HttpGet("/api/v1/integrations/freshdesk/agent-me")]
    [Authorize]
    public async Task<IActionResult> GetAgentMe(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("👤 Obteniendo agente actual desde cache");

            var agent = await _agentMeSyncService.GetCachedAsync(ct);

            if (agent == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "No se encontró información del agente actual en cache. Ejecute POST /agent-me/sync primero."
                });
            }

            return Ok(new
            {
                success = true,
                agent = new
                {
                    agent_id = agent.AgentId,
                    agent_email = agent.AgentEmail,
                    agent_name = agent.AgentName,
                    agent_type = agent.AgentType,
                    is_active = agent.IsActive,
                    language = agent.Language,
                    time_zone = agent.TimeZone,
                    mobile = agent.Mobile,
                    phone = agent.Phone,
                    last_login_at = agent.LastLoginAt,
                    freshdesk_created_at = agent.FreshdeskCreatedAt,
                    freshdesk_updated_at = agent.FreshdeskUpdatedAt,
                    synced_at = agent.SyncedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al obtener agente actual desde cache");
            return StatusCode(500, new
            {
                success = false,
                message = "Error al obtener agente actual desde cache",
                error = ex.Message
            });
        }
    }
    
    /// <summary>
    /// 👥 Sincronizar todos los agentes desde Freshdesk - Solo ADMIN
    /// POST /api/v1/integrations/freshdesk/agents/sync
    /// </summary>
    [HttpPost("/api/v1/integrations/freshdesk/agents/sync")]
    [Authorize(Roles = "Admin,ADMIN")]
    public async Task<IActionResult> SyncAgents(CancellationToken ct = default)
    {
        try
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            
            _logger.LogInformation("👥 Iniciando sincronización de todos los agentes");
            _logger.LogInformation("   👤 Usuario: {Email}", userEmail);

            var result = await _agentsSyncService.SyncAllAsync(ct);

            return Ok(new
            {
                success = result.Success,
                pagesFetched = result.PagesFetched,
                agentsUpserted = result.AgentsUpserted,
                durationMs = result.DurationMs,
                sampleFirst3 = result.SampleFirst3.Select(s => new
                {
                    agent_id = s.AgentId,
                    name = s.Name,
                    email = s.Email
                }).ToList(),
                startedAt = result.StartedAt,
                completedAt = result.CompletedAt,
                error = result.Error
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al sincronizar agentes");
            return StatusCode(500, new
            {
                success = false,
                message = "Error al sincronizar agentes desde Freshdesk",
                error = ex.Message
            });
        }
    }
    
    /// <summary>
    /// 📊 Obtener estado de sincronización de agentes
    /// GET /api/v1/integrations/freshdesk/agents/status
    /// </summary>
    [HttpGet("/api/v1/integrations/freshdesk/agents/status")]
    [Authorize]
    public async Task<IActionResult> GetAgentsStatus(CancellationToken ct = default)
    {
        try
        {
            var status = await _agentsSyncService.GetStatusAsync(ct);

            return Ok(new
            {
                success = true,
                totalAgents = status.TotalAgents,
                activeAgents = status.ActiveAgents,
                maxUpdatedAt = status.MaxUpdatedAt,
                maxSyncedAt = status.MaxSyncedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al obtener status de agentes");
            return StatusCode(500, new
            {
                success = false,
                message = "Error al obtener status de agentes",
                error = ex.Message
            });
        }
    }
}





