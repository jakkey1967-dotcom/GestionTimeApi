using Serilog;
using Serilog.Events;

namespace GestionTime.Api.Logging;

/// <summary>
/// Configuración de Serilog para GestionTime con separación de logs informativos y errores
/// </summary>
public static class SerilogConfiguration
{
    /// <summary>
    /// Configura Serilog con dos archivos separados:
    /// - gestiontime.log: Information y Warning
    /// - gestiontimeerror.log: Debug (opcional), Error y Critical
    /// </summary>
    public static void ConfigureSerilog(WebApplicationBuilder builder)
    {
        var options = new GestionTimeLoggingOptions();
        builder.Configuration.GetSection(GestionTimeLoggingOptions.SectionName).Bind(options);

        // Asegurar que el directorio existe
        var logDirectory = Path.Combine(AppContext.BaseDirectory, options.LogDirectory);
        Directory.CreateDirectory(logDirectory);

        var infoLogPath = Path.Combine(logDirectory, options.InfoLogFileName);
        var errorLogPath = Path.Combine(logDirectory, options.ErrorLogFileName);

        var fileSizeBytes = options.MaxFileSizeMB * 1024L * 1024L;

        // Nivel mínimo para el archivo de errores
        var errorFileMinLevel = options.EnableDebugLogs 
            ? LogEventLevel.Debug 
            : LogEventLevel.Error;

        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "GestionTime.Api")
            .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName);

        // ? CONSOLA SIEMPRE HABILITADA (desarrollo y producción)
        loggerConfig.WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}");

        // Archivo informativo (Information + Warning)
        loggerConfig.WriteTo.Logger(lc => lc
            .Filter.ByIncludingOnly(evt => 
                evt.Level == LogEventLevel.Information || 
                evt.Level == LogEventLevel.Warning)
            .WriteTo.File(
                path: infoLogPath,
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true,
                fileSizeLimitBytes: fileSizeBytes,
                retainedFileCountLimit: options.RetainedFileCountLimit,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                shared: true));

        // Archivo de errores/debug (Debug + Error + Fatal)
        loggerConfig.WriteTo.Logger(lc => lc
            .Filter.ByIncludingOnly(evt => 
                evt.Level >= errorFileMinLevel && 
                evt.Level != LogEventLevel.Information && 
                evt.Level != LogEventLevel.Warning)
            .WriteTo.File(
                path: errorLogPath,
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true,
                fileSizeLimitBytes: fileSizeBytes,
                retainedFileCountLimit: options.RetainedFileCountLimit,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                shared: true));

        Log.Logger = loggerConfig.CreateLogger();

        builder.Host.UseSerilog();

        Log.Information("Sistema de logging inicializado. EnableDebugLogs={EnableDebugLogs}", options.EnableDebugLogs);
    }

    /// <summary>
    /// Cierra el logger de forma segura
    /// </summary>
    public static void CloseAndFlush()
    {
        Log.Information("Cerrando sistema de logging...");
        Log.CloseAndFlush();
    }
}
