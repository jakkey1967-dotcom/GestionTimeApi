namespace GestionTime.Api.Logging;

/// <summary>
/// Configuración del sistema de logging de GestionTime
/// </summary>
public sealed class GestionTimeLoggingOptions
{
    public const string SectionName = "Logging:GestionTime";

    /// <summary>
    /// Activa los logs de nivel Debug en gestiontimeerror.log
    /// </summary>
    public bool EnableDebugLogs { get; set; } = false;

    /// <summary>
    /// Directorio donde se guardan los archivos de log
    /// </summary>
    public string LogDirectory { get; set; } = "logs";

    /// <summary>
    /// Tamaño máximo de archivo en MB antes de rotar
    /// </summary>
    public int MaxFileSizeMB { get; set; } = 10;

    /// <summary>
    /// Número de archivos históricos a mantener
    /// </summary>
    public int RetainedFileCountLimit { get; set; } = 5;

    /// <summary>
    /// Nombre del archivo de logs informativos
    /// </summary>
    public string InfoLogFileName { get; set; } = "gestiontime.log";

    /// <summary>
    /// Nombre del archivo de logs de errores/debug
    /// </summary>
    public string ErrorLogFileName { get; set; } = "gestiontimeerror.log";
}
