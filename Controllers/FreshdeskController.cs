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
    private readonly ILogger<FreshdeskController> _logger;

    public FreshdeskController(
        FreshdeskService freshdeskService,
        FreshdeskClient freshdeskClient,
        ILogger<FreshdeskController> logger)
    {
        _freshdeskService = freshdeskService;
        _freshdeskClient = freshdeskClient;
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
    /// 🎫 Buscar tickets en Freshdesk - Requiere autenticación
    /// GET /api/v1/freshdesk/tickets/suggest?term=&amp;limit=10&amp;includeUnassigned=true
    /// </summary>
    [HttpGet("tickets/suggest")]
    [Authorize]
    public async Task<IActionResult> SuggestTickets(
        [FromQuery] string? term,
        [FromQuery] int limit = 10,
        [FromQuery] bool includeUnassigned = true,
        CancellationToken ct = default)
    {
        try
        {
            // Obtener email desde JWT
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            if (string.IsNullOrEmpty(userEmail) || string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("❌ Email o UserId no encontrado en JWT");
                return Unauthorized(new { 
                    success = false, 
                    message = "Email no encontrado en token de autenticación" 
                });
            }

            _logger.LogInformation("🎫 SuggestTickets - User: {Email}, term: {Term}, limit: {Limit}, includeUnassigned: {IncludeUnassigned}", 
                userEmail, term ?? "ninguno", limit, includeUnassigned);

            // Resolver agentId desde email (con cache)
            if (!Guid.TryParse(userId, out var userGuid))
            {
                _logger.LogError("❌ UserId inválido: {UserId}", userId);
                return Unauthorized(new
                {
                    success = false,
                    message = "UserId inválido en token de autenticación"
                });
            }
            
            var agentId = await _freshdeskService.ResolveAgentIdByEmailAsync(userGuid, userEmail, ct);
            
            if (!agentId.HasValue)
            {
                _logger.LogWarning("⚠️ No se encontró agentId para {Email}", userEmail);
                return Ok(new
                {
                    success = false,
                    message = "Usuario no encontrado como agente en Freshdesk",
                    count = 0,
                    tickets = new List<object>()
                });
            }

            // Construir query base (agente + term)
            var baseQueryParts = new List<string>();
            
            // Filtro de agente
            if (includeUnassigned)
            {
                baseQueryParts.Add($"(agent_id:{agentId.Value} OR agent_id:null)");
            }
            else
            {
                baseQueryParts.Add($"agent_id:{agentId.Value}");
            }
            
            // Filtro de búsqueda por término (con sanitización)
            if (!string.IsNullOrWhiteSpace(term))
            {
                var safeTerm = term.Trim();
                
                // Si es numérico, buscar por ID
                if (long.TryParse(safeTerm, out var ticketId))
                {
                    baseQueryParts.Add($"id:{ticketId}");
                }
                else
                {
                    // Sanitizar: escapar comillas simples para query Freshdesk
                    safeTerm = safeTerm.Replace("'", "\\'");
                    
                    // Si es texto, buscar en subject y description
                    baseQueryParts.Add($"(subject:'{safeTerm}' OR description:'{safeTerm}')");
                }
            }
            
            var baseQuery = string.Join(" AND ", baseQueryParts);
            
            // Estrategia: 2 queries separadas para priorizar "Open" primero
            // Query 1: Status Open/Pending/Waiting (2,3,6,7)
            // Query 2: Status Resolved/Closed (4,5)
            
            var allTickets = new List<FreshdeskTicketDto>();
            
            // Query 1: Tickets abiertos/pendientes (PRIORIDAD ALTA)
            var openStatuses = new[] { 2, 3, 6, 7 }; // Open, Pending, Waiting on Customer, Waiting on Third Party
            var openQuery = $"{baseQuery} AND (status:{string.Join(" OR status:", openStatuses)})";
            
            _logger.LogInformation("📝 Query 1 (Open): {Query}", openQuery);
            
            try
            {
                var openResult = await _freshdeskClient.SearchTicketsAsync(openQuery, 1, ct);
                allTickets.AddRange(openResult.Results);
                _logger.LogInformation("   ✅ Query 1: {Count} tickets abiertos encontrados", openResult.Results.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Error en Query 1 (Open tickets)");
            }
            
            // Query 2: Tickets cerrados/resueltos (PRIORIDAD BAJA)
            var remainingLimit = limit - allTickets.Count;
            
            if (remainingLimit > 0)
            {
                var closedStatuses = new[] { 4, 5 }; // Resolved, Closed
                var closedQuery = $"{baseQuery} AND (status:{string.Join(" OR status:", closedStatuses)})";
                
                _logger.LogInformation("📝 Query 2 (Closed): {Query}", closedQuery);
                
                try
                {
                    var closedResult = await _freshdeskClient.SearchTicketsAsync(closedQuery, 1, ct);
                    allTickets.AddRange(closedResult.Results.Take(remainingLimit));
                    _logger.LogInformation("   ✅ Query 2: {Count} tickets cerrados encontrados", closedResult.Results.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ Error en Query 2 (Closed tickets)");
                }
            }
            
            // Tomar solo el límite solicitado y mapear a DTO
            var tickets = allTickets
                .Take(Math.Min(limit, 50))
                .Select(t => new
                {
                    id = t.Id,
                    subject = t.Subject,
                    status = t.Status,           // Numérico para lógica
                    statusName = t.StatusName,   // Solo para display
                    priority = t.Priority,
                    priorityName = t.PriorityName,
                    updatedAt = t.UpdatedAt
                })
                .ToList();
            
            _logger.LogInformation("📊 Total tickets devueltos: {Count} (Open: {Open}, Closed: {Closed})", 
                tickets.Count,
                tickets.Count(t => new[] { 2, 3, 6, 7 }.Contains(t.status)),
                tickets.Count(t => new[] { 4, 5 }.Contains(t.status)));

            return Ok(new
            {
                success = true,
                count = tickets.Count,
                tickets = tickets
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al buscar tickets");
            return StatusCode(500, new
            {
                success = false,
                message = "Error al buscar tickets en Freshdesk",
                error = ex.Message
            });
        }
    }
    
    /// <summary>
    /// 🎯 Super Filtro de Tickets - Buscar con priorización configurable
    /// GET /api/v1/freshdesk/tickets/suggest-filtered?term=&amp;limit=10&amp;includeUnassigned=true&amp;openFirst=true
    /// </summary>
    [HttpGet("tickets/suggest-filtered")]
    [Authorize]
    public async Task<IActionResult> SuggestTicketsFiltered(
        [FromQuery] string? term,
        [FromQuery] int limit = 10,
        [FromQuery] bool includeUnassigned = true,
        [FromQuery] bool openFirst = true,
        CancellationToken ct = default)
    {
        try
        {
            // Validar y limitar parámetros
            limit = Math.Min(limit, 20); // Cap máximo: 20
            
            // Obtener email y userId desde JWT
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            if (string.IsNullOrEmpty(userEmail) || string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("❌ Email o UserId no encontrado en JWT");
                return Unauthorized(new
                {
                    success = false,
                    message = "Credenciales no encontradas en token de autenticación"
                });
            }
            
            // Validar y parsear userId
            if (!Guid.TryParse(userId, out var userGuid))
            {
                _logger.LogError("❌ UserId inválido: {UserId}", userId);
                return Unauthorized(new
                {
                    success = false,
                    message = "UserId inválido en token de autenticación"
                });
            }
            
            // Sanitizar term
            string? safeTerm = null;
            if (!string.IsNullOrWhiteSpace(term))
            {
                safeTerm = term.Trim();
                
                // Limitar a 80 caracteres
                if (safeTerm.Length > 80)
                {
                    safeTerm = safeTerm.Substring(0, 80);
                }
                
                // Escapar comillas simples
                safeTerm = safeTerm.Replace("'", "\\'");
            }
            
            _logger.LogInformation("🎯 SuggestTicketsFiltered - User: {Email}, term: '{Term}' (len:{Len}), limit: {Limit}, includeUnassigned: {IncludeUnassigned}, openFirst: {OpenFirst}",
                userEmail, safeTerm ?? "ninguno", safeTerm?.Length ?? 0, limit, includeUnassigned, openFirst);
            
            // Resolver agentId desde email (con cache de 24h)
            var agentId = await _freshdeskService.ResolveAgentIdByEmailAsync(userGuid, userEmail, ct);
            
            if (!agentId.HasValue)
            {
                _logger.LogWarning("⚠️ No se encontró agentId para {Email}", userEmail);
                return Ok(new
                {
                    success = false,
                    message = "Usuario no encontrado como agente en Freshdesk",
                    count = 0,
                    tickets = new List<object>()
                });
            }
            
            _logger.LogInformation("   ✅ AgentId resuelto: {AgentId}", agentId.Value);
            
            // Construir filtro base de asignación
            string assignFilter;
            if (includeUnassigned)
            {
                assignFilter = $"(agent_id:{agentId.Value} OR agent_id:null)";
            }
            else
            {
                assignFilter = $"agent_id:{agentId.Value}";
            }
            
            // Construir filtro de término
            string? termFilter = null;
            if (!string.IsNullOrEmpty(safeTerm))
            {
                if (long.TryParse(safeTerm, out var ticketId))
                {
                    // Es numérico: buscar por ID
                    termFilter = $"id:{ticketId}";
                }
                else
                {
                    // Es texto: buscar en subject y description
                    termFilter = $"(subject:'{safeTerm}' OR description:'{safeTerm}')";
                }
            }
            
            var allTickets = new List<FreshdeskTicketDto>();
            
            if (openFirst)
            {
                // Estrategia: 2 queries separadas (Open primero, luego Closed)
                
                // Query 1: Tickets OPEN (status=2)
                var queryPartsOpen = new List<string> { assignFilter, "status:2" };
                if (termFilter != null)
                {
                    queryPartsOpen.Add(termFilter);
                }
                var queryOpen = string.Join(" AND ", queryPartsOpen);
                
                _logger.LogInformation("   📝 Query Open: {Query}", queryOpen);
                
                try
                {
                    var openTickets = await _freshdeskClient.SearchAllTicketsAsync(queryOpen, limit, ct);
                    allTickets.AddRange(openTickets);
                    _logger.LogInformation("   ✅ Tickets Open: {Count}", openTickets.Count);
                }
                catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized || 
                                                       ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    _logger.LogError(ex, "❌ Freshdesk auth failed");
                    return StatusCode(502, new
                    {
                        success = false,
                        message = "Freshdesk authentication failed"
                    });
                }
                catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning(ex, "⚠️ Freshdesk rate limited");
                    return StatusCode(503, new
                    {
                        success = false,
                        message = "Freshdesk rate limited. Please try again later."
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ Error al obtener tickets Open");
                }
                
                // Query 2: Tickets RESOLVED/CLOSED (status=4,5) - solo si no alcanzamos el límite
                var remainingLimit = limit - allTickets.Count;
                if (remainingLimit > 0)
                {
                    var queryPartsClosed = new List<string> { assignFilter, "(status:4 OR status:5)" };
                    if (termFilter != null)
                    {
                        queryPartsClosed.Add(termFilter);
                    }
                    var queryClosed = string.Join(" AND ", queryPartsClosed);
                    
                    _logger.LogInformation("   📝 Query Closed: {Query}", queryClosed);
                    
                    try
                    {
                        var closedTickets = await _freshdeskClient.SearchAllTicketsAsync(queryClosed, remainingLimit, ct);
                        allTickets.AddRange(closedTickets);
                        _logger.LogInformation("   ✅ Tickets Closed: {Count}", closedTickets.Count);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "⚠️ Error al obtener tickets Closed");
                    }
                }
            }
            else
            {
                // Estrategia: 1 query y ordenar en backend
                var queryParts = new List<string> { assignFilter, "(status:2 OR status:4 OR status:5)" };
                if (termFilter != null)
                {
                    queryParts.Add(termFilter);
                }
                var queryAll = string.Join(" AND ", queryParts);
                
                _logger.LogInformation("   📝 Query All: {Query}", queryAll);
                
                try
                {
                    var tickets = await _freshdeskClient.SearchAllTicketsAsync(queryAll, limit, ct);
                    
                    // Ordenar en backend: Open (2) primero, luego Resolved/Closed (4,5)
                    allTickets = tickets
                        .OrderBy(t => t.Status == 2 ? 0 : 1) // Open primero
                        .ThenByDescending(t => t.UpdatedAt) // Más recientes primero
                        .Take(limit)
                        .ToList();
                    
                    _logger.LogInformation("   ✅ Tickets All (ordenados): {Count}", allTickets.Count);
                }
                catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized || 
                                                       ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    _logger.LogError(ex, "❌ Freshdesk auth failed");
                    return StatusCode(502, new
                    {
                        success = false,
                        message = "Freshdesk authentication failed"
                    });
                }
                catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning(ex, "⚠️ Freshdesk rate limited");
                    return StatusCode(503, new
                    {
                        success = false,
                        message = "Freshdesk rate limited. Please try again later."
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error al obtener tickets");
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "Error al buscar tickets en Freshdesk"
                    });
                }
            }
            
            // Enriquecer tickets con información del cliente y técnico asignado
            _logger.LogInformation("🔍 Enriqueciendo {Count} tickets con información del cliente y técnico...", allTickets.Count);
            
            var companyCache = new Dictionary<long, string>(); // Cache en memoria para esta request
            var agentCache = new Dictionary<long, (string name, string email)>(); // Cache de agentes/técnicos
            var rateLimitReached = false;
            
            foreach (var ticket in allTickets)
            {
                if (rateLimitReached)
                    break;
                
                try
                {
                    // Obtener detalles del ticket con requester
                    var detail = await _freshdeskClient.GetTicketDetailAsync(ticket.Id, ct);
                    
                    if (detail != null)
                    {
                        // === INFORMACIÓN DEL CLIENTE (REQUESTER) ===
                        ticket.RequesterName = detail.Requester?.Name ?? "";
                        ticket.CompanyId = detail.CompanyId;
                        
                        // Si tiene companyId, obtener company name (con cache)
                        if (detail.CompanyId.HasValue)
                        {
                            // Verificar cache primero
                            if (companyCache.TryGetValue(detail.CompanyId.Value, out var cachedCompanyName))
                            {
                                ticket.CompanyName = cachedCompanyName;
                            }
                            else
                            {
                                // No está en cache, obtener de Freshdesk
                                var company = await _freshdeskClient.GetCompanyAsync(detail.CompanyId.Value, ct);
                                if (company != null)
                                {
                                    ticket.CompanyName = company.Name;
                                    companyCache[detail.CompanyId.Value] = company.Name;
                                }
                            }
                        }
                        
                        // === INFORMACIÓN DEL TÉCNICO ASIGNADO (RESPONDER) ===
                        ticket.ResponderId = detail.ResponderId;
                        
                        if (detail.ResponderId.HasValue)
                        {
                            // Verificar cache de agentes primero
                            if (agentCache.TryGetValue(detail.ResponderId.Value, out var cachedAgent))
                            {
                                ticket.TechnicianName = cachedAgent.name;
                                ticket.TechnicianEmail = cachedAgent.email;
                            }
                            else
                            {
                                // No está en cache, obtener de Freshdesk
                                var agent = await _freshdeskClient.GetAgentAsync(detail.ResponderId.Value, ct);
                                if (agent != null)
                                {
                                    // Preferir contact.name si existe, si no usar agent.name
                                    var agentName = !string.IsNullOrEmpty(agent.Contact?.Name) 
                                        ? agent.Contact.Name 
                                        : agent.Name;
                                    
                                    var agentEmail = !string.IsNullOrEmpty(agent.Contact?.Email)
                                        ? agent.Contact.Email
                                        : agent.Email;
                                    
                                    ticket.TechnicianName = agentName;
                                    ticket.TechnicianEmail = agentEmail;
                                    
                                    // Guardar en cache
                                    agentCache[detail.ResponderId.Value] = (agentName, agentEmail);
                                }
                                else
                                {
                                    // No se pudo obtener info del agente
                                    ticket.TechnicianName = "";
                                    ticket.TechnicianEmail = "";
                                }
                            }
                        }
                        else
                        {
                            // Ticket sin asignar
                            ticket.TechnicianName = "";
                            ticket.TechnicianEmail = "";
                        }
                    }
                }
                catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning("⚠️ Rate limit alcanzado durante enriquecimiento. Deteniendo enriquecimiento.");
                    rateLimitReached = true;
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ Error al enriquecer ticket {TicketId}. Continuando con siguiente.", ticket.Id);
                    // Continuar con el siguiente ticket sin romper
                }
            }
            
            _logger.LogInformation("✅ Enriquecimiento completado. Companies: {CompanyCache}, Agents: {AgentCache}", 
                companyCache.Count, agentCache.Count);
            
            // Mapear a DTO con clientName y technicianName
            var ticketsResponse = allTickets
                .Take(limit)
                .Select(t => new
                {
                    id = t.Id,
                    subject = t.Subject,
                    status = t.Status,
                    statusName = t.StatusName,
                    priority = t.Priority,
                    priorityName = t.PriorityName,
                    updatedAt = t.UpdatedAt,
                    clientName = t.ClientName,
                    requesterName = t.RequesterName,
                    companyName = t.CompanyName,
                    responderId = t.ResponderId,
                    technicianName = t.TechnicianName ?? "",
                    technicianEmail = t.TechnicianEmail
                })
                .ToList();
            
            _logger.LogInformation("📊 Total tickets devueltos: {Count} (Open: {Open}, Closed: {Closed}, Asignados: {Assigned})",
                ticketsResponse.Count,
                ticketsResponse.Count(t => t.status == 2),
                ticketsResponse.Count(t => t.status == 4 || t.status == 5),
                ticketsResponse.Count(t => t.responderId.HasValue));


            
            return Ok(new
            {
                success = true,
                count = ticketsResponse.Count,
                tickets = ticketsResponse
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error inesperado en SuggestTicketsFiltered");
            return StatusCode(500, new
            {
                success = false,
                message = "Error interno al procesar la solicitud"
            });
        }
    }
    
    /// <summary>
    /// 📋 Obtener detalles completos de un ticket de Freshdesk
    /// GET /api/v1/freshdesk/tickets/{ticketId}/details
    /// </summary>
    /// <param name="ticketId">ID del ticket en Freshdesk</param>
    [HttpGet("tickets/{ticketId}/details")]
    [Authorize]
    public async Task<IActionResult> GetTicketDetails(
        [FromRoute] int ticketId,
        CancellationToken ct = default)
    {
        try
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            
            _logger.LogInformation("📋 GetTicketDetails - User: {Email}, TicketId: {TicketId}", 
                userEmail ?? "unknown", ticketId);
            
            if (ticketId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "ID de ticket inválido"
                });
            }
            
            var details = await _freshdeskClient.GetTicketDetailsForEditAsync(ticketId, ct);
            
            if (details == null)
            {
                _logger.LogWarning("⚠️ Ticket {TicketId} no encontrado", ticketId);
                return NotFound(new
                {
                    success = false,
                    message = "Ticket no encontrado"
                });
            }
            
            // Mapear a formato exacto solicitado
            var response = new
            {
                id = details.Id,
                subject = details.Subject,
                status = details.Status,
                priority = details.Priority,
                created_at = details.CreatedAt.ToString("o"), // ISO 8601
                updated_at = details.UpdatedAt.ToString("o"), // ISO 8601
                description_text = details.DescriptionText,
                requester = details.Requester != null ? new
                {
                    id = details.Requester.Id,
                    name = details.Requester.Name,
                    email = details.Requester.Email
                } : null,
                company = details.Company != null ? new
                {
                    id = details.Company.Id,
                    name = details.Company.Name
                } : null,
                conversations = details.Conversations.Select(c => new
                {
                    id = c.Id,
                    incoming = c.Incoming,
                    @private = c.Private, // "private" es palabra reservada en C#
                    created_at = c.CreatedAt.ToString("o"),
                    updated_at = c.UpdatedAt.ToString("o"),
                    from_email = c.FromEmail,
                    to_emails = c.ToEmails,
                    cc_emails = c.CcEmails,
                    body_text = c.BodyText
                }).ToList()
            };
            
            _logger.LogInformation("✅ Detalles del ticket {TicketId} obtenidos correctamente ({ConvCount} conversaciones)", 
                ticketId, details.Conversations.Count);
            
            return Ok(response);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized || 
                                               ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            _logger.LogError(ex, "❌ Error de autenticación con Freshdesk para ticket {TicketId}", ticketId);
            return StatusCode(502, new
            {
                success = false,
                message = "Error de autenticación con Freshdesk"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al obtener detalles del ticket {TicketId}", ticketId);
            return StatusCode(502, new
            {
                success = false,
                message = "Error al comunicarse con Freshdesk"
            });
        }
    }
    
    /// <summary>
    /// 🏷️ Buscar tags en caché local - Puede ser público o con autenticación
    /// GET /api/v1/freshdesk/tags/suggest?term=&amp;limit=20
    /// </summary>
    [HttpGet("tags/suggest")]
    [Authorize]
    public async Task<IActionResult> SuggestTags(
        [FromQuery] string? term,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("🏷️ SuggestTags - term: {Term}, limit: {Limit}", term ?? "ninguno", limit);

            var tags = await _freshdeskService.SuggestTagsAsync(term, Math.Min(limit, 50), ct);

            return Ok(new
            {
                success = true,
                count = tags.Count,
                tags = tags
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al buscar tags");
            return StatusCode(500, new
            {
                success = false,
                message = "Error al buscar tags",
                error = ex.Message
            });
        }
    }
    
    
    /// <summary>
    /// 🔄 Sincronizar tags desde Freshdesk - Solo ADMIN
    /// POST /api/v1/freshdesk/tags/sync?mode=recent&amp;days=30&amp;limit=1000
    /// </summary>
    /// <param name="mode">Modo de sincronización: "recent" (últimos N días) o "full" (todos)</param>
    /// <param name="days">Número de días hacia atrás (solo para mode=recent)</param>
    /// <param name="limit">Límite máximo de tickets a procesar</param>
    [HttpPost("tags/sync")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SyncTags(
        [FromQuery] string mode = "recent",
        [FromQuery] int days = 30,
        [FromQuery] int limit = 1000,
        CancellationToken ct = default)
    {
        try
        {
            // Verificar si el endpoint está habilitado
            var syncApiEnabled = Environment.GetEnvironmentVariable("FRESHDESK_TAGS_SYNC_API_ENABLED");
            if (syncApiEnabled != "true")
            {
                _logger.LogWarning("❌ Intento de sincronización cuando FRESHDESK_TAGS_SYNC_API_ENABLED != true");
                return NotFound(new
                {
                    success = false,
                    message = "Endpoint de sincronización deshabilitado",
                    timestamp = DateTime.UtcNow
                });
            }
            
            _logger.LogInformation("🔄 Iniciando sincronización MANUAL de tags");
            _logger.LogInformation("   👤 Usuario: {Email}", User.FindFirstValue(ClaimTypes.Email));
            _logger.LogInformation("   📊 Parámetros: mode={Mode}, days={Days}, limit={Limit}", mode, days, limit);

            // Validaciones
            if (mode != "recent" && mode != "full")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "❌ Modo inválido. Use 'recent' o 'full'",
                    validModes = new[] { "recent", "full" }
                });
            }

            if (days < 1 || days > 365)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "❌ 'days' debe estar entre 1 y 365"
                });
            }

            if (limit < 1 || limit > 5000)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "❌ 'limit' debe estar entre 1 y 5000"
                });
            }

            var result = await _freshdeskService.SyncTagsFromFreshdeskAsync(mode, days, limit, ct);

            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    message = $"✅ Sincronización completada en {result.DurationMs}ms",
                    metrics = new
                    {
                        ticketsScanned = result.TicketsScanned,
                        tagsFound = result.TagsFound,
                        inserted = result.TagsInserted,
                        updated = result.TagsUpdated,
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
                        tagsFound = result.TagsFound,
                        durationMs = result.DurationMs
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al sincronizar tags");
            return StatusCode(500, new
            {
                success = false,
                message = "Error al sincronizar tags desde Freshdesk",
                error = ex.Message
            });
        }
    }
}
