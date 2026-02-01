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
/// Servicio para sincronizar empresas de Freshdesk
/// </summary>
public class FreshdeskCompaniesSyncService
{
    private readonly FreshdeskClient _freshdeskClient;
    private readonly GestionTimeDbContext _dbContext;
    private readonly FreshdeskOptions _options;
    private readonly ILogger<FreshdeskCompaniesSyncService> _logger;

    public FreshdeskCompaniesSyncService(
        FreshdeskClient freshdeskClient,
        GestionTimeDbContext dbContext,
        IOptions<FreshdeskOptions> options,
        ILogger<FreshdeskCompaniesSyncService> logger)
    {
        _freshdeskClient = freshdeskClient;
        _dbContext = dbContext;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Sincroniza todas las empresas desde Freshdesk
    /// </summary>
    public async Task<CompaniesSyncResult> SyncAllAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var result = new CompaniesSyncResult
        {
            StartedAt = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("🏢 Iniciando sincronización completa de companies");

            // Asegurar que la tabla existe
            await EnsureTableExistsAsync(ct);

            int page = 1;
            int totalUpserts = 0;
            bool hasMorePages = true;
            List<CompanySample> samples = new();

            while (hasMorePages && !ct.IsCancellationRequested)
            {
                var companies = await _freshdeskClient.GetCompaniesPageAsync(page, _options.PerPage, ct);

                if (companies.Count == 0)
                {
                    _logger.LogInformation("   ✅ No hay más companies. Terminando sincronización.");
                    break;
                }

                result.PagesFetched++;

                // Guardar samples de las primeras 3 companies de la primera página
                if (page == 1 && samples.Count < 3)
                {
                    samples.AddRange(companies.Take(3).Select(c => new CompanySample
                    {
                        CompanyId = c.Id,
                        Name = c.Name
                    }));
                }

                // Hacer UPSERT de companies
                int upserted = await UpsertCompaniesAsync(companies, ct);
                totalUpserts += upserted;

                _logger.LogInformation("   📄 Página {Page}: {Count} companies procesadas ({Upserted} upserted, total: {Total})",
                    page, companies.Count, upserted, totalUpserts);

                // Si recibimos menos companies que perPage, no hay más páginas
                if (companies.Count < _options.PerPage)
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

            result.CompaniesUpserted = totalUpserts;
            result.SampleFirst3 = samples;

            sw.Stop();
            result.CompletedAt = DateTime.UtcNow;
            result.DurationMs = (int)sw.ElapsedMilliseconds;

            _logger.LogInformation("✅ Sincronización completada exitosamente");
            _logger.LogInformation("   📊 Páginas obtenidas: {Pages}", result.PagesFetched);
            _logger.LogInformation("   💾 Companies upserted: {Upserted}", totalUpserts);
            _logger.LogInformation("   ⏱️ Duración: {DurationMs}ms", result.DurationMs);

            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            result.CompletedAt = DateTime.UtcNow;
            result.DurationMs = (int)sw.ElapsedMilliseconds;
            result.Error = ex.Message;

            _logger.LogError(ex, "❌ Error en sincronización de companies");
            throw;
        }
    }

    /// <summary>
    /// Asegura que la tabla existe (CREATE TABLE IF NOT EXISTS)
    /// </summary>
    private async Task EnsureTableExistsAsync(CancellationToken ct)
    {
        var sql = @"
            CREATE TABLE IF NOT EXISTS pss_dvnx.freshdesk_companies_cache (
              company_id       bigint primary key,
              name             text not null,
              description      text null,
              note             text null,
              domains          text[] null,
              health_score     text null,
              account_tier     text null,
              renewal_date     timestamptz null,
              industry         text null,
              phone            text null,
              custom_fields    jsonb null,
              created_at       timestamptz null,
              updated_at       timestamptz null,
              raw              jsonb not null,
              synced_at        timestamptz not null default now()
            );

            CREATE INDEX IF NOT EXISTS ix_fd_companies_name
              ON pss_dvnx.freshdesk_companies_cache (name);

            CREATE INDEX IF NOT EXISTS ix_fd_companies_updated_at
              ON pss_dvnx.freshdesk_companies_cache (updated_at DESC);

            CREATE INDEX IF NOT EXISTS ix_fd_companies_synced_at
              ON pss_dvnx.freshdesk_companies_cache (synced_at DESC);
        ";

        var connection = _dbContext.Database.GetDbConnection();

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync(ct);

        _logger.LogInformation("✅ Tabla freshdesk_companies_cache verificada/creada");
    }

    /// <summary>
    /// Hace UPSERT de companies en freshdesk_companies_cache
    /// </summary>
    private async Task<int> UpsertCompaniesAsync(List<FreshdeskCompanyDto> companies, CancellationToken ct)
    {
        if (companies.Count == 0)
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

            foreach (var company in companies)
            {
                // Serializar todo el objeto como raw JSON
                var rawJson = JsonSerializer.Serialize(company);

                // Convertir domains a array de PostgreSQL
                var domainsArray = company.Domains?.ToArray() ?? Array.Empty<string>();

                // Convertir custom_fields a JSON
                var customFieldsJson = company.CustomFields != null
                    ? JsonSerializer.Serialize(company.CustomFields)
                    : null;

                var sql = @"
                    INSERT INTO pss_dvnx.freshdesk_companies_cache (
                        company_id, name, description, note, domains,
                        health_score, account_tier, renewal_date, industry, phone,
                        custom_fields, created_at, updated_at, raw, synced_at
                    )
                    VALUES (
                        @company_id, @name, @description, @note, @domains,
                        @health_score, @account_tier, @renewal_date, @industry, @phone,
                        @custom_fields::jsonb, @created_at, @updated_at, @raw::jsonb, NOW()
                    )
                    ON CONFLICT (company_id) 
                    DO UPDATE SET
                        name = EXCLUDED.name,
                        description = EXCLUDED.description,
                        note = EXCLUDED.note,
                        domains = EXCLUDED.domains,
                        health_score = EXCLUDED.health_score,
                        account_tier = EXCLUDED.account_tier,
                        renewal_date = EXCLUDED.renewal_date,
                        industry = EXCLUDED.industry,
                        phone = EXCLUDED.phone,
                        custom_fields = EXCLUDED.custom_fields,
                        created_at = EXCLUDED.created_at,
                        updated_at = EXCLUDED.updated_at,
                        raw = EXCLUDED.raw,
                        synced_at = NOW()
                ";

                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;
                cmd.Transaction = transaction;

                cmd.Parameters.Add(new NpgsqlParameter("@company_id", company.Id));
                cmd.Parameters.Add(new NpgsqlParameter("@name", company.Name));
                cmd.Parameters.Add(new NpgsqlParameter("@description", (object?)company.Description ?? DBNull.Value));
                cmd.Parameters.Add(new NpgsqlParameter("@note", (object?)company.Note ?? DBNull.Value));
                cmd.Parameters.Add(new NpgsqlParameter("@domains", domainsArray));
                cmd.Parameters.Add(new NpgsqlParameter("@health_score", (object?)company.HealthScore ?? DBNull.Value));
                cmd.Parameters.Add(new NpgsqlParameter("@account_tier", (object?)company.AccountTier ?? DBNull.Value));
                cmd.Parameters.Add(new NpgsqlParameter("@renewal_date", (object?)company.RenewalDate ?? DBNull.Value));
                cmd.Parameters.Add(new NpgsqlParameter("@industry", (object?)company.Industry ?? DBNull.Value));
                cmd.Parameters.Add(new NpgsqlParameter("@phone", DBNull.Value)); // Null por ahora
                cmd.Parameters.Add(new NpgsqlParameter("@custom_fields", (object?)customFieldsJson ?? DBNull.Value));
                cmd.Parameters.Add(new NpgsqlParameter("@created_at", (object?)company.CreatedAt ?? DBNull.Value));
                cmd.Parameters.Add(new NpgsqlParameter("@updated_at", (object?)company.UpdatedAt ?? DBNull.Value));
                cmd.Parameters.Add(new NpgsqlParameter("@raw", rawJson));

                await cmd.ExecuteNonQueryAsync(ct);
                upserted++;
            }

            await transaction.CommitAsync(ct);
            return upserted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al hacer UPSERT de {Count} companies", companies.Count);
            throw;
        }
    }

    /// <summary>
    /// Obtiene el status de la sincronización
    /// </summary>
    public async Task<CompaniesStatusResult> GetStatusAsync(CancellationToken ct = default)
    {
        try
        {
            var sql = @"
                SELECT 
                    COUNT(*) as total_count,
                    MAX(updated_at) as max_updated_at,
                    MAX(synced_at) as max_synced_at
                FROM pss_dvnx.freshdesk_companies_cache
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
                return new CompaniesStatusResult
                {
                    TotalCompanies = reader.GetInt32(0),
                    MaxUpdatedAt = reader.IsDBNull(1) ? null : reader.GetDateTime(1),
                    MaxSyncedAt = reader.IsDBNull(2) ? null : reader.GetDateTime(2)
                };
            }

            return new CompaniesStatusResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al obtener status de companies");
            throw;
        }
    }
}

/// <summary>
/// Resultado de la sincronización de companies
/// </summary>
public class CompaniesSyncResult
{
    public int PagesFetched { get; set; }
    public int CompaniesUpserted { get; set; }
    public int DurationMs { get; set; }
    public List<CompanySample> SampleFirst3 { get; set; } = new();
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Sample de una company
/// </summary>
public class CompanySample
{
    public long CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Status de la sincronización de companies
/// </summary>
public class CompaniesStatusResult
{
    public int TotalCompanies { get; set; }
    public DateTime? MaxUpdatedAt { get; set; }
    public DateTime? MaxSyncedAt { get; set; }
}
