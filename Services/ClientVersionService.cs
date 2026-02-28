using GestionTime.Api.Contracts.ClientVersion;
using GestionTime.Domain.Versioning;
using GestionTime.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestionTime.Api.Services;

/// <summary>Gestiona el registro y comparación de versiones del cliente Desktop.</summary>
public sealed class ClientVersionService
{
    private readonly GestionTimeDbContext _db;
    private readonly ILogger<ClientVersionService> _logger;

    public ClientVersionService(GestionTimeDbContext db, ILogger<ClientVersionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // GL-BEGIN: SemanticVersion
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

            // semver: stable > prerelease
            if (Prerelease == null && other.Prerelease == null) return 0;
            if (Prerelease == null) return 1;
            if (other.Prerelease == null) return -1;

            return string.Compare(Prerelease, other.Prerelease, StringComparison.OrdinalIgnoreCase);
        }
    }
    // GL-END: SemanticVersion

    // GL-BEGIN: Register
    /// <summary>Registra la versión del cliente y calcula si requiere actualización.</summary>
    public async Task<RegisterVersionResponse> RegisterAsync(
        Guid userId,
        RegisterVersionRequest req,
        CancellationToken ct = default)
    {
        var platform = string.IsNullOrWhiteSpace(req.Platform) ? "Desktop" : req.Platform.Trim();
        var suffix = $"_{platform.ToLowerInvariant()}";

        var parsed = SemanticVersion.TryParse(req.AppVersion, out var clientSemver);

        var entry = new ClientVersion
        {
            UserId = userId,
            Platform = platform,
            AppVersionRaw = req.AppVersion,
            VerMajor = parsed ? clientSemver!.Major : 0,
            VerMinor = parsed ? clientSemver!.Minor : 0,
            VerPatch = parsed ? clientSemver!.Patch : 0,
            VerPrerelease = parsed ? clientSemver!.Prerelease : null,
            OsVersion = req.OsVersion,
            MachineName = req.MachineName,
            LoggedAt = DateTimeOffset.UtcNow
        };
        _db.ClientVersions.Add(entry);
        await _db.SaveChangesAsync(ct);

        var minRaw = await GetSettingAsync($"min_client_version{suffix}", "2.0.0", ct);
        var latestRaw = await GetSettingAsync($"latest_client_version{suffix}", null, ct);
        var updateUrl = await GetSettingAsync($"update_url{suffix}", null, ct);

        if (!parsed)
        {
            _logger.LogWarning("Versión no parseable: {Raw} userId={UserId}", req.AppVersion, userId);
            return new RegisterVersionResponse
            {
                Ok = true,
                UpdateRequired = true,
                UpdateAvailable = true,
                MinRequiredVersion = minRaw ?? "2.0.0",
                LatestVersion = latestRaw,
                UpdateUrl = updateUrl,
                Message = $"No se pudo determinar tu versión '{req.AppVersion}'. Actualiza a la versión mínima {minRaw}."
            };
        }

        SemanticVersion.TryParse(minRaw, out var minSemver);
        SemanticVersion.TryParse(latestRaw, out var latestSemver);

        var updateRequired = minSemver != null && clientSemver!.CompareTo(minSemver) < 0;
        var updateAvailable = latestSemver != null && clientSemver!.CompareTo(latestSemver) < 0;

        string? message = null;
        if (updateRequired)
            message = $"Tu versión {req.AppVersion} es inferior a la mínima requerida {minRaw}. Actualiza.";
        else if (updateAvailable)
            message = $"Hay una versión más reciente disponible ({latestRaw}).";

        _logger.LogInformation(
            "ClientVersion registrada: userId={UserId} ver={Ver} platform={Platform} updateRequired={Req} updateAvailable={Avail}",
            userId, req.AppVersion, platform, updateRequired, updateAvailable);

        return new RegisterVersionResponse
        {
            Ok = true,
            UpdateRequired = updateRequired,
            UpdateAvailable = updateAvailable,
            MinRequiredVersion = minRaw ?? "2.0.0",
            LatestVersion = latestRaw,
            UpdateUrl = (updateRequired || updateAvailable) ? updateUrl : null,
            Message = message
        };
    }
    // GL-END: Register

    // GL-BEGIN: GetLatestPerUser
    /// <summary>Devuelve la última versión registrada por cada usuario.</summary>
    public async Task<List<ClientVersionSummaryDto>> GetLatestPerUserAsync(CancellationToken ct = default)
    {
        // Paso 1: obtener la fecha máxima por usuario
        var maxPerUser = await _db.ClientVersions
            .GroupBy(cv => cv.UserId)
            .Select(g => new { UserId = g.Key, MaxLoggedAt = g.Max(x => x.LoggedAt) })
            .ToListAsync(ct);

        if (maxPerUser.Count == 0) return new List<ClientVersionSummaryDto>();

        var userIds = maxPerUser.Select(x => x.UserId).ToList();

        // Paso 2: cargar registros de esos usuarios con navegación a User
        var candidates = await _db.ClientVersions
            .Include(cv => cv.User)
            .Where(cv => userIds.Contains(cv.UserId))
            .ToListAsync(ct);

        // Paso 3: en memoria, quedarse solo con la fila más reciente por usuario
        var maxLookup = maxPerUser.ToDictionary(x => x.UserId, x => x.MaxLoggedAt);

        return candidates
            .Where(cv => cv.LoggedAt == maxLookup[cv.UserId])
            .Select(cv => new ClientVersionSummaryDto
            {
                UserId = cv.UserId,
                FullName = cv.User?.FullName ?? "",
                Email = cv.User?.Email ?? "",
                AppVersionRaw = cv.AppVersionRaw,
                Platform = cv.Platform,
                OsVersion = cv.OsVersion,
                MachineName = cv.MachineName,
                LoggedAt = cv.LoggedAt
            })
            .OrderBy(x => x.FullName)
            .ToList();
    }
    // GL-END: GetLatestPerUser

    // GL-BEGIN: GetOutdated
    /// <summary>Devuelve usuarios cuya última versión está por debajo del mínimo requerido.</summary>
    public async Task<List<ClientVersionSummaryDto>> GetOutdatedAsync(CancellationToken ct = default)
    {
        var minRaw = await GetSettingAsync("min_client_version_desktop", "2.0.0", ct);
        if (!SemanticVersion.TryParse(minRaw, out var minSemver) || minSemver == null)
            return new List<ClientVersionSummaryDto>();

        var all = await GetLatestPerUserAsync(ct);

        return all.Where(dto =>
        {
            if (!SemanticVersion.TryParse(dto.AppVersionRaw, out var v) || v == null)
                return true; // no parseable → considerar desactualizado
            return v.CompareTo(minSemver) < 0;
        }).ToList();
    }
    // GL-END: GetOutdated

    private async Task<string?> GetSettingAsync(string key, string? defaultValue, CancellationToken ct)
    {
        var setting = await _db.AppSettings
            .Where(s => s.Key == key)
            .FirstOrDefaultAsync(ct);
        return setting?.Value ?? defaultValue;
    }
}
