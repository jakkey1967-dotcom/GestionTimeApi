using System.Text.Json;

namespace GestionTime.Api.Services;

/// <summary>
/// Servicio centralizado para gestionar la configuración de clientes multi-tenant
/// Lee desde clients.config.json y proporciona acceso unificado a todas las propiedades del cliente
/// </summary>
public class ClientConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private ClientConfig? _clientConfig;
    private readonly object _lock = new();

    public ClientConfigurationService(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    /// <summary>
    /// Obtiene el ID del cliente actual (schema de base de datos)
    /// </summary>
    public string GetClientId()
    {
        return Environment.GetEnvironmentVariable("DB_SCHEMA")
               ?? _configuration["Database:Schema"]
               ?? "pss_dvnx";
    }

    /// <summary>
    /// Obtiene la configuración completa del cliente actual
    /// </summary>
    public ClientConfig GetCurrentClient()
    {
        var clientId = GetClientId();
        var allClients = GetAllClients();
        
        return allClients.FirstOrDefault(c => c.Id == clientId)
               ?? new ClientConfig
               {
                   Id = clientId,
                   Name = clientId,
                   ApiUrl = _configuration["ApiUrl"] ?? "",
                   Logo = "LogoOscuro.png"
               };
    }

    /// <summary>
    /// Obtiene todos los clientes configurados
    /// </summary>
    public List<ClientConfig> GetAllClients()
    {
        if (_clientConfig != null)
            return _clientConfig.Clients;

        lock (_lock)
        {
            if (_clientConfig != null)
                return _clientConfig.Clients;

            try
            {
                var configPath = Path.Combine(Directory.GetCurrentDirectory(), "clients.config.json");
                
                if (!File.Exists(configPath))
                {
                    // Configuración por defecto si no existe el archivo
                    _clientConfig = new ClientConfig
                    {
                        Clients = new List<ClientConfig>
                        {
                            new ClientConfig
                            {
                                Id = "pss_dvnx",
                                Name = "GestionTime Global-retail.com",
                                ApiUrl = "https://gestiontimeapi.onrender.com",
                                Logo = "pss_dvnx_logo.png"
                            }
                        }
                    };
                    return _clientConfig.Clients;
                }

                var json = File.ReadAllText(configPath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                _clientConfig = JsonSerializer.Deserialize<ClientConfig>(json, options);
                
                if (_clientConfig?.Clients == null || _clientConfig.Clients.Count == 0)
                {
                    throw new InvalidOperationException("No se encontraron clientes configurados en clients.config.json");
                }

                return _clientConfig.Clients;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error al cargar clients.config.json: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Obtiene el schema de base de datos del cliente actual
    /// </summary>
    public string GetDatabaseSchema()
    {
        return GetClientId();
    }

    /// <summary>
    /// Obtiene el nombre descriptivo del cliente actual
    /// </summary>
    public string GetClientName()
    {
        return GetCurrentClient().Name;
    }

    /// <summary>
    /// Obtiene la URL de la API del cliente actual
    /// </summary>
    public string GetApiUrl()
    {
        return GetCurrentClient().ApiUrl;
    }

    /// <summary>
    /// Obtiene el nombre del archivo de logo del cliente actual
    /// </summary>
    public string GetLogoFileName()
    {
        return GetCurrentClient().Logo;
    }

    /// <summary>
    /// Obtiene la ruta completa del logo del cliente actual
    /// </summary>
    public string GetLogoPath()
    {
        return $"/images/{GetLogoFileName()}";
    }

    /// <summary>
    /// Obtiene el directorio wwwroot específico del cliente actual
    /// </summary>
    public string GetClientWwwrootPath()
    {
        var clientId = GetClientId();
        return Path.Combine(Directory.GetCurrentDirectory(), $"wwwroot-{clientId}");
    }

    /// <summary>
    /// Verifica si existe un directorio wwwroot específico para el cliente actual
    /// </summary>
    public bool HasClientSpecificWwwroot()
    {
        return Directory.Exists(GetClientWwwrootPath());
    }

    /// <summary>
    /// Obtiene la configuración de un cliente específico por ID
    /// </summary>
    public ClientConfig? GetClientById(string clientId)
    {
        return GetAllClients().FirstOrDefault(c => c.Id == clientId);
    }

    /// <summary>
    /// Verifica si un cliente existe en la configuración
    /// </summary>
    public bool ClientExists(string clientId)
    {
        return GetAllClients().Any(c => c.Id == clientId);
    }
}

/// <summary>
/// Modelo de configuración del cliente
/// </summary>
public class ClientConfig
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = string.Empty;
    public string Logo { get; set; } = string.Empty;
    public List<ClientConfig> Clients { get; set; } = new();
}
