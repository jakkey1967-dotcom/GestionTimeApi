namespace GestionTime.Infrastructure.Services.Freshdesk;

public class FreshdeskOptions
{
    public const string SectionName = "Freshdesk";
    
    public string Domain { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Intervalo de sincronización automática de tags (en horas)
    /// Default: 24 horas (1 vez al día)
    /// Variable de entorno: FRESHDESK__SYNCINTERVALHOURS
    /// </summary>
    public int SyncIntervalHours { get; set; } = 24;
    
    /// <summary>
    /// Habilitar/deshabilitar sincronización automática
    /// Default: true
    /// Variable de entorno: FRESHDESK__SYNCENABLED
    /// </summary>
    public bool SyncEnabled { get; set; } = true;
    
    /// <summary>
    /// Normaliza la BaseUrl asegurando que tenga https:// y trailing slash
    /// </summary>
    public string BaseUrl
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Domain))
                return string.Empty;
            
            var normalized = Domain.Trim();
            
            // Si no empieza con http, agregar https://
            if (!normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && 
                !normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                normalized = $"https://{normalized}";
            }
            
            // Si no termina con .freshdesk.com, agregarlo
            if (!normalized.Contains(".freshdesk.com", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.TrimEnd('/');
                normalized = $"{normalized}.freshdesk.com";
            }
            
            // Asegurar trailing slash
            if (!normalized.EndsWith('/'))
            {
                normalized += "/";
            }
            
            return normalized;
        }
    }
    
    public bool IsConfigured => !string.IsNullOrEmpty(Domain) && !string.IsNullOrEmpty(ApiKey);
}
