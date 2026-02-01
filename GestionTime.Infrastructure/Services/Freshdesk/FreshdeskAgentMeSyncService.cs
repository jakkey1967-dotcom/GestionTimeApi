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
/// Servicio para sincronizar el agente actual de Freshdesk (/api/v2/agents/me)
/// </summary>
public class FreshdeskAgentMeSyncService
{
    private readonly FreshdeskClient _freshdeskClient;
    private readonly GestionTimeDbContext _dbContext;
    private readonly FreshdeskOptions _options;
    private readonly ILogger<FreshdeskAgentMeSyncService> _logger;

    public FreshdeskAgentMeSyncService(
        FreshdeskClient freshdeskClient,
        GestionTimeDbContext dbContext,
        IOptions<FreshdeskOptions> options,
        ILogger<FreshdeskAgentMeSyncService> logger)
    {
        _freshdeskClient = freshdeskClient;
        _dbContext = dbContext;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Sincroniza el agente actual desde Freshdesk
    /// </summary>
    public async Task<AgentMeSyncResult> SyncAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var result = new AgentMeSyncResult
        {
            StartedAt = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("👤 Iniciando sincronización del agente actual (me)");

            // Asegurar que la tabla existe
            await EnsureTableExistsAsync(ct);

            // Obtener agente actual desde Freshdesk
            var agent = await _freshdeskClient.GetCurrentAgentAsync(ct);

            if (agent == null)
            {
                throw new InvalidOperationException("No se pudo obtener información del agente actual");
            }

            // Hacer UPSERT
            await UpsertAgentMeAsync(agent, ct);

            result.Success = true;
            result.AgentId = agent.Id;
            result.AgentEmail = agent.Contact?.Email;
            result.FreshdeskUpdatedAt = agent.UpdatedAt;

            sw.Stop();
            result.CompletedAt = DateTime.UtcNow;
            result.DurationMs = (int)sw.ElapsedMilliseconds;

            _logger.LogInformation("✅ Sincronización completada exitosamente");
            _logger.LogInformation("   👤 Agent ID: {AgentId}", result.AgentId);
            _logger.LogInformation("   📧 Email: {Email}", result.AgentEmail);
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

            _logger.LogError(ex, "❌ Error en sincronización del agente actual");
            throw;
        }
    }

    /// <summary>
    /// Asegura que la tabla existe
    /// </summary>
    private async Task EnsureTableExistsAsync(CancellationToken ct)
    {
        var sql = @"
            CREATE TABLE IF NOT EXISTS pss_dvnx.freshdesk_agent_me_cache (
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

            CREATE INDEX IF NOT EXISTS ix_fd_agent_me_email
              ON pss_dvnx.freshdesk_agent_me_cache (agent_email);

            CREATE INDEX IF NOT EXISTS ix_fd_agent_me_synced_at
              ON pss_dvnx.freshdesk_agent_me_cache (synced_at DESC);
        ";

        var connection = _dbContext.Database.GetDbConnection();

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync(ct);

        _logger.LogInformation("✅ Tabla freshdesk_agent_me_cache verificada/creada");
    }

    /// <summary>
    /// Hace UPSERT del agente actual
    /// </summary>
    private async Task UpsertAgentMeAsync(FreshdeskAgentMeDto agent, CancellationToken ct)
    {
        try
        {
            var connection = _dbContext.Database.GetDbConnection();

            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync(ct);
            }

            // Serializar todo el objeto como raw JSON (eliminando signature si existe)
            var rawJson = JsonSerializer.Serialize(agent);

            var sql = @"
                INSERT INTO pss_dvnx.freshdesk_agent_me_cache (
                    agent_id, agent_email, agent_name, agent_type,
                    is_active, language, time_zone, mobile, phone,
                    last_login_at, freshdesk_created_at, freshdesk_updated_at,
                    raw, synced_at
                )
                VALUES (
                    @agent_id, @agent_email, @agent_name, @agent_type,
                    @is_active, @language, @time_zone, @mobile, @phone,
                    @last_login_at, @freshdesk_created_at, @freshdesk_updated_at,
                    @raw::jsonb, NOW()
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

            cmd.Parameters.Add(new NpgsqlParameter("@agent_id", agent.Id));
            cmd.Parameters.Add(new NpgsqlParameter("@agent_email", (object?)agent.Contact?.Email ?? DBNull.Value));
            cmd.Parameters.Add(new NpgsqlParameter("@agent_name", (object?)agent.Contact?.Name ?? DBNull.Value));
            cmd.Parameters.Add(new NpgsqlParameter("@agent_type", (object?)agent.Type ?? DBNull.Value));
            cmd.Parameters.Add(new NpgsqlParameter("@is_active", (object?)agent.Contact?.Active ?? DBNull.Value));
            cmd.Parameters.Add(new NpgsqlParameter("@language", (object?)agent.Contact?.Language ?? DBNull.Value));
            cmd.Parameters.Add(new NpgsqlParameter("@time_zone", (object?)agent.Contact?.TimeZone ?? DBNull.Value));
            cmd.Parameters.Add(new NpgsqlParameter("@mobile", (object?)agent.Contact?.Mobile ?? DBNull.Value));
            cmd.Parameters.Add(new NpgsqlParameter("@phone", (object?)agent.Contact?.Phone ?? DBNull.Value));
            cmd.Parameters.Add(new NpgsqlParameter("@last_login_at", (object?)agent.Contact?.LastLoginAt ?? DBNull.Value));
            cmd.Parameters.Add(new NpgsqlParameter("@freshdesk_created_at", (object?)agent.CreatedAt ?? DBNull.Value));
            cmd.Parameters.Add(new NpgsqlParameter("@freshdesk_updated_at", (object?)agent.UpdatedAt ?? DBNull.Value));
            cmd.Parameters.Add(new NpgsqlParameter("@raw", rawJson));

            await cmd.ExecuteNonQueryAsync(ct);

            _logger.LogInformation("✅ Agente actual sincronizado correctamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al hacer UPSERT del agente actual");
            throw;
        }
    }

    /// <summary>
    /// Obtiene el agente actual desde cache
    /// </summary>
    public async Task<AgentMeCachedResult?> GetCachedAsync(CancellationToken ct = default)
    {
        try
        {
            var sql = @"
                SELECT 
                    agent_id,
                    agent_email,
                    agent_name,
                    agent_type,
                    is_active,
                    language,
                    time_zone,
                    mobile,
                    phone,
                    last_login_at,
                    freshdesk_created_at,
                    freshdesk_updated_at,
                    synced_at
                FROM pss_dvnx.freshdesk_agent_me_cache
                ORDER BY synced_at DESC
                LIMIT 1
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
                return new AgentMeCachedResult
                {
                    AgentId = reader.GetInt64(0),
                    AgentEmail = reader.IsDBNull(1) ? null : reader.GetString(1),
                    AgentName = reader.IsDBNull(2) ? null : reader.GetString(2),
                    AgentType = reader.IsDBNull(3) ? null : reader.GetString(3),
                    IsActive = reader.IsDBNull(4) ? null : reader.GetBoolean(4),
                    Language = reader.IsDBNull(5) ? null : reader.GetString(5),
                    TimeZone = reader.IsDBNull(6) ? null : reader.GetString(6),
                    Mobile = reader.IsDBNull(7) ? null : reader.GetString(7),
                    Phone = reader.IsDBNull(8) ? null : reader.GetString(8),
                    LastLoginAt = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
                    FreshdeskCreatedAt = reader.IsDBNull(10) ? null : reader.GetDateTime(10),
                    FreshdeskUpdatedAt = reader.IsDBNull(11) ? null : reader.GetDateTime(11),
                    SyncedAt = reader.GetDateTime(12)
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al obtener agente actual desde cache");
            throw;
        }
    }
}

/// <summary>
/// Resultado de la sincronización del agente actual
/// </summary>
public class AgentMeSyncResult
{
    public bool Success { get; set; }
    public long AgentId { get; set; }
    public string? AgentEmail { get; set; }
    public DateTime? FreshdeskUpdatedAt { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public int DurationMs { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Resultado del agente actual desde cache
/// </summary>
public class AgentMeCachedResult
{
    public long AgentId { get; set; }
    public string? AgentEmail { get; set; }
    public string? AgentName { get; set; }
    public string? AgentType { get; set; }
    public bool? IsActive { get; set; }
    public string? Language { get; set; }
    public string? TimeZone { get; set; }
    public string? Mobile { get; set; }
    public string? Phone { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? FreshdeskCreatedAt { get; set; }
    public DateTime? FreshdeskUpdatedAt { get; set; }
    public DateTime SyncedAt { get; set; }
}
