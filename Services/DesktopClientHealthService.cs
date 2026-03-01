using GestionTime.Api.Contracts.Admin;
using GestionTime.Domain.Auth;
using GestionTime.Domain.Versioning;
using GestionTime.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestionTime.Api.Services;

/// <summary>Calcula el estado de salud de clientes Desktop por usuario.</summary>
public sealed class DesktopClientHealthService
{
    private readonly GestionTimeDbContext _db;
    private readonly ILogger<DesktopClientHealthService> _logger;

    public DesktopClientHealthService(GestionTimeDbContext db, ILogger<DesktopClientHealthService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // GL-BEGIN: SemanticVersion (compartido con ClientVersionService)
    /// <summary>Versión semántica simple compatible con major.minor.patch[-prerelease].</summary>
    private sealed record SemanticVersion(int Major, int Minor, int Patch, string? Prerelease)
    {
        public static bool TryParse(string? raw, out SemanticVersion? result)
        {
            result = null;
            if (string.IsNullOrWhiteSpace(raw)) return false;

            var dashIdx = raw.IndexOf('-');
            var versionCore = dashIdx >= 0 ? raw[..dashIdx] : raw;
            var prerelease = dashIdx >= 0 ? raw[(dashIdx + 1)..] : null;

            var parts = versionCore.Split('.');
            if (parts.Length < 3) return false;

            if (!int.TryParse(parts[0], out var major) ||
                !int.TryParse(parts[1], out var minor) ||
                !int.TryParse(parts[2], out var patch))
                return false;

            result = new SemanticVersion(major, minor, patch, prerelease);
            return true;
        }

        /// <summary>Negativo = this menor, 0 = igual, positivo = this mayor.</summary>
        public int CompareTo(SemanticVersion other)
        {
            if (Major != other.Major) return Major.CompareTo(other.Major);
            if (Minor != other.Minor) return Minor.CompareTo(other.Minor);
            if (Patch != other.Patch) return Patch.CompareTo(other.Patch);

            if (Prerelease == null && other.Prerelease == null) return 0;
            if (Prerelease == null) return 1;
            if (other.Prerelease == null) return -1;

            return string.Compare(Prerelease, other.Prerelease, StringComparison.OrdinalIgnoreCase);
        }
    }
    // GL-END: SemanticVersion

    // GL-BEGIN: GetHealthAsync
    /// <summary>Consulta paginada del estado de salud de clientes Desktop.</summary>
    public async Task<DesktopClientHealthResponse> GetHealthAsync(DesktopClientHealthQuery query, CancellationToken ct = default)
    {
        var latestRaw = await GetSettingAsync("latest_client_version_desktop", "2.0.2-beta", ct);
        var minRaw = await GetSettingAsync("min_client_version_desktop", "2.0.0", ct);
        var updateUrl = await GetSettingAsync("update_url_desktop", null, ct);
        var inactiveWeeksStr = await GetSettingAsync("desktop_inactive_weeks", "2", ct);
        var inactiveWeeks = int.TryParse(inactiveWeeksStr, out var iw) ? iw : 2;

        SemanticVersion.TryParse(latestRaw, out var latestSemver);
        SemanticVersion.TryParse(minRaw, out var minSemver);

        var inactiveCutoff = DateTimeOffset.UtcNow.AddDays(-inactiveWeeks * 7);

        // Obtener usuarios con última versión Desktop desde la vista
        var viewRows = await _db.Set<VDesktopClientLastVersion>().ToListAsync(ct);

        // Obtener todos los usuarios habilitados para detectar NEVER
        var allUsers = await _db.Users
            .Where(u => u.Enabled)
            .Select(u => new { u.Id, u.FullName, u.Email })
            .ToListAsync(ct);

        var viewByUser = viewRows.ToDictionary(v => v.UserId);

        // Construir lista completa con status
        var items = new List<DesktopClientHealthItemDto>();
        foreach (var user in allUsers)
        {
            string status;
            string? currentVersion = null;
            DateTimeOffset? lastSeen = null;
            string? machineName = null;
            string? osVersion = null;

            if (viewByUser.TryGetValue(user.Id, out var row))
            {
                currentVersion = row.AppVersionRaw;
                lastSeen = row.LoggedAt;
                machineName = row.MachineName;
                osVersion = row.OsVersion;

                if (!SemanticVersion.TryParse(row.AppVersionRaw, out var userSemver) || userSemver == null)
                {
                    status = "REQUIRED";
                }
                else if (minSemver != null && userSemver.CompareTo(minSemver) < 0)
                {
                    status = "REQUIRED";
                }
                else if (row.LoggedAt < inactiveCutoff)
                {
                    status = "INACTIVE";
                }
                else if (latestSemver != null && userSemver.CompareTo(latestSemver) < 0)
                {
                    status = "OUTDATED";
                }
                else
                {
                    status = "OK";
                }
            }
            else
            {
                status = "NEVER";
            }

            items.Add(new DesktopClientHealthItemDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                CurrentVersion = currentVersion,
                LastSeenAt = lastSeen,
                Status = status,
                MachineName = machineName,
                OsVersion = osVersion,
                UpdateUrl = status is "REQUIRED" or "OUTDATED" or "NEVER" ? updateUrl : null
            });
        }

        // Filtros
        if (query.AgentId.HasValue)
            items = items.Where(i => i.UserId == query.AgentId.Value).ToList();

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var q = query.Q.Trim().ToLowerInvariant();
            items = items.Where(i =>
                i.FullName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                i.Email.Contains(q, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var statusFilter = query.Status.Trim().ToUpperInvariant();
            items = items.Where(i => i.Status == statusFilter).ToList();
        }

        // Ordenar: primero los problemáticos
        var statusOrder = new Dictionary<string, int>
        {
            ["REQUIRED"] = 0, ["OUTDATED"] = 1, ["INACTIVE"] = 2,
            ["NEVER"] = 3, ["OK"] = 4
        };
        items = items
            .OrderBy(i => statusOrder.GetValueOrDefault(i.Status, 99))
            .ThenBy(i => i.FullName)
            .ToList();

        var total = items.Count;
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var paged = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new DesktopClientHealthResponse
        {
            GeneratedAt = DateTimeOffset.UtcNow,
            LatestVersion = latestRaw,
            MinVersion = minRaw,
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = paged
        };
    }
    // GL-END: GetHealthAsync

    // GL-BEGIN: GetEmailHistoryAsync
    /// <summary>Devuelve histórico de emails enviados a un usuario (paginado).</summary>
    public async Task<(List<EmailOutboxItemDto> Items, int Total)> GetEmailHistoryAsync(
        EmailOutboxQuery query, CancellationToken ct = default)
    {
        var baseQuery = _db.EmailOutboxes
            .Where(e => e.UserId == query.UserId)
            .OrderByDescending(e => e.CreatedAt);

        var total = await baseQuery.CountAsync(ct);

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var items = await baseQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EmailOutboxItemDto
            {
                Id = e.Id,
                Kind = e.Kind,
                Platform = e.Platform,
                TargetVersionRaw = e.TargetVersionRaw,
                PeriodKey = e.PeriodKey,
                Subject = e.Subject,
                BodyPreview = e.BodyPreview,
                Status = e.Status,
                SentAt = e.SentAt,
                Error = e.Error,
                CreatedAt = e.CreatedAt
            })
            .ToListAsync(ct);

        return (items, total);
    }
    // GL-END: GetEmailHistoryAsync

    private async Task<string?> GetSettingAsync(string key, string? defaultValue, CancellationToken ct)
    {
        var setting = await _db.AppSettings
            .Where(s => s.Key == key)
            .FirstOrDefaultAsync(ct);
        return setting?.Value ?? defaultValue;
    }
}
