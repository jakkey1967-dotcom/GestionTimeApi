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
        var updateUrl = await GetSettingAsync("update_url_desktop", null, ct);
        var releaseUrl = await GetSettingAsync("desktop_release_url", null, ct);
        var highlights = await GetSettingAsync("desktop_release_highlights_md", null, ct);
        var latestRaw = await GetSettingAsync("latest_client_version_desktop", "2.0.2-beta", ct);

        var pending = await _db.EmailOutboxes
            .Include(e => e.User)
            .Where(e => e.Status == "PENDING")
            .OrderBy(e => e.CreatedAt)
            .Take(50)
            .ToListAsync(ct);

        // Resolver ruta del logo una vez
        var logoPath = ResolveLogoPath();
        var logoImages = logoPath != null
            ? new List<EmailLinkedImage> { new("gestiontime-logo", logoPath) }
            : null;

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

                var htmlBody = BuildHtmlBody(entry, updateUrl, releaseUrl, highlights, latestRaw);

                if (logoImages != null)
                    await emailSender.SendRawEmailWithImagesAsync(toEmail,
                        entry.Subject ?? "GestionTime Desktop", htmlBody, logoImages, ct);
                else
                    await emailSender.SendRawEmailAsync(toEmail,
                        entry.Subject ?? "GestionTime Desktop", htmlBody, ct);

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
            "VERSION_REQUIRED" => $"Actualización obligatoria - GestionTime Desktop v{targetVersion}",
            "VERSION_OUTDATED" => $"Nueva versión disponible - GestionTime Desktop v{targetVersion}",
            "INACTIVE" => "Hace tiempo que no te conectas - GestionTime Desktop",
            "NEVER" => "Registra tu versión - GestionTime Desktop",
            _ => "Información importante - GestionTime Desktop"
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

    private static string BuildHtmlBody(EmailOutbox entry, string? updateUrl, string? releaseUrl, string? highlights, string? latestVersion)
    {
        var name = entry.User?.FullName ?? "usuario";
        var firstName = name.Split(' ')[0];
        var kind = entry.Kind;
        var target = entry.TargetVersionRaw;
        var latest = latestVersion ?? target;
        var downloadUrl = releaseUrl ?? updateUrl ?? "#";

        // Bloque de mensaje principal según kind
        var messageHtml = kind switch
        {
            "VERSION_REQUIRED" => $@"
                <tr><td style=""padding:0 30px;"">
                    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#fff3cd;border-left:4px solid #d4a017;border-radius:4px;margin:20px 0;"">
                        <tr><td style=""padding:14px 18px;font-size:14px;color:#856404;"">
                            <strong>Acción requerida: actualiza para continuar.</strong><br/>
                            Tu versión actual es inferior a la mínima requerida (<strong>v{target}</strong>).
                        </td></tr>
                    </table>
                </td></tr>",
            "VERSION_OUTDATED" => $@"
                <tr><td style=""padding:0 30px;"">
                    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#d4edda;border-left:4px solid #28a745;border-radius:4px;margin:20px 0;"">
                        <tr><td style=""padding:14px 18px;font-size:14px;color:#155724;"">
                            Hay una nueva versión de GestionTime Desktop disponible: <strong>v{latest}</strong>.
                            Te recomendamos actualizar para disfrutar de las últimas mejoras.
                        </td></tr>
                    </table>
                </td></tr>",
            "INACTIVE" => @"
                <tr><td style=""padding:0 30px;"">
                    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#e2e3e5;border-left:4px solid #6c757d;border-radius:4px;margin:20px 0;"">
                        <tr><td style=""padding:14px 18px;font-size:14px;color:#383d41;"">
                            Hace tiempo que no detectamos actividad tuya en GestionTime Desktop.
                            Si necesitas ayuda o tienes problemas técnicos, contacta con soporte.
                        </td></tr>
                    </table>
                </td></tr>",
            "NEVER" => @"
                <tr><td style=""padding:0 30px;"">
                    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#cce5ff;border-left:4px solid #004085;border-radius:4px;margin:20px 0;"">
                        <tr><td style=""padding:14px 18px;font-size:14px;color:#004085;"">
                            Aún no tenemos registro de tu versión de GestionTime Desktop.
                            Descarga e instala la última versión para comenzar a usarla.
                        </td></tr>
                    </table>
                </td></tr>",
            _ => ""
        };

        // Bloque de mejoras (solo para VERSION_REQUIRED y VERSION_OUTDATED)
        var improvementsHtml = "";
        if (kind is "VERSION_REQUIRED" or "VERSION_OUTDATED")
        {
            improvementsHtml = $@"
                <tr><td style=""padding:10px 30px 0 30px;"">
                    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#f8f9fa;border-radius:6px;border:1px solid #dee2e6;"">
                        <tr><td style=""padding:18px 20px 6px 20px;"">
                            <p style=""margin:0 0 12px 0;font-size:15px;font-weight:bold;color:#1a1a2e;"">Mejoras incluidas en v{latest}</p>
                            <table cellpadding=""0"" cellspacing=""0"" style=""font-size:13px;color:#333;"">
                                <tr><td style=""padding:4px 0;vertical-align:top;width:20px;"">&#8226;</td>
                                    <td style=""padding:4px 0;"">Encuentra proyectos y clientes más rápido con el nuevo filtro avanzado (AutoSuggestBox con chips).</td></tr>
                                <tr><td style=""padding:4px 0;vertical-align:top;width:20px;"">&#8226;</td>
                                    <td style=""padding:4px 0;"">Visualiza tus horas semanales de un vistazo con la gráfica mejorada (horas en lugar de porcentajes).</td></tr>
                                <tr><td style=""padding:4px 0;vertical-align:top;width:20px;"">&#8226;</td>
                                    <td style=""padding:4px 0;"">Recibe avisos automáticos cuando haya una actualización disponible.</td></tr>
                                <tr><td style=""padding:4px 0;vertical-align:top;width:20px;"">&#8226;</td>
                                    <td style=""padding:4px 0;"">Selecciona elementos con un solo clic de ratón, sin necesidad de doble clic.</td></tr>
                                <tr><td style=""padding:4px 0;vertical-align:top;width:20px;"">&#8226;</td>
                                    <td style=""padding:4px 0;"">Nuevo botón Salir en la ventana de Informes para cerrar con comodidad.</td></tr>
                            </table>
                        </td></tr>
                    </table>
                </td></tr>";
        }

        // Bloque de pasos rápidos
        var stepsHtml = kind is "VERSION_REQUIRED" or "VERSION_OUTDATED" or "NEVER" ? $@"
                <tr><td style=""padding:20px 30px 0 30px;"">
                    <p style=""margin:0 0 10px 0;font-size:15px;font-weight:bold;color:#1a1a2e;"">Pasos rápidos</p>
                    <table cellpadding=""0"" cellspacing=""0"" style=""font-size:13px;color:#333;"">
                        <tr><td style=""padding:3px 8px 3px 0;vertical-align:top;font-weight:bold;color:#0B8C99;"">1.</td>
                            <td style=""padding:3px 0;"">Descarga el instalador desde el enlace de más abajo.</td></tr>
                        <tr><td style=""padding:3px 8px 3px 0;vertical-align:top;font-weight:bold;color:#0B8C99;"">2.</td>
                            <td style=""padding:3px 0;"">Cierra GestionTime Desktop si está abierto.</td></tr>
                        <tr><td style=""padding:3px 8px 3px 0;vertical-align:top;font-weight:bold;color:#0B8C99;"">3.</td>
                            <td style=""padding:3px 0;"">Ejecuta el instalador (.msi) y sigue las instrucciones.</td></tr>
                        <tr><td style=""padding:3px 8px 3px 0;vertical-align:top;font-weight:bold;color:#0B8C99;"">4.</td>
                            <td style=""padding:3px 0;"">Abre la aplicación e inicia sesión con tus credenciales habituales.</td></tr>
                    </table>
                </td></tr>" : "";

        // Botón CTA
        var buttonHtml = kind is not "INACTIVE" ? $@"
                <tr><td style=""padding:24px 30px 0 30px;text-align:center;"">
                    <table cellpadding=""0"" cellspacing=""0"" align=""center"">
                        <tr><td style=""background-color:#0B8C99;border-radius:6px;"">
                            <a href=""{downloadUrl}"" target=""_blank""
                               style=""display:inline-block;padding:14px 36px;color:#ffffff;font-size:15px;font-weight:bold;text-decoration:none;font-family:Arial,sans-serif;"">
                                Actualizar ahora (v{latest})
                            </a>
                        </td></tr>
                    </table>
                    <p style=""margin:14px 0 0 0;font-size:12px;color:#888;"">
                        Si el botón no funciona, copia y pega este enlace en tu navegador:
                    </p>
                    <p style=""margin:4px 0 0 0;font-size:11px;color:#0B8C99;word-break:break-all;"">
                        {downloadUrl}
                    </p>
                </td></tr>" : "";

        return $@"<!DOCTYPE html>
<html lang=""es"" xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <meta charset=""UTF-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>GestionTime Desktop</title>
</head>
<body style=""margin:0;padding:0;background-color:#eaeaea;font-family:Arial,Helvetica,sans-serif;"">
<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#eaeaea;"">
<tr><td align=""center"" style=""padding:30px 10px;"">

    <!-- Contenedor principal 600px -->
    <table role=""presentation"" width=""600"" cellpadding=""0"" cellspacing=""0""
           style=""background-color:#ffffff;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.08);"">

        <!-- HEADER oscuro con logo -->
        <tr><td style=""background-color:#1a1a2e;padding:28px 30px;text-align:center;"">
            <img src=""cid:gestiontime-logo"" alt=""GestionTime"" width=""180""
                 style=""display:block;margin:0 auto 12px auto;max-width:180px;height:auto;"" />
            <p style=""margin:0;font-size:18px;font-weight:bold;color:#ffffff;letter-spacing:0.5px;"">
                {(kind == "VERSION_REQUIRED" ? "Actualización obligatoria" :
                  kind == "VERSION_OUTDATED" ? "Nueva versión disponible" :
                  kind == "INACTIVE" ? "Te echamos de menos" :
                  kind == "NEVER" ? "Comienza a usar GestionTime" :
                  "Información importante")}
            </p>
        </td></tr>

        <!-- Barra teal decorativa -->
        <tr><td style=""background-color:#0B8C99;height:4px;font-size:0;line-height:0;"">&nbsp;</td></tr>

        <!-- SALUDO -->
        <tr><td style=""padding:28px 30px 0 30px;"">
            <p style=""margin:0;font-size:15px;color:#333;"">Hola <strong>{firstName}</strong>,</p>
        </td></tr>

        <!-- MENSAJE PRINCIPAL -->
        {messageHtml}

        <!-- MEJORAS -->
        {improvementsHtml}

        <!-- PASOS RÁPIDOS -->
        {stepsHtml}

        <!-- BOTÓN CTA -->
        {buttonHtml}

        <!-- CIERRE -->
        <tr><td style=""padding:28px 30px 0 30px;"">
            <p style=""margin:0;font-size:14px;color:#333;"">Saludos,<br/><strong>El equipo de GestionTime</strong></p>
        </td></tr>

        <!-- FOOTER -->
        <tr><td style=""padding:24px 30px;border-top:1px solid #eee;margin-top:20px;"">
            <p style=""margin:0;font-size:11px;color:#999;text-align:center;"">
                Este es un correo automático enviado por GestionTime. Por favor, no respondas a este mensaje.
            </p>
            <p style=""margin:6px 0 0 0;font-size:11px;color:#bbb;text-align:center;"">
                &copy; 2025 GestionTime. Todos los derechos reservados.
            </p>
        </td></tr>

    </table>

</td></tr>
</table>
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

    private static string? ResolveLogoPath()
    {
        var candidates = new[]
        {
            Path.Combine("wwwroot", "images", "LogoOscuro.png"),
            Path.Combine("wwwroot_pss_dvnx", "images", "LogoOscuro.png"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "images", "LogoOscuro.png"),
            Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "LogoOscuro.png")
        };
        return candidates.FirstOrDefault(File.Exists);
    }
    // GL-END: Helpers

    private sealed record CampaignCandidate(Guid UserId, string FullName, string Email, string Kind,
        string? TargetVersion, string? CurrentVersion);
}
