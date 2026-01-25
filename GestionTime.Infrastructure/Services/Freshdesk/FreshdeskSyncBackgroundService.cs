using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

namespace GestionTime.Infrastructure.Services.Freshdesk;

/// <summary>
/// Servicio en background que sincroniza tags de Freshdesk automáticamente
/// Configurable mediante variables de entorno:
/// - FRESHDESK__SYNCINTERVALHOURS: Intervalo en horas (default: 24)
/// - FRESHDESK__SYNCENABLED: true/false (default: true)
/// </summary>
public class FreshdeskSyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<FreshdeskSyncBackgroundService> _logger;
    private readonly FreshdeskOptions _options;

    public FreshdeskSyncBackgroundService(
        IServiceProvider services,
        ILogger<FreshdeskSyncBackgroundService> logger,
        IOptions<FreshdeskOptions> options)
    {
        _services = services;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.SyncEnabled)
        {
            _logger.LogInformation("🔕 Sincronización automática de Freshdesk DESHABILITADA (SyncEnabled=false)");
            return;
        }

        if (!_options.IsConfigured)
        {
            _logger.LogWarning("⚠️ Freshdesk no está configurado. Sincronización automática deshabilitada.");
            return;
        }

        var intervalHours = _options.SyncIntervalHours;
        if (intervalHours <= 0)
        {
            _logger.LogWarning("⚠️ SyncIntervalHours inválido: {Hours}. Usando default: 24 horas.", intervalHours);
            intervalHours = 24;
        }

        _logger.LogInformation("🔄 Sincronización automática de Freshdesk HABILITADA");
        _logger.LogInformation("   📅 Intervalo: cada {Hours} horas", intervalHours);
        _logger.LogInformation("   🌐 Domain: {Domain}", _options.Domain);

        // Esperar 1 minuto antes de la primera sincronización (dar tiempo a que la app arranque)
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("🔄 Iniciando sincronización automática de tags desde Freshdesk...");

                using (var scope = _services.CreateScope())
                {
                    var freshdeskService = scope.ServiceProvider.GetRequiredService<FreshdeskService>();
                    
                    // Sincronización automática: modo "recent" con 30 días
                    var result = await freshdeskService.SyncTagsFromFreshdeskAsync("recent", 30, 1000, stoppingToken);
                    
                    if (result.Success)
                    {
                        _logger.LogInformation("✅ Sincronización automática completada: {Inserted} nuevos, {Updated} actualizados ({Duration}ms)", 
                            result.TagsInserted, result.TagsUpdated, result.DurationMs);
                    }
                    else
                    {
                        _logger.LogError("❌ Sincronización automática falló: {Error}", result.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error en sincronización automática de Freshdesk. Se reintentará en {Hours} horas.", intervalHours);
            }

            // Esperar el intervalo configurado antes de la siguiente sincronización
            var delay = TimeSpan.FromHours(intervalHours);
            _logger.LogInformation("⏰ Próxima sincronización en {Hours} horas ({NextSync})", 
                intervalHours, DateTime.Now.Add(delay).ToString("yyyy-MM-dd HH:mm:ss"));
            
            await Task.Delay(delay, stoppingToken);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🛑 Deteniendo sincronización automática de Freshdesk");
        return base.StopAsync(cancellationToken);
    }
}
