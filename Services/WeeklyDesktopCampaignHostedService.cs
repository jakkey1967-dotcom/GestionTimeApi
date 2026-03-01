using GestionTime.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace GestionTime.Api.Services;

/// <summary>Job semanal que ejecuta la campaña de emails Desktop según desktop_send_dow + desktop_send_hour.</summary>
public sealed class WeeklyDesktopCampaignHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WeeklyDesktopCampaignHostedService> _logger;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(10);

    public WeeklyDesktopCampaignHostedService(IServiceScopeFactory scopeFactory, ILogger<WeeklyDesktopCampaignHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WeeklyDesktopCampaignHostedService iniciado (check cada {Min} min)", CheckInterval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndRunAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error en WeeklyDesktopCampaignHostedService loop");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task CheckAndRunAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GestionTimeDbContext>();

        // Leer configuración de día y hora
        var sendDow = (await db.AppSettings.FindAsync(new object[] { "desktop_send_dow" }, ct))?.Value ?? "MON";
        var sendHourStr = (await db.AppSettings.FindAsync(new object[] { "desktop_send_hour" }, ct))?.Value ?? "09";
        var sendHour = int.TryParse(sendHourStr, out var h) ? h : 9;

        // Hora en Europe/Madrid
        var madridTz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");
        var nowMadrid = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, madridTz);

        var targetDow = ParseDayOfWeek(sendDow);
        if (nowMadrid.DayOfWeek != targetDow || nowMadrid.Hour != sendHour)
            return;

        // Verificar que no se ha ejecutado ya en esta hora (usar period_key como control)
        var campaign = scope.ServiceProvider.GetRequiredService<DesktopClientCampaignService>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

        _logger.LogInformation("WeeklyDesktopCampaign: ejecutando campaña ({Dow} {Hour}:00 Madrid)", sendDow, sendHour);

        var result = await campaign.RunCampaignAsync(dryRun: false, ct);
        _logger.LogInformation("WeeklyDesktopCampaign: enqueued={Enqueued} skipped={Skipped}", result.Enqueued, result.Skipped);

        var (sent, errors) = await campaign.SendPendingAsync(emailSender, ct);
        _logger.LogInformation("WeeklyDesktopCampaign: sent={Sent} errors={Errors}", sent, errors);
    }

    private static DayOfWeek ParseDayOfWeek(string dow) => dow.ToUpperInvariant() switch
    {
        "MON" => DayOfWeek.Monday,
        "TUE" => DayOfWeek.Tuesday,
        "WED" => DayOfWeek.Wednesday,
        "THU" => DayOfWeek.Thursday,
        "FRI" => DayOfWeek.Friday,
        "SAT" => DayOfWeek.Saturday,
        "SUN" => DayOfWeek.Sunday,
        _ => DayOfWeek.Monday
    };
}
