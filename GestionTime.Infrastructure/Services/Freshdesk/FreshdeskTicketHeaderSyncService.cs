using System.Diagnostics;
using System.Text.Json;
using GestionTime.Domain.Freshdesk;
using GestionTime.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace GestionTime.Infrastructure.Services.Freshdesk;

/// <summary>
/// Servicio para sincronizar cabeceras de tickets de Freshdesk
/// </summary>
public class FreshdeskTicketHeaderSyncService
{
    private readonly FreshdeskClient _freshdeskClient;
    private readonly GestionTimeDbContext _dbContext;
    private readonly FreshdeskOptions _options;
    private readonly ILogger<FreshdeskTicketHeaderSyncService> _logger;
    private const string SCOPE = "ticket_headers";

    public FreshdeskTicketHeaderSyncService(
        FreshdeskClient freshdeskClient,
        GestionTimeDbContext dbContext,
        IOptions<FreshdeskOptions> options,
        ILogger<FreshdeskTicketHeaderSyncService> logger)
    {
        _freshdeskClient = freshdeskClient;
        _dbContext = dbContext;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Sincroniza ticket headers desde Freshdesk
    /// </summary>
    /// <param name="full">True para sincronización completa, False para incremental</param>
    /// <param name="ct">CancellationToken</param>
    /// <returns>Resultado de la sincronización</returns>
    public async Task<SyncResult> SyncAsync(bool full, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var result = new SyncResult
        {
            StartedAt = DateTime.UtcNow,
            Mode = full ? "full" : "incremental"
        };

        try
        {
            _logger.LogInformation("🔄 Iniciando sincronización de ticket headers (modo: {Mode})", result.Mode);

            // Obtener estado previo
            var state = await GetSyncStateAsync(ct);
            DateTime? updatedSince = null;

            if (!full && state != null && state.LastUpdatedSince.HasValue)
            {
                updatedSince = state.LastUpdatedSince.Value;
                _logger.LogInformation("   📅 Sincronización incremental desde: {UpdatedSince}", updatedSince);
            }
            else
            {
                _logger.LogInformation("   📦 Sincronización completa (full)");
            }

            int page = 1;
            int totalUpserts = 0;
            DateTime? maxUpdatedAt = null;
            bool hasMorePages = true;

            while (hasMorePages && !ct.IsCancellationRequested)
            {
                List<FreshdeskTicketDto> tickets;

                if (updatedSince.HasValue)
                {
                    // Modo incremental
                    tickets = await _freshdeskClient.GetTicketsUpdatedSinceAsync(
                        updatedSince.Value, 
                        page, 
                        _options.PerPage, 
                        ct);
                }
                else
                {
                    // Modo full
                    tickets = await _freshdeskClient.GetTicketsPageAsync(
                        page, 
                        _options.PerPage, 
                        ct);
                }

                if (tickets.Count == 0)
                {
                    _logger.LogInformation("   ✅ No hay más tickets. Terminando sincronización.");
                    break;
                }

                result.TicketsScanned += tickets.Count;

                // Calcular máximo updated_at
                foreach (var ticket in tickets)
                {
                    if (!maxUpdatedAt.HasValue || ticket.UpdatedAt > maxUpdatedAt.Value)
                    {
                        maxUpdatedAt = ticket.UpdatedAt;
                    }
                }

                // Hacer UPSERT de tickets
                int upserted = await UpsertTicketsAsync(tickets, ct);
                totalUpserts += upserted;

                _logger.LogInformation("   📄 Página {Page}: {Count} tickets procesados ({Upserted} upserted, total: {Total})",
                    page, tickets.Count, upserted, totalUpserts);

                // Si recibimos menos tickets que perPage, no hay más páginas
                if (tickets.Count < _options.PerPage)
                {
                    hasMorePages = false;
                }

                page++;

                // Delay para evitar rate limit
                if (hasMorePages)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(500), ct);
                }
            }

            result.TagsInserted = totalUpserts;
            result.Success = true;

            // Guardar estado de sincronización
            await SaveSyncStateAsync(
                lastResultCount: result.TicketsScanned,
                lastMaxUpdatedAt: maxUpdatedAt,
                lastUpdatedSince: maxUpdatedAt,
                lastError: null,
                ct: ct);

            sw.Stop();
            result.CompletedAt = DateTime.UtcNow;
            result.DurationMs = (int)sw.ElapsedMilliseconds;

            _logger.LogInformation("✅ Sincronización completada exitosamente");
            _logger.LogInformation("   📊 Tickets escaneados: {Scanned}", result.TicketsScanned);
            _logger.LogInformation("   💾 Tickets upserted: {Upserted}", totalUpserts);
            _logger.LogInformation("   📅 Max updated_at: {MaxUpdatedAt}", maxUpdatedAt?.ToString("o") ?? "N/A");
            _logger.LogInformation("   ⏱️ Duración: {DurationMs}ms", result.DurationMs);

            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            result.Success = false;
            result.Error = ex.Message;
            result.CompletedAt = DateTime.UtcNow;
            result.DurationMs = (int)sw.ElapsedMilliseconds;

            _logger.LogError(ex, "❌ Error en sincronización de ticket headers");

            // ⚠️ TEMPORAL: Comentado para debugging - Guardar estado con error
            /*
            await SaveSyncStateAsync(
                lastResultCount: result.TicketsScanned,
                lastMaxUpdatedAt: null,
                lastUpdatedSince: null,
                lastError: ex.Message,
                ct: ct);
            */
            _logger.LogWarning("   ℹ️ Estado de error NO guardado (modo debug)");

            return result;
        }
    }

    /// <summary>
    /// Hace UPSERT de tickets en freshdesk_ticket_header
    /// </summary>
    private async Task<int> UpsertTicketsAsync(List<FreshdeskTicketDto> tickets, CancellationToken ct)
    {
        if (tickets.Count == 0)
            return 0;

        try
        {
            var connection = _dbContext.Database.GetDbConnection();
            
            // Solo abrir si no está ya abierta
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync(ct);
            }

            using var transaction = await connection.BeginTransactionAsync(ct);

            int upserted = 0;

            foreach (var ticket in tickets)
            {
                // Convertir tags a JSONB (si hay tags)
                var tagsJson = ticket.Tags != null && ticket.Tags.Any()
                    ? JsonSerializer.Serialize(ticket.Tags)
                    : null;
                
                // Convertir custom_fields a JSON
                var customFieldsJson = ticket.CustomFields != null 
                    ? JsonSerializer.Serialize(ticket.CustomFields)
                    : null;

                var sql = @"
                    INSERT INTO pss_dvnx.freshdesk_ticket_header (
                        ticket_id, subject, status, priority, type,
                        requester_id, responder_id, group_id, company_id,
                        created_at, updated_at, company_name, tags, custom_fields
                    )
                    VALUES (
                        @ticket_id, @subject, @status, @priority, @type,
                        @requester_id, @responder_id, @group_id, @company_id,
                        @created_at, @updated_at, @company_name, @tags::jsonb, @custom_fields::jsonb
                    )
                    ON CONFLICT (ticket_id) 
                    DO UPDATE SET
                        subject = EXCLUDED.subject,
                        status = EXCLUDED.status,
                        priority = EXCLUDED.priority,
                        type = EXCLUDED.type,
                        requester_id = EXCLUDED.requester_id,
                        responder_id = EXCLUDED.responder_id,
                        group_id = EXCLUDED.group_id,
                        company_id = EXCLUDED.company_id,
                        created_at = EXCLUDED.created_at,
                        updated_at = EXCLUDED.updated_at,
                        company_name = EXCLUDED.company_name,
                        tags = EXCLUDED.tags,
                        custom_fields = EXCLUDED.custom_fields
                ";

                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;
                cmd.Transaction = transaction;

                cmd.Parameters.Add(new NpgsqlParameter("@ticket_id", ticket.Id));
                cmd.Parameters.Add(new NpgsqlParameter("@subject", (object?)ticket.Subject ?? DBNull.Value));
                cmd.Parameters.Add(new NpgsqlParameter("@status", (object?)ticket.Status ?? DBNull.Value));
                cmd.Parameters.Add(new NpgsqlParameter("@priority", (object?)ticket.Priority ?? DBNull.Value));
                cmd.Parameters.Add(new NpgsqlParameter("@type", (object?)ticket.Type ?? DBNull.Value));
                cmd.Parameters.Add(new NpgsqlParameter("@requester_id", (object?)ticket.RequesterId ?? DBNull.Value));
                cmd.Parameters.Add(new NpgsqlParameter("@responder_id", (object?)ticket.ResponderId ?? DBNull.Value));
                cmd.Parameters.Add(new NpgsqlParameter("@group_id", (object?)ticket.GroupId ?? DBNull.Value));
                cmd.Parameters.Add(new NpgsqlParameter("@company_id", (object?)ticket.CompanyId ?? DBNull.Value));
                cmd.Parameters.Add(new NpgsqlParameter("@created_at", ticket.CreatedAt));
                cmd.Parameters.Add(new NpgsqlParameter("@updated_at", ticket.UpdatedAt));
                cmd.Parameters.Add(new NpgsqlParameter("@company_name", DBNull.Value)); // Dejar null por ahora
                cmd.Parameters.Add(new NpgsqlParameter("@tags", (object?)tagsJson ?? DBNull.Value));
                cmd.Parameters.Add(new NpgsqlParameter("@custom_fields", (object?)customFieldsJson ?? DBNull.Value));

                await cmd.ExecuteNonQueryAsync(ct);
                upserted++;
            }

            await transaction.CommitAsync(ct);
            return upserted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al hacer UPSERT de {Count} tickets", tickets.Count);
            throw;
        }
    }

    /// <summary>
    /// Obtiene el estado de sincronización actual
    /// </summary>
    private async Task<FreshdeskSyncState?> GetSyncStateAsync(CancellationToken ct)
    {
        var sql = @"
            SELECT scope, last_sync_at, last_result_count, last_max_updated_at, 
                   last_updated_since, last_error
            FROM pss_dvnx.freshdesk_sync_state
            WHERE scope = @scope
        ";

        var connection = _dbContext.Database.GetDbConnection();
        
        // Solo abrir si no está ya abierta
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }
        
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new NpgsqlParameter("@scope", SCOPE));

        using var reader = await cmd.ExecuteReaderAsync(ct);

        if (await reader.ReadAsync(ct))
        {
            return new FreshdeskSyncState
            {
                Scope = reader.GetString(0),
                LastSyncAt = reader.IsDBNull(1) ? null : reader.GetDateTime(1),
                LastResultCount = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                LastMaxUpdatedAt = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                LastUpdatedSince = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                LastError = reader.IsDBNull(5) ? null : reader.GetString(5)
            };
        }

        return null;
    }

    /// <summary>
    /// Guarda el estado de sincronización
    /// </summary>
    private async Task SaveSyncStateAsync(
        int lastResultCount,
        DateTime? lastMaxUpdatedAt,
        DateTime? lastUpdatedSince,
        string? lastError,
        CancellationToken ct)
    {
        var sql = @"
            INSERT INTO pss_dvnx.freshdesk_sync_state (
                scope, last_sync_at, last_result_count, last_max_updated_at,
                last_updated_since, last_error
            )
            VALUES (
                @scope, NOW(), @last_result_count, @last_max_updated_at,
                @last_updated_since, @last_error
            )
            ON CONFLICT (scope)
            DO UPDATE SET
                last_sync_at = NOW(),
                last_result_count = EXCLUDED.last_result_count,
                last_max_updated_at = EXCLUDED.last_max_updated_at,
                last_updated_since = EXCLUDED.last_updated_since,
                last_error = EXCLUDED.last_error
        ";

        var connection = _dbContext.Database.GetDbConnection();
        
        // Solo abrir si no está ya abierta
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }
        
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        cmd.Parameters.Add(new NpgsqlParameter("@scope", SCOPE));
        cmd.Parameters.Add(new NpgsqlParameter("@last_result_count", (object?)lastResultCount ?? DBNull.Value));
        cmd.Parameters.Add(new NpgsqlParameter("@last_max_updated_at", (object?)lastMaxUpdatedAt ?? DBNull.Value));
        cmd.Parameters.Add(new NpgsqlParameter("@last_updated_since", (object?)lastUpdatedSince ?? DBNull.Value));
        cmd.Parameters.Add(new NpgsqlParameter("@last_error", (object?)lastError ?? DBNull.Value));

        await cmd.ExecuteNonQueryAsync(ct);
    }

    /// <summary>
    /// Obtiene el estado actual de sincronización para el cliente
    /// </summary>
    public async Task<FreshdeskSyncState?> GetStatusAsync(CancellationToken ct = default)
    {
        return await GetSyncStateAsync(ct);
    }
}

/// <summary>
/// Resultado de una sincronización
/// </summary>
public class SyncResult
{
    public bool Success { get; set; }
    public string Mode { get; set; } = string.Empty;
    public int TicketsScanned { get; set; }
    public int TagsFound { get; set; }
    public int TagsInserted { get; set; }
    public int TagsUpdated { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public int DurationMs { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Estado de sincronización de Freshdesk
/// </summary>
public class FreshdeskSyncState
{
    public string Scope { get; set; } = string.Empty;
    public DateTime? LastSyncAt { get; set; }
    public int? LastResultCount { get; set; }
    public DateTime? LastMaxUpdatedAt { get; set; }
    public DateTime? LastUpdatedSince { get; set; }
    public string? LastError { get; set; }
}

