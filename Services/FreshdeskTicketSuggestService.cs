using GestionTime.Api.Contracts.Freshdesk;
using GestionTime.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;

namespace GestionTime.Api.Services;

/// <summary>
/// Servicio para sugerir tickets de Freshdesk desde la vista v_freshdesk_ticket_company_min
/// </summary>
public class FreshdeskTicketSuggestService
{
    private readonly GestionTimeDbContext _dbContext;
    private readonly ILogger<FreshdeskTicketSuggestService> _logger;

    public FreshdeskTicketSuggestService(
        GestionTimeDbContext dbContext,
        ILogger<FreshdeskTicketSuggestService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Sugiere tickets desde la vista v_freshdesk_ticket_company_min con filtros opcionales
    /// </summary>
    /// <param name="agentId">ID del agente asignado (opcional)</param>
    /// <param name="ticket">Prefijo del ticket ID (opcional)</param>
    /// <param name="customer">Parte del nombre del cliente (opcional)</param>
    /// <param name="limit">Límite de resultados (default 10, max 50)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Lista de tickets sugeridos</returns>
    public async Task<List<FreshdeskTicketSuggestDto>> SuggestAsync(
        long? agentId = null,
        string? ticket = null,
        string? customer = null,
        int limit = 10,
        CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Validar y ajustar límite
            limit = Math.Clamp(limit, 1, 50);

            // Preparar parámetros
            long? agentIdParam = agentId;
            string? ticketPrefixParam = string.IsNullOrWhiteSpace(ticket) ? null : $"{ticket.Trim()}%";
            string? customerLikeParam = string.IsNullOrWhiteSpace(customer) ? null : $"%{customer.Trim()}%";

            _logger.LogInformation("🔍 Sugiriendo tickets desde view v_freshdesk_ticket_company_min");
            _logger.LogInformation("   Filtros: agentId={AgentId}, ticket={Ticket}, customer={Customer}, limit={Limit}",
                agentIdParam?.ToString() ?? "null",
                ticket ?? "null",
                customer ?? "null",
                limit);

            // Construir SQL parametrizado con filtros opcionales
            var sql = @"
                SELECT
                    ticket_id,
                    company_name_cache,
                    subject,
                    status,
                    agente_asignado_id,
                    agente_asignado_nombre
                FROM pss_dvnx.v_freshdesk_ticket_company_min
                WHERE 1=1
                    AND (@agentId IS NULL OR agente_asignado_id = @agentId)
                    AND (@ticketPrefix IS NULL OR ticket_id::text LIKE @ticketPrefix)
                    AND (@customerLike IS NULL OR company_name_cache ILIKE @customerLike)
                ORDER BY ticket_id DESC
                LIMIT @limit
            ";

            var connection = _dbContext.Database.GetDbConnection();

            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync(ct);
            }

            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;

            // Parámetros con tipos explícitos para evitar error "could not determine data type"
            cmd.Parameters.Add(new NpgsqlParameter("@agentId", NpgsqlTypes.NpgsqlDbType.Bigint)
            {
                Value = agentIdParam.HasValue ? (object)agentIdParam.Value : DBNull.Value
            });
            cmd.Parameters.Add(new NpgsqlParameter("@ticketPrefix", NpgsqlTypes.NpgsqlDbType.Text)
            {
                Value = (object?)ticketPrefixParam ?? DBNull.Value
            });
            cmd.Parameters.Add(new NpgsqlParameter("@customerLike", NpgsqlTypes.NpgsqlDbType.Text)
            {
                Value = (object?)customerLikeParam ?? DBNull.Value
            });
            cmd.Parameters.Add(new NpgsqlParameter("@limit", NpgsqlTypes.NpgsqlDbType.Integer)
            {
                Value = limit
            });

            var results = new List<FreshdeskTicketSuggestDto>();

            using var reader = await cmd.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                var dto = new FreshdeskTicketSuggestDto
                {
                    TicketId = reader.GetInt64(0),
                    Customer = reader.IsDBNull(1) ? null : reader.GetString(1),
                    Subject = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Status = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                    AgentId = reader.IsDBNull(4) ? null : reader.GetInt64(4),
                    AgentName = reader.IsDBNull(5) ? null : reader.GetString(5)
                };

                results.Add(dto);
            }

            sw.Stop();
            _logger.LogInformation("✅ Sugerencias obtenidas: {Count} tickets en {Ms}ms",
                results.Count, sw.ElapsedMilliseconds);

            return results;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "❌ Error al obtener sugerencias de tickets (duración: {Ms}ms)", sw.ElapsedMilliseconds);
            throw;
        }
    }
}
