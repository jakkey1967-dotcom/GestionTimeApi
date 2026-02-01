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
/// Servicio para sincronizar agentes de Freshdesk (/api/v2/agents)
/// </summary>
public class FreshdeskAgentsSyncService
{
    private readonly FreshdeskClient _freshdeskClient;
    private readonly GestionTimeDbContext _dbContext;
    private readonly FreshdeskOptions _options;
    private readonly ILogger<FreshdeskAgentsSyncService> _logger;

    public FreshdeskAgentsSyncService(
        FreshdeskClient freshdeskClient,
        GestionTimeDbContext dbContext,
        IOptions<FreshdeskOptions> options,
        ILogger<FreshdeskAgentsSyncService> logger)
    {
        _freshdeskClient = freshdeskClient;
        _dbContext = dbContext;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Sincroniza todos los agentes desde Freshdesk
    /// </summary>
    public async Task<AgentsSyncResult> SyncAllAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var result = new AgentsSyncResult
        {
            StartedAt = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("👥 Iniciando sincronización completa de agents");

            // Asegurar que la tabla existe
            await EnsureTableExistsAsync(ct);

            int page = 1;
            int totalUpserts = 0;
            bool hasMorePages = true;
            List<AgentSample> samples = new();

            while (hasMorePages && !ct.IsCancellationRequested)
            {
                var agents = await _freshdeskClient.GetAgentsPageAsync(page, _options.PerPage, ct);

                if (agents.Count == 0)
                {
                    _logger.LogInformation("   ✅ No hay más agents. Terminando sincronización.");
                    break;
                }

                result.PagesFetched++;

                // Guardar samples de los primeros 3 agents de la primera página
                if (page == 1 && samples.Count < 3)
                {
                    samples.AddRange(agents.Take(3).Select(a => new AgentSample
                    {
                        AgentId = a.Id,
                        Name = a.Contact?.Name,
                        Email = a.Contact?.Email
                    }));
                }

                // Hacer UPSERT de agents
                int upserted = await UpsertAgentsAsync(agents, ct);
                totalUpserts += upserted;

                _logger.LogInformation("   📄 Página {Page}: {Count} agents procesados ({Upserted} upserted, total: {Total})",
                    page, agents.Count, upserted, totalUpserts);

                // Si recibimos menos agents que perPage, no hay más páginas
                if (agents.Count < _options.PerPage)
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

            result.AgentsUpserted = totalUpserts;
            result.SampleFirst3 = samples;
            result.Success = true;

            sw.Stop();
            result.CompletedAt = DateTime.UtcNow;
            result.DurationMs = (int)sw.ElapsedMilliseconds;

            _logger.LogInformation("✅ Sincronización completada exitosamente");
            _logger.LogInformation("   📊 Páginas obtenidas: {Pages}", result.PagesFetched);
            _logger.LogInformation("   💾 Agents upserted: {Upserted}", totalUpserts);
            _logger.LogInformation("   ⏱️ Duración: {DurationMs}ms", result.DurationMs);

            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            result.CompletedAt = DateTime.UtcNow;
            result.DurationMs = (int)sw.ElapsedMilliseconds;
            result.Error = ex.Message;

            _logger.LogError(ex, "❌ Error en sincronización de agents");
            throw;
        }
    }

    /// <summary>
    /// Asegura que la tabla existe (CREATE TABLE IF NOT EXISTS)
    /// </summary>
    private async Task EnsureTableExistsAsync(CancellationToken ct)
    {
        var sql = @"
            CREATE TABLE IF NOT EXISTS pss_dvnx.freshdesk_agents_cache (
              agent_id              bigint PRIMARY KEY,
              agent_email           text NOT NULL,
              agent_name            text NULL,
              agent_type            text NULL,
              is_active             boolean NULL,
              language              text NULL,
              time_zone             text NULL,
              mobile                text NULL,
              phone                 text NULL,
              last_login_at         timestamptz NULL,
              freshdesk_created_at  timestamptz NULL,
              freshdesk_updated_at  timestamptz NULL,
              raw                   jsonb NOT NULL,
              synced_at             timestamptz NOT NULL DEFAULT NOW()
            );

            CREATE INDEX IF NOT EXISTS ix_fd_agents_email
              ON pss_dvnx.freshdesk_agents_cache (agent_email);

            CREATE INDEX IF NOT EXISTS ix_fd_agents_active
              ON pss_dvnx.freshdesk_agents_cache (is_active) WHERE is_active = true;

            CREATE INDEX IF NOT EXISTS ix_fd_agents_updated_at
              ON pss_dvnx.freshdesk_agents_cache (freshdesk_updated_at DESC);

            CREATE INDEX IF NOT EXISTS ix_fd_agents_synced_at
              ON pss_dvnx.freshdesk_agents_cache (synced_at DESC);
        ";

        var connection = _dbContext.Database.GetDbConnection();

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync(ct);

        _logger.LogInformation("✅ Tabla freshdesk_agents_cache verificada/creada");
    }

    /// <summary>
    /// Hace UPSERT de agents en freshdesk_agents_cache
    /// </summary>
    private async Task<int> UpsertAgentsAsync(List<FreshdeskAgentDto> agents, CancellationToken ct)
    {
        if (agents.Count == 0)
            return 0;

        try
        {
            var connection = _dbContext.Database.GetDbConnection();

            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync(ct);
            }

            using var transaction = await connection.BeginTransactionAsync(ct);

            int upserted = 0;

            foreach (var agent in agents)
            {
                // Serializar todo el objeto como raw JSON
                var rawJson = JsonSerializer.Serialize(agent);

                var sql = @"
                    INSERT INTO pss_dvnx.freshdesk_agents_cache (
                        agent_id, agent_email, agent_name, agent_type, is_active,
                        language, time_zone, mobile, phone, last_login_at,
                        freshdesk_created_at, freshdesk_updated_at, raw, synced_at
                    )
                    VALUES (
                        @agent_id, @agent_email, @agent_name, @agent_type, @is_active,
                        @language, @time_zone, @mobile, @phone, @last_login_at,
                        @freshdesk_created_at, @freshdesk_updated_at, @raw::jsonb, NOW()
                    )
                    ON CONFLICT (agent_id) 
                    DO UPDATE SET
                        agent_email = EXCLUDED.agent_email,
                        agent_name = EXCLUDED.agent_name,
                        agent_type = EXCLUDED.agent_type,
                        is_active = EXCLUDED.is_active,
                        language = EXCLUDED.language,
                        time_zone = EXCLUDED.time_zone,
                        mobile = EXCLUDED.mobile,
                        phone = EXCLUDED.phone,
                        last_login_at = EXCLUDED.last_login_at,
                        freshdesk_created_at = EXCLUDED.freshdesk_created_at,
                        freshdesk_updated_at = EXCLUDED.freshdesk_updated_at,
                        raw = EXCLUDED.raw,
                        synced_at = NOW()
                ";

                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;
                cmd.Transaction = transaction;

                cmd.Parameters.Add(new NpgsqlParameter("@agent_id", agent.Id));
                cmd.Parameters.Add(new NpgsqlParameter("@agent_email", agent.Contact?.Email ?? ""));
                cmd.Parameters.Add(new NpgsqlParameter("@agent_name", (object?)agent.Contact?.Name ?? DBNull.Value));
                cmd.Parameters.Add(new NpgsqlParameter("@agent_type", (object?)agent.Type ?? DBNull.Value));
                cmd.Parameters.Add(new NpgsqlParameter("@is_active", (object?)agent.Available ?? DBNull.Value));
                cmd.Parameters.Add(new NpgsqlParameter("@language", (object?)agent.Language ?? DBNull.Value));
                cmd.Parameters.Add(new NpgsqlParameter("@time_zone", (object?)agent.TimeZone ?? DBNull.Value));
                cmd.Parameters.Add(new NpgsqlParameter("@mobile", (object?)agent.Contact?.Mobile ?? DBNull.Value));
                cmd.Parameters.Add(new NpgsqlParameter("@phone", (object?)agent.Contact?.Phone ?? DBNull.Value));
                cmd.Parameters.Add(new NpgsqlParameter("@last_login_at", (object?)agent.LastLoginAt ?? DBNull.Value));
                cmd.Parameters.Add(new NpgsqlParameter("@freshdesk_created_at", (object?)agent.CreatedAt ?? DBNull.Value));
                cmd.Parameters.Add(new NpgsqlParameter("@freshdesk_updated_at", (object?)agent.UpdatedAt ?? DBNull.Value));
                cmd.Parameters.Add(new NpgsqlParameter("@raw", rawJson));

                await cmd.ExecuteNonQueryAsync(ct);
                upserted++;
            }

            await transaction.CommitAsync(ct);

            return upserted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al hacer UPSERT de agents");
            throw;
        }
    }

    /// <summary>
    /// Obtiene el estado actual de la sincronización
    /// </summary>
    public async Task<AgentsStatusDto> GetStatusAsync(CancellationToken ct = default)
    {
        var sql = @"
            SELECT 
                COUNT(*) as total_agents,
                COUNT(*) FILTER (WHERE is_active = true) as active_agents,
                MAX(freshdesk_updated_at) as max_updated_at,
                MAX(synced_at) as max_synced_at
            FROM pss_dvnx.freshdesk_agents_cache
        ";

        var connection = _dbContext.Database.GetDbConnection();

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        using var reader = await cmd.ExecuteReaderAsync(ct);

        if (await reader.ReadAsync(ct))
        {
            return new AgentsStatusDto
            {
                TotalAgents = reader.GetInt32(0),
                ActiveAgents = reader.GetInt32(1),
                MaxUpdatedAt = reader.IsDBNull(2) ? null : reader.GetDateTime(2),
                MaxSyncedAt = reader.IsDBNull(3) ? null : reader.GetDateTime(3)
            };
        }

        return new AgentsStatusDto();
    }
}

// DTO para resultado de sincronización
public class AgentsSyncResult
{
    public bool Success { get; set; }
    public int PagesFetched { get; set; }
    public int AgentsUpserted { get; set; }
    public int DurationMs { get; set; }
    public List<AgentSample> SampleFirst3 { get; set; } = new();
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Error { get; set; }
}

// DTO para muestra de agente
public class AgentSample
{
    public long AgentId { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
}

// DTO para estado de sincronización
public class AgentsStatusDto
{
    public int TotalAgents { get; set; }
    public int ActiveAgents { get; set; }
    public DateTime? MaxUpdatedAt { get; set; }
    public DateTime? MaxSyncedAt { get; set; }
}
