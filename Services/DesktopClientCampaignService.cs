using System.Globalization;
using GestionTime.Api.Contracts.Admin;
using GestionTime.Domain.Versioning;
using GestionTime.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestionTime.Api.Services;

/// <summary>Motor de campañas de email Desktop: detecta candidatos, encola con deduplicación semanal.</summary>
public sealed class DesktopClientCampaignService
{
    private readonly GestionTimeDbContext _db;
    private readonly ILogger<DesktopClientCampaignService> _logger;

    public DesktopClientCampaignService(GestionTimeDbContext db, ILogger<DesktopClientCampaignService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // GL-BEGIN: SemanticVersion
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

    // GL-BEGIN: RunCampaignAsync
    /// <summary>Ejecuta la campaña: detecta candidatos, encola emails con deduplicación.</summary>
    public async Task<DesktopCampaignRunResponse> RunCampaignAsync(bool dryRun, CancellationToken ct = default)
    {
        var latestRaw = await GetSettingAsync("latest_client_version_desktop", "2.0.2-beta", ct);
        var minRaw = await GetSettingAsync("min_client_version_desktop", "2.0.0", ct);
        var updateUrl = await GetSettingAsync("update_url_desktop", null, ct);
        var inactiveWeeksStr = await GetSettingAsync("desktop_inactive_weeks", "2", ct);
        var releaseUrl = await GetSettingAsync("desktop_release_url", null, ct);
        var highlights = await GetSettingAsync("desktop_release_highlights_md", null, ct);
        var inactiveWeeks = int.TryParse(inactiveWeeksStr, out var iw) ? iw : 2;

        SemanticVersion.TryParse(latestRaw, out var latestSemver);
        SemanticVersion.TryParse(minRaw, out var minSemver);

        var inactiveCutoff = DateTimeOffset.UtcNow.AddDays(-inactiveWeeks * 7);
        var periodKey = GetIsoWeekKey(DateTimeOffset.UtcNow);

        // Usuarios con versión Desktop
        var viewRows = await _db.Set<VDesktopClientLastVersion>().ToListAsync(ct);

        // Usuarios habilitados sin versión registrada (NEVER)
        var viewUserIds = viewRows.Select(v => v.UserId).ToHashSet();
        var neverUsers = await _db.Users
            .Where(u => u.Enabled && !viewUserIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName, u.Email })
            .ToListAsync(ct);

        var candidates = new List<CampaignCandidate>();

        foreach (var row in viewRows)
        {
            if (!SemanticVersion.TryParse(row.AppVersionRaw, out var userSemver) || userSemver == null)
            {
                candidates.Add(new CampaignCandidate(row.UserId, row.FullName, row.Email, "VERSION_REQUIRED",
                    minRaw, row.AppVersionRaw));
                continue;
            }

            if (minSemver != null && userSemver.CompareTo(minSemver) < 0)
            {
                candidates.Add(new CampaignCandidate(row.UserId, row.FullName, row.Email, "VERSION_REQUIRED",
                    minRaw, row.AppVersionRaw));
            }
            else if (row.LoggedAt < inactiveCutoff)
            {
                candidates.Add(new CampaignCandidate(row.UserId, row.FullName, row.Email, "INACTIVE",
                    null, row.AppVersionRaw));
            }
            else if (latestSemver != null && userSemver.CompareTo(latestSemver) < 0)
            {
                candidates.Add(new CampaignCandidate(row.UserId, row.FullName, row.Email, "VERSION_OUTDATED",
                    latestRaw, row.AppVersionRaw));
            }
        }

        foreach (var u in neverUsers)
        {
            candidates.Add(new CampaignCandidate(u.Id, u.FullName, u.Email, "NEVER", null, null));
        }

        int enqueued = 0, skipped = 0;

        foreach (var c in candidates)
        {
            var dedupeKey = $"{c.UserId}|{c.Kind}|Desktop|{c.TargetVersion ?? ""}|{periodKey}";

            // Verificar si ya existe
            var exists = await _db.EmailOutboxes
                .AnyAsync(e => e.DedupeKey == dedupeKey, ct);

            if (exists)
            {
                skipped++;
                continue;
            }

            var subject = BuildSubject(c.Kind, c.TargetVersion, c.CurrentVersion);
            var bodyPreview = BuildBodyPreview(c.Kind, c.FullName, c.CurrentVersion, c.TargetVersion,
                updateUrl, releaseUrl, highlights);

            if (!dryRun)
            {
                _db.EmailOutboxes.Add(new EmailOutbox
                {
                    UserId = c.UserId,
                    Kind = c.Kind,
                    Platform = "Desktop",
                    TargetVersionRaw = c.TargetVersion,
                    PeriodKey = periodKey,
                    DedupeKey = dedupeKey,
                    Subject = subject,
                    BodyPreview = bodyPreview?.Length > 500 ? bodyPreview[..500] : bodyPreview,
                    Status = "PENDING",
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }

            enqueued++;
        }

        if (!dryRun && enqueued > 0)
            await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Campaña Desktop {Mode}: period={Period} candidates={Candidates} enqueued={Enqueued} skipped={Skipped}",
            dryRun ? "DRY-RUN" : "REAL", periodKey, candidates.Count, enqueued, skipped);

        return new DesktopCampaignRunResponse
        {
            PeriodKey = periodKey,
            Candidates = candidates.Count,
            Enqueued = enqueued,
            Skipped = skipped,
            DryRun = dryRun
        };
    }
    // GL-END: RunCampaignAsync

    // GL-BEGIN: SendPendingAsync
    /// <summary>Envía emails PENDING usando IEmailSender. Actualiza status a SENT o ERROR.</summary>
    public async Task<(int Sent, int Errors)> SendPendingAsync(IEmailSender emailSender, CancellationToken ct = default)
    {
        var pending = await _db.EmailOutboxes
            .Include(e => e.User)
            .Where(e => e.Status == "PENDING")
            .OrderBy(e => e.CreatedAt)
            .Take(50) // Lote máximo por ejecución
            .ToListAsync(ct);

        int sent = 0, errors = 0;

        foreach (var entry in pending)
        {
            try
            {
                var toEmail = entry.User?.Email;
                if (string.IsNullOrEmpty(toEmail))
                {
                    entry.Status = "SKIPPED";
                    entry.Error = "Email vacío";
                    continue;
                }

                var htmlBody = BuildHtmlBody(entry);
                await emailSender.SendRawEmailAsync(toEmail, entry.Subject ?? "GestionTime Desktop", htmlBody, ct);

                entry.Status = "SENT";
                entry.SentAt = DateTimeOffset.UtcNow;
                sent++;
            }
            catch (Exception ex)
            {
                entry.Status = "ERROR";
                entry.Error = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
                errors++;
                _logger.LogWarning(ex, "Error enviando email outbox id={Id} userId={UserId}", entry.Id, entry.UserId);
            }
        }

        if (pending.Count > 0)
            await _db.SaveChangesAsync(ct);

        if (sent > 0 || errors > 0)
            _logger.LogInformation("SendPending: sent={Sent} errors={Errors}", sent, errors);

        return (sent, errors);
    }
    // GL-END: SendPendingAsync

    // GL-BEGIN: Helpers
    private static string GetIsoWeekKey(DateTimeOffset date)
    {
        var d = date.UtcDateTime;
        var week = ISOWeek.GetWeekOfYear(d);
        var year = ISOWeek.GetYear(d);
        return $"{year}-W{week:D2}";
    }

    private static string BuildSubject(string kind, string? targetVersion, string? currentVersion)
    {
        return kind switch
        {
            "VERSION_REQUIRED" => $"⚠️ GestionTime Desktop: actualización obligatoria a v{targetVersion}",
            "VERSION_OUTDATED" => $"📢 GestionTime Desktop: nueva versión v{targetVersion} disponible",
            "INACTIVE" => "⏰ GestionTime Desktop: hace tiempo que no te conectas",
            "NEVER" => "👋 GestionTime Desktop: aún no has registrado tu versión",
            _ => "GestionTime Desktop: información importante"
        };
    }

    private static string? BuildBodyPreview(string kind, string fullName, string? currentVersion,
        string? targetVersion, string? updateUrl, string? releaseUrl, string? highlights)
    {
        var name = string.IsNullOrWhiteSpace(fullName) ? "usuario" : fullName.Split(' ')[0];
        return kind switch
        {
            "VERSION_REQUIRED" => $"Hola {name}, tu versión {currentVersion} es inferior a la mínima {targetVersion}. Descarga: {updateUrl}",
            "VERSION_OUTDATED" => $"Hola {name}, hay una versión más reciente ({targetVersion}). Descarga: {updateUrl}",
            "INACTIVE" => $"Hola {name}, no hemos detectado actividad reciente en GestionTime Desktop.",
            "NEVER" => $"Hola {name}, aún no tenemos registro de tu versión Desktop. Descarga: {updateUrl}",
            _ => null
        };
    }

    private static string BuildHtmlBody(EmailOutbox entry)
    {
        var name = entry.User?.FullName ?? "usuario";
        var firstName = name.Split(' ')[0];
        var kind = entry.Kind;
        var target = entry.TargetVersionRaw;

        var messageBlock = kind switch
        {
            "VERSION_REQUIRED" => $@"
                <p>Tu versión actual de <strong>GestionTime Desktop</strong> es inferior a la mínima requerida (<strong>v{target}</strong>).</p>
                <p>Es <strong>obligatorio</strong> actualizar para seguir usando la aplicación correctamente.</p>",
            "VERSION_OUTDATED" => $@"
                <p>Hay una nueva versión de <strong>GestionTime Desktop</strong> disponible: <strong>v{target}</strong>.</p>
                <p>Te recomendamos actualizar para disfrutar de las últimas mejoras y correcciones.</p>",
            "INACTIVE" => @"
                <p>Hace tiempo que no detectamos actividad tuya en <strong>GestionTime Desktop</strong>.</p>
                <p>Si necesitas ayuda o tienes problemas técnicos, no dudes en contactar con soporte.</p>",
            "NEVER" => @"
                <p>Aún no tenemos registro de tu versión de <strong>GestionTime Desktop</strong>.</p>
                <p>Descarga e instala la última versión para comenzar a usar la aplicación.</p>",
            _ => "<p>Información importante sobre GestionTime Desktop.</p>"
        };

        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f9f9f9; }}
                    .header {{ background-color: #0B8C99; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}
                    .content {{ padding: 30px; background-color: white; border-radius: 0 0 8px 8px; }}
                    .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>GestionTime Desktop</h1>
                    </div>
                    <div class='content'>
                        <p>Hola <strong>{firstName}</strong>,</p>
                        {messageBlock}
                        <p>Saludos,<br/>El equipo de GestionTime</p>
                    </div>
                    <div class='footer'>
                        <p>Este es un correo automático, por favor no respondas a este mensaje.</p>
                        <p>&copy; 2025 GestionTime. Todos los derechos reservados.</p>
                    </div>
                </div>
            </body>
            </html>";
    }

    private async Task<string?> GetSettingAsync(string key, string? defaultValue, CancellationToken ct)
    {
        var setting = await _db.AppSettings
            .Where(s => s.Key == key)
            .FirstOrDefaultAsync(ct);
        return setting?.Value ?? defaultValue;
    }
    // GL-END: Helpers

    private sealed record CampaignCandidate(Guid UserId, string FullName, string Email, string Kind,
        string? TargetVersion, string? CurrentVersion);
}
