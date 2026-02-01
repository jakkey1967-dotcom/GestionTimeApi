using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GestionTime.Domain.Freshdesk;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GestionTime.Infrastructure.Services.Freshdesk;

public class FreshdeskClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FreshdeskClient> _logger;
    private readonly FreshdeskOptions _options;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public FreshdeskClient(
        HttpClient httpClient, 
        IOptions<FreshdeskOptions> options,
        ILogger<FreshdeskClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
        
        var authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_options.ApiKey}:X"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        
        // 🔍 LOG DETALLADO DE CONFIGURACIÓN
        _logger.LogInformation("🔧 FreshdeskClient configurado:");
        _logger.LogInformation("   BaseUrl: {BaseUrl}", _options.BaseUrl);
        _logger.LogInformation("   ApiKey: {ApiKeyMasked}", MaskApiKey(_options.ApiKey));
        _logger.LogInformation("   Auth Header: Basic {AuthPrefix}...", authValue.Substring(0, Math.Min(10, authValue.Length)));
    }
    
    private static string MaskApiKey(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey) || apiKey.Length < 8)
            return "***";
        return $"{apiKey.Substring(0, 4)}...{apiKey.Substring(apiKey.Length - 4)}";
    }
    
    /// <summary>
    /// 🏓 Ping a Freshdesk API para verificar conectividad y credenciales
    /// </summary>
    public async Task<(bool Success, int StatusCode, string? AgentEmail, string? Error)> PingAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("🏓 Ping a Freshdesk API...");
            _logger.LogInformation("   URL: {BaseUrl}api/v2/agents/me", _options.BaseUrl);
            
            var response = await _httpClient.GetAsync("api/v2/agents/me", ct);
            var statusCode = (int)response.StatusCode;
            
            _logger.LogInformation("   Response StatusCode: {StatusCode}", statusCode);
            
            if (response.IsSuccessStatusCode)
            {
                // 🔍 Log del JSON raw para debugging
                var jsonContent = await response.Content.ReadAsStringAsync(ct);
                _logger.LogInformation("   📄 JSON Response: {Json}", jsonContent);
                
                var agent = await response.Content.ReadFromJsonAsync<FreshdeskAgentDto>(JsonOptions, ct);
                
                _logger.LogInformation("   📧 Parsed Email: {Email}", agent?.Email ?? "NULL");
                _logger.LogInformation("   👤 Parsed Name: {Name}", agent?.Name ?? "NULL");
                _logger.LogInformation("   🆔 Parsed Id: {Id}", agent?.Id ?? 0);
                
                _logger.LogInformation("✅ Ping exitoso - Agent: {Email}", agent?.Email ?? "Unknown");
                return (true, statusCode, agent?.Email, null);
            }
            else if (statusCode == 401 || statusCode == 403)
            {
                var error = "Credenciales inválidas (API Key incorrecta)";
                _logger.LogWarning("❌ Ping fallido: {Error}", error);
                return (false, statusCode, null, error);
            }
            else
            {
                var error = $"Error HTTP {statusCode}";
                _logger.LogWarning("❌ Ping fallido: {Error}", error);
                return (false, statusCode, null, error);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "❌ Error de conexión al hacer ping a Freshdesk");
            return (false, 0, null, $"Error de conexión: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error inesperado al hacer ping a Freshdesk");
            return (false, 0, null, $"Error inesperado: {ex.Message}");
        }
    }

    public async Task<List<FreshdeskAgentDto>> SearchAgentsByEmailAsync(string email, CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var url = $"/api/v2/agents/autocomplete?term={Uri.EscapeDataString(email)}";
            _logger.LogInformation("🔍 Buscando agente Freshdesk por email: {Email}", email);
            _logger.LogInformation("   URL: {BaseUrl}{Path}", _options.BaseUrl, url);
            
            var response = await _httpClient.GetAsync(url, ct);
            
            _logger.LogInformation("   Response StatusCode: {StatusCode}", response.StatusCode);
            
            response.EnsureSuccessStatusCode();
            
            var agents = await response.Content.ReadFromJsonAsync<List<FreshdeskAgentDto>>(JsonOptions, ct) 
                         ?? new List<FreshdeskAgentDto>();
            
            sw.Stop();
            _logger.LogInformation("Freshdesk agents autocomplete completado en {Ms}ms, encontrados: {Count}", 
                sw.ElapsedMilliseconds, agents.Count);
            
            return agents;
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Error HTTP al buscar agente en Freshdesk (duración: {Ms}ms)", sw.ElapsedMilliseconds);
            
            if (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                throw new InvalidOperationException("Rate limit alcanzado en Freshdesk API", ex);
            
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Error inesperado al buscar agente en Freshdesk (duración: {Ms}ms)", sw.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<FreshdeskSearchResponse<FreshdeskTicketDto>> SearchTicketsAsync(
        string query, 
        int page = 1,
        CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("🔍 Buscando tickets en Freshdesk");
            _logger.LogInformation("   📝 Query original: {Query}", query);
            _logger.LogInformation("   📄 Página: {Page}", page);
            
            // Freshdesk espera: /api/v2/search/tickets?query="..."&page={page}
            var url = $"/api/v2/search/tickets?query=\"{query}\"&page={page}";
            
            _logger.LogInformation("   🌐 URL completa: {BaseUrl}{Path}", _options.BaseUrl, url);
            
            var response = await _httpClient.GetAsync(url, ct);
            
            _logger.LogInformation("   📊 Response StatusCode: {StatusCode}", response.StatusCode);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("   ❌ Error de Freshdesk: {Error}", errorContent);
            }
            
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<FreshdeskSearchResponse<FreshdeskTicketDto>>(JsonOptions, ct)
                         ?? new FreshdeskSearchResponse<FreshdeskTicketDto>();
            
            sw.Stop();
            _logger.LogInformation("   ✅ Completado en {Ms}ms, encontrados: {Count}/{Total}", 
                sw.ElapsedMilliseconds, result.Results.Count, result.Total);
            
            return result;
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            _logger.LogError(ex, "❌ Error HTTP al buscar tickets (duración: {Ms}ms)", sw.ElapsedMilliseconds);
            
            if (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                throw new InvalidOperationException("Rate limit alcanzado en Freshdesk API", ex);
            
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "❌ Error inesperado al buscar tickets (duración: {Ms}ms)", sw.ElapsedMilliseconds);
            throw;
        }
    }
    
    /// <summary>
    /// Buscar todos los tickets con paginación automática
    /// Freshdesk limita a máximo 10 páginas (300 tickets aprox)
    /// </summary>
    public async Task<List<FreshdeskTicketDto>> SearchAllTicketsAsync(
        string query,
        int maxResults = 1000,
        CancellationToken ct = default)
    {
        const int MAX_PAGES = 10; // Límite de Freshdesk API
        
        _logger.LogInformation("🔍 Buscando tickets con paginación automática (max: {Max} tickets, {MaxPages} páginas)", 
            maxResults, MAX_PAGES);
        
        var allTickets = new List<FreshdeskTicketDto>();
        var page = 1;
        
        while (allTickets.Count < maxResults && page <= MAX_PAGES)
        {
            var result = await SearchTicketsAsync(query, page, ct);
            
            if (result.Results.Count == 0)
                break;
            
            allTickets.AddRange(result.Results);
            
            _logger.LogInformation("   📄 Página {Page}: +{Count} tickets (total: {Total})", 
                page, result.Results.Count, allTickets.Count);
            
            if (allTickets.Count >= result.Total || allTickets.Count >= maxResults)
                break;
            
            page++;
            
            // Delay entre páginas para evitar rate limit
            if (page <= MAX_PAGES)
                await Task.Delay(TimeSpan.FromMilliseconds(500), ct);
        }
        
        if (page > MAX_PAGES)
        {
            _logger.LogWarning("⚠️ Alcanzado límite de {MaxPages} páginas de Freshdesk. Solo se obtuvieron {Count} tickets de {Total} totales.", 
                MAX_PAGES, allTickets.Count, maxResults);
        }
        
        var finalResults = allTickets.Take(maxResults).ToList();
        _logger.LogInformation("✅ Búsqueda completada: {Count} tickets obtenidos", finalResults.Count);
        
        return finalResults;
    }

    public async Task<FreshdeskTicketDto?> GetTicketByIdAsync(long ticketId, CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("Obteniendo ticket Freshdesk: {TicketId}", ticketId);
            
            var response = await _httpClient.GetAsync($"/api/v2/tickets/{ticketId}", ct);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            
            response.EnsureSuccessStatusCode();
            
            var ticket = await response.Content.ReadFromJsonAsync<FreshdeskTicketDto>(JsonOptions, ct);
            
            sw.Stop();
            _logger.LogInformation("Freshdesk ticket obtenido en {Ms}ms", sw.ElapsedMilliseconds);
            
            return ticket;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Error al obtener ticket {TicketId} (duración: {Ms}ms)", ticketId, sw.ElapsedMilliseconds);
            throw;
        }
    }
    
    /// <summary>
    /// Obtiene detalles completos de un ticket incluyendo requester
    /// GET /api/v2/tickets/{id}?include=requester
    /// </summary>
    public async Task<FreshdeskTicketDetailDto?> GetTicketDetailAsync(long ticketId, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v2/tickets/{ticketId}?include=requester", ct);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                throw new HttpRequestException("Rate limit exceeded", null, System.Net.HttpStatusCode.TooManyRequests);
            
            response.EnsureSuccessStatusCode();
            
            // Parsear respuesta manualmente porque Freshdesk devuelve campos snake_case
            var json = await response.Content.ReadAsStringAsync(ct);
            var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            var detail = new FreshdeskTicketDetailDto
            {
                Id = root.GetProperty("id").GetInt64(),
                CompanyId = root.TryGetProperty("company_id", out var companyIdProp) && companyIdProp.ValueKind != System.Text.Json.JsonValueKind.Null
                    ? companyIdProp.GetInt64()
                    : null,
                ResponderId = root.TryGetProperty("responder_id", out var responderIdProp) && responderIdProp.ValueKind != System.Text.Json.JsonValueKind.Null
                    ? responderIdProp.GetInt64()
                    : null
            };
            
            // Parsear requester si está incluido
            if (root.TryGetProperty("requester", out var requesterProp) && requesterProp.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                detail.Requester = new FreshdeskRequesterDto
                {
                    Id = requesterProp.GetProperty("id").GetInt64(),
                    Name = requesterProp.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "" : "",
                    Email = requesterProp.TryGetProperty("email", out var emailProp) ? emailProp.GetString() ?? "" : ""
                };
            }
            
            return detail;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            throw; // Propagar para manejo especial
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Error al obtener detalles del ticket {TicketId}", ticketId);
            return null;
        }
    }
    
    /// <summary>
    /// Obtiene información de una compañía
    /// GET /api/v2/companies/{id}
    /// </summary>
    public async Task<FreshdeskCompanyDto?> GetCompanyAsync(long companyId, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v2/companies/{companyId}", ct);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                throw new HttpRequestException("Rate limit exceeded", null, System.Net.HttpStatusCode.TooManyRequests);
            
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync(ct);
            var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            return new FreshdeskCompanyDto
            {
                Id = root.GetProperty("id").GetInt64(),
                Name = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "" : ""
            };
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            throw; // Propagar para manejo especial
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Error al obtener compañía {CompanyId}", companyId);
            return null;
        }
    }
    
    /// <summary>
    /// Obtiene información de un agente/técnico
    /// GET /api/v2/agents/{id}
    /// </summary>
    public async Task<FreshdeskAgentDetailDto?> GetAgentAsync(long agentId, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v2/agents/{agentId}", ct);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            
            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("⚠️ Acceso denegado al obtener agente {AgentId} (403 Forbidden)", agentId);
                return null;
            }
            
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                throw new HttpRequestException("Rate limit exceeded", null, System.Net.HttpStatusCode.TooManyRequests);
            
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync(ct);
            var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            var agent = new FreshdeskAgentDetailDto
            {
                Id = root.GetProperty("id").GetInt64(),
                Name = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "" : "",
                Email = root.TryGetProperty("email", out var emailProp) ? emailProp.GetString() ?? "" : ""
            };
            
            // Parsear contact si existe (a veces los agentes tienen info en contact)
            if (root.TryGetProperty("contact", out var contactProp) && contactProp.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                agent.Contact = new FreshdeskContactDto
                {
                    Name = contactProp.TryGetProperty("name", out var contactNameProp) ? contactNameProp.GetString() ?? "" : "",
                    Email = contactProp.TryGetProperty("email", out var contactEmailProp) ? contactEmailProp.GetString() ?? "" : ""
                };
            }
            
            return agent;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            throw; // Propagar para manejo especial
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Error al obtener agente {AgentId}", agentId);
            return null;
        }
    }
    
    /// <summary>
    /// Obtiene detalles completos de un ticket incluyendo requester, company y conversations
    /// GET /api/v2/tickets/{id}?include=company,requester,conversations
    /// </summary>
    public async Task<FreshdeskTicketDetailsDto?> GetTicketDetailsForEditAsync(int ticketId, CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("📋 Obteniendo detalles completos del ticket: {TicketId}", ticketId);
            
            var url = $"/api/v2/tickets/{ticketId}?include=company,requester,conversations";
            var response = await _httpClient.GetAsync(url, ct);
            
            sw.Stop();
            _logger.LogInformation("   📊 Freshdesk response: {StatusCode} en {Ms}ms", response.StatusCode, sw.ElapsedMilliseconds);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("   ⚠️ Ticket {TicketId} no encontrado en Freshdesk", ticketId);
                return null;
            }
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || 
                response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogError("   ❌ Error de autenticación con Freshdesk (status: {StatusCode})", response.StatusCode);
                throw new HttpRequestException($"Freshdesk authentication failed: {response.StatusCode}", null, response.StatusCode);
            }
            
            response.EnsureSuccessStatusCode();
            
            // Parsear respuesta JSON manualmente (Freshdesk usa snake_case)
            var json = await response.Content.ReadAsStringAsync(ct);
            var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            var details = new FreshdeskTicketDetailsDto
            {
                Id = root.GetProperty("id").GetInt64(),
                Subject = root.TryGetProperty("subject", out var subjectProp) ? subjectProp.GetString() ?? "" : "",
                Status = root.TryGetProperty("status", out var statusProp) ? statusProp.GetInt32() : 0,
                Priority = root.TryGetProperty("priority", out var priorityProp) ? priorityProp.GetInt32() : 0,
                CreatedAt = root.TryGetProperty("created_at", out var createdProp) && createdProp.ValueKind != System.Text.Json.JsonValueKind.Null
                    ? DateTime.Parse(createdProp.GetString() ?? DateTime.UtcNow.ToString())
                    : DateTime.UtcNow,
                UpdatedAt = root.TryGetProperty("updated_at", out var updatedProp) && updatedProp.ValueKind != System.Text.Json.JsonValueKind.Null
                    ? DateTime.Parse(updatedProp.GetString() ?? DateTime.UtcNow.ToString())
                    : DateTime.UtcNow,
                DescriptionText = root.TryGetProperty("description_text", out var descTextProp) ? descTextProp.GetString() ?? "" : ""
            };
            
            // Parsear requester
            if (root.TryGetProperty("requester", out var requesterProp) && requesterProp.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                details.Requester = new FreshdeskRequesterInfoDto
                {
                    Id = requesterProp.TryGetProperty("id", out var reqIdProp) ? reqIdProp.GetInt64() : 0,
                    Name = requesterProp.TryGetProperty("name", out var reqNameProp) ? reqNameProp.GetString() ?? "" : "",
                    Email = requesterProp.TryGetProperty("email", out var reqEmailProp) ? reqEmailProp.GetString() ?? "" : ""
                };
            }
            
            // Parsear company
            if (root.TryGetProperty("company", out var companyProp) && companyProp.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                details.Company = new FreshdeskCompanyInfoDto
                {
                    Id = companyProp.TryGetProperty("id", out var compIdProp) ? compIdProp.GetInt64() : 0,
                    Name = companyProp.TryGetProperty("name", out var compNameProp) ? compNameProp.GetString() ?? "" : ""
                };
            }
            
            // Parsear conversations
            if (root.TryGetProperty("conversations", out var convsProp) && convsProp.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                var conversations = new List<FreshdeskConversationDto>();
                
                foreach (var conv in convsProp.EnumerateArray())
                {
                    var conversation = new FreshdeskConversationDto
                    {
                        Id = conv.TryGetProperty("id", out var convIdProp) ? convIdProp.GetInt64() : 0,
                        Incoming = conv.TryGetProperty("incoming", out var incomingProp) && incomingProp.GetBoolean(),
                        Private = conv.TryGetProperty("private", out var privateProp) && privateProp.GetBoolean(),
                        CreatedAt = conv.TryGetProperty("created_at", out var convCreatedProp) && convCreatedProp.ValueKind != System.Text.Json.JsonValueKind.Null
                            ? DateTime.Parse(convCreatedProp.GetString() ?? DateTime.UtcNow.ToString())
                            : DateTime.UtcNow,
                        UpdatedAt = conv.TryGetProperty("updated_at", out var convUpdatedProp) && convUpdatedProp.ValueKind != System.Text.Json.JsonValueKind.Null
                            ? DateTime.Parse(convUpdatedProp.GetString() ?? DateTime.UtcNow.ToString())
                            : DateTime.UtcNow,
                        FromEmail = conv.TryGetProperty("from_email", out var fromEmailProp) ? fromEmailProp.GetString() ?? "" : "",
                        BodyText = conv.TryGetProperty("body_text", out var bodyTextProp) ? bodyTextProp.GetString() ?? "" : ""
                    };
                    
                    // Parsear to_emails array
                    if (conv.TryGetProperty("to_emails", out var toEmailsProp) && toEmailsProp.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        conversation.ToEmails = toEmailsProp.EnumerateArray()
                            .Select(e => e.GetString() ?? "")
                            .Where(e => !string.IsNullOrEmpty(e))
                            .ToList();
                    }
                    
                    // Parsear cc_emails array
                    if (conv.TryGetProperty("cc_emails", out var ccEmailsProp) && ccEmailsProp.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        conversation.CcEmails = ccEmailsProp.EnumerateArray()
                            .Select(e => e.GetString() ?? "")
                            .Where(e => !string.IsNullOrEmpty(e))
                            .ToList();
                    }
                    
                    conversations.Add(conversation);
                }
                
                // Eliminar duplicados por ID y ordenar por fecha de creación
                details.Conversations = conversations
                    .GroupBy(c => c.Id)
                    .Select(g => g.First())
                    .OrderBy(c => c.CreatedAt)
                    .ToList();
                
                _logger.LogInformation("   ✅ Parseadas {Count} conversaciones (únicas)", details.Conversations.Count);
            }
            
            _logger.LogInformation("   ✅ Detalles completos obtenidos correctamente");
            
            return details;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized || 
                                               ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            sw.Stop();
            _logger.LogError(ex, "❌ Error de autenticación con Freshdesk (duración: {Ms}ms)", sw.ElapsedMilliseconds);
            throw; // Propagar para manejo en controller
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "❌ Error al obtener detalles del ticket {TicketId} (duración: {Ms}ms)", ticketId, sw.ElapsedMilliseconds);
            throw;
        }
    }
    
    /// <summary>
    /// Obtiene una página de tickets de Freshdesk
    /// GET /api/v2/tickets?per_page=100&page=1
    /// </summary>
    public async Task<List<FreshdeskTicketDto>> GetTicketsPageAsync(
        int page = 1, 
        int perPage = 100, 
        CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var url = $"/api/v2/tickets?per_page={perPage}&page={page}";
            _logger.LogInformation("📥 Obteniendo página {Page} de tickets (perPage: {PerPage})", page, perPage);
            _logger.LogDebug("   URL: {BaseUrl}{Path}", _options.BaseUrl, url);
            
            var response = await _httpClient.GetAsync(url, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("   ❌ Error de Freshdesk: Status={Status}, Body={Body}", 
                    response.StatusCode, errorContent);
            }
            
            response.EnsureSuccessStatusCode();
            
            var tickets = await response.Content.ReadFromJsonAsync<List<FreshdeskTicketDto>>(JsonOptions, ct) 
                         ?? new List<FreshdeskTicketDto>();
            
            sw.Stop();
            _logger.LogInformation("   ✅ Obtenidos {Count} tickets en {Ms}ms", tickets.Count, sw.ElapsedMilliseconds);
            
            return tickets;
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            _logger.LogError(ex, "❌ Error HTTP al obtener página {Page} (duración: {Ms}ms)", page, sw.ElapsedMilliseconds);
            
            if (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                throw new InvalidOperationException("Rate limit alcanzado en Freshdesk API", ex);
            
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "❌ Error inesperado al obtener página {Page} (duración: {Ms}ms)", page, sw.ElapsedMilliseconds);
            throw;
        }
    }
    
    /// <summary>
    /// Obtiene tickets actualizados desde una fecha específica
    /// GET /api/v2/tickets?updated_since=2025-01-01T00:00:00Z&per_page=100&page=1
    /// </summary>
    public async Task<List<FreshdeskTicketDto>> GetTicketsUpdatedSinceAsync(
        DateTime updatedSince,
        int page = 1,
        int perPage = 100,
        CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            // Freshdesk espera ISO 8601 en UTC
            var updatedSinceStr = updatedSince.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            var url = $"/api/v2/tickets?updated_since={Uri.EscapeDataString(updatedSinceStr)}&per_page={perPage}&page={page}";
            
            _logger.LogInformation("📥 Obteniendo tickets actualizados desde {UpdatedSince} (página {Page}, perPage: {PerPage})", 
                updatedSinceStr, page, perPage);
            _logger.LogDebug("   URL: {BaseUrl}{Path}", _options.BaseUrl, url);
            
            var response = await _httpClient.GetAsync(url, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("   ❌ Error de Freshdesk: Status={Status}, Body={Body}", 
                    response.StatusCode, errorContent);
            }
            
            response.EnsureSuccessStatusCode();
            
            var tickets = await response.Content.ReadFromJsonAsync<List<FreshdeskTicketDto>>(JsonOptions, ct) 
                         ?? new List<FreshdeskTicketDto>();
            
            sw.Stop();
            _logger.LogInformation("   ✅ Obtenidos {Count} tickets en {Ms}ms", tickets.Count, sw.ElapsedMilliseconds);
            
            return tickets;
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            _logger.LogError(ex, "❌ Error HTTP al obtener tickets desde {UpdatedSince} (duración: {Ms}ms)", 
                updatedSince, sw.ElapsedMilliseconds);
            
            if (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                throw new InvalidOperationException("Rate limit alcanzado en Freshdesk API", ex);
            
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "❌ Error inesperado al obtener tickets desde {UpdatedSince} (duración: {Ms}ms)", 
                updatedSince, sw.ElapsedMilliseconds);
            throw;
        }
    }
    
    /// <summary>
    /// Obtiene una página de companies de Freshdesk
    /// GET /api/v2/companies?per_page=100&page=1
    /// </summary>
    public async Task<List<FreshdeskCompanyDto>> GetCompaniesPageAsync(
        int page = 1, 
        int perPage = 100, 
        CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var url = $"/api/v2/companies?per_page={perPage}&page={page}";
            _logger.LogInformation("🏢 Obteniendo página {Page} de companies (perPage: {PerPage})", page, perPage);
            _logger.LogDebug("   URL: {BaseUrl}{Path}", _options.BaseUrl, url);
            
            var response = await _httpClient.GetAsync(url, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("   ❌ Error de Freshdesk: Status={Status}, Body={Body}", 
                    response.StatusCode, errorContent);
            }
            
            response.EnsureSuccessStatusCode();
            
            var companies = await response.Content.ReadFromJsonAsync<List<FreshdeskCompanyDto>>(JsonOptions, ct) 
                         ?? new List<FreshdeskCompanyDto>();
            
            sw.Stop();
            _logger.LogInformation("   ✅ Obtenidas {Count} companies en {Ms}ms", companies.Count, sw.ElapsedMilliseconds);
            
            return companies;
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            _logger.LogError(ex, "❌ Error HTTP al obtener página {Page} de companies (duración: {Ms}ms)", page, sw.ElapsedMilliseconds);
            
            if (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                throw new InvalidOperationException("Rate limit alcanzado en Freshdesk API", ex);
            
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "❌ Error inesperado al obtener página {Page} de companies (duración: {Ms}ms)", page, sw.ElapsedMilliseconds);
            throw;
        }
    }
    
    /// <summary>
    /// Obtiene información del agente actual (dueño de la API Key)
    /// GET /api/v2/agents/me
    /// </summary>
    public async Task<FreshdeskAgentMeDto?> GetCurrentAgentAsync(CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var url = "/api/v2/agents/me";
            _logger.LogInformation("👤 Obteniendo información del agente actual (me)");
            _logger.LogDebug("   URL: {BaseUrl}{Path}", _options.BaseUrl, url);
            
            var response = await _httpClient.GetAsync(url, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("   ❌ Error de Freshdesk: Status={Status}, Body={Body}", 
                    response.StatusCode, errorContent);
            }
            
            response.EnsureSuccessStatusCode();
            
            var agent = await response.Content.ReadFromJsonAsync<FreshdeskAgentMeDto>(JsonOptions, ct);
            
            sw.Stop();
            _logger.LogInformation("   ✅ Obtenida información del agente en {Ms}ms", sw.ElapsedMilliseconds);
            
            return agent;
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            _logger.LogError(ex, "❌ Error HTTP al obtener agente actual (duración: {Ms}ms)", sw.ElapsedMilliseconds);
            
            if (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                throw new InvalidOperationException("Rate limit alcanzado en Freshdesk API", ex);
            
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "❌ Error inesperado al obtener agente actual (duración: {Ms}ms)", sw.ElapsedMilliseconds);
            throw;
        }
    }
    
    /// <summary>
    /// Obtiene una página de agentes de Freshdesk
    /// GET /api/v2/agents?per_page=100&page=1
    /// </summary>
    public async Task<List<FreshdeskAgentDto>> GetAgentsPageAsync(
        int page = 1, 
        int perPage = 100, 
        CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var url = $"/api/v2/agents?per_page={perPage}&page={page}";
            _logger.LogInformation("👥 Obteniendo página {Page} de agents (perPage: {PerPage})", page, perPage);
            _logger.LogDebug("   URL: {BaseUrl}{Path}", _options.BaseUrl, url);
            
            var response = await _httpClient.GetAsync(url, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("   ❌ Error de Freshdesk: Status={Status}, Body={Body}", 
                    response.StatusCode, errorContent);
            }
            
            response.EnsureSuccessStatusCode();
            
            var agents = await response.Content.ReadFromJsonAsync<List<FreshdeskAgentDto>>(JsonOptions, ct) 
                         ?? new List<FreshdeskAgentDto>();
            
            sw.Stop();
            _logger.LogInformation("   ✅ Obtenidos {Count} agents en {Ms}ms", agents.Count, sw.ElapsedMilliseconds);
            
            return agents;
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            _logger.LogError(ex, "❌ Error HTTP al obtener página {Page} de agents (duración: {Ms}ms)", page, sw.ElapsedMilliseconds);
            
            if (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                throw new InvalidOperationException("Rate limit alcanzado en Freshdesk API", ex);
            
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "❌ Error inesperado al obtener página {Page} de agents (duración: {Ms}ms)", page, sw.ElapsedMilliseconds);
            throw;
        }
    }
}




























