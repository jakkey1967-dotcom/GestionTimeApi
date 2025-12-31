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
    
    // ==================== CONFIGURACIÓN DE BASE DE DATOS ====================
    
    /// <summary>
    /// Obtiene la configuración de base de datos del cliente actual
    /// </summary>
    public DatabaseConfig GetDatabaseConfig()
    {
        var client = GetCurrentClient();
        return client.Database ?? new DatabaseConfig { Schema = GetClientId() };
    }
    
    /// <summary>
    /// Obtiene el connection string del cliente actual (resuelve variables de entorno)
    /// </summary>
    public string GetConnectionString()
    {
        var dbConfig = GetDatabaseConfig();
        var connectionString = dbConfig.ConnectionString;
        
        // Reemplazar variables de entorno
        if (connectionString.Contains("${DATABASE_URL}"))
        {
            var dbUrl = Environment.GetEnvironmentVariable("DATABASE_URL") 
                        ?? _configuration.GetConnectionString("Default") 
                        ?? throw new InvalidOperationException("DATABASE_URL no configurado");
            connectionString = connectionString.Replace("${DATABASE_URL}", dbUrl);
        }
        
        return connectionString;
    }
    
    // ==================== CONFIGURACIÓN JWT ====================
    
    /// <summary>
    /// Obtiene la configuración JWT del cliente actual
    /// </summary>
    public JwtConfig GetJwtConfig()
    {
        var client = GetCurrentClient();
        var jwtConfig = client.Jwt ?? new JwtConfig();
        
        // Reemplazar variable de entorno para la clave
        if (jwtConfig.Key.Contains("${JWT_SECRET_KEY}"))
        {
            var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") 
                        ?? _configuration["Jwt:Key"] 
                        ?? throw new InvalidOperationException("JWT_SECRET_KEY no configurado");
            jwtConfig.Key = jwtConfig.Key.Replace("${JWT_SECRET_KEY}", jwtKey);
        }
        
        return jwtConfig;
    }
    
    // ==================== CONFIGURACIÓN CORS ====================
    
    /// <summary>
    /// Obtiene los orígenes CORS permitidos del cliente actual
    /// </summary>
    public string[] GetCorsOrigins()
    {
        var client = GetCurrentClient();
        return client.Cors?.Origins.ToArray() ?? Array.Empty<string>();
    }
    
    // ==================== CONFIGURACIÓN EMAIL ====================
    
    /// <summary>
    /// Obtiene la configuración de email del cliente actual
    /// </summary>
    public EmailConfig GetEmailConfig()
    {
        var client = GetCurrentClient();
        var emailConfig = client.Email ?? new EmailConfig();
        
        // Reemplazar variables de entorno
        if (emailConfig.SmtpHost.Contains("${SMTP_HOST}"))
        {
            emailConfig.SmtpHost = Environment.GetEnvironmentVariable("SMTP_HOST") 
                                  ?? _configuration["Email:SmtpHost"] 
                                  ?? "smtp.gmail.com";
        }
        
        if (emailConfig.SmtpUser.Contains("${SMTP_USER}"))
        {
            emailConfig.SmtpUser = Environment.GetEnvironmentVariable("SMTP_USER") 
                                  ?? _configuration["Email:SmtpUser"] 
                                  ?? throw new InvalidOperationException("SMTP_USER no configurado");
        }
        
        if (emailConfig.SmtpPassword.Contains("${SMTP_PASSWORD}"))
        {
            emailConfig.SmtpPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD") 
                                      ?? _configuration["Email:SmtpPassword"] 
                                      ?? throw new InvalidOperationException("SMTP_PASSWORD no configurado");
        }
        
        return emailConfig;
    }
    
    // ==================== CONFIGURACIÓN DE CARACTERÍSTICAS ====================
    
    /// <summary>
    /// Obtiene la configuración de características del cliente actual
    /// </summary>
    public FeaturesConfig GetFeaturesConfig()
    {
        return GetCurrentClient().Features ?? new FeaturesConfig();
    }
    
    /// <summary>
    /// Verifica si el cliente requiere confirmación de email
    /// </summary>
    public bool RequiresEmailConfirmation()
    {
        return GetFeaturesConfig().RequireEmailConfirmation;
    }
    
    /// <summary>
    /// Verifica si el cliente permite auto-registro
    /// </summary>
    public bool AllowsSelfRegistration()
    {
        return GetFeaturesConfig().AllowSelfRegistration;
    }
    
    /// <summary>
    /// Obtiene los días de expiración de contraseña del cliente
    /// </summary>
    public int GetPasswordExpirationDays()
    {
        return GetFeaturesConfig().PasswordExpirationDays;
    }
    
    // ==================== CONFIGURACIÓN DE BRANDING ====================
    
    /// <summary>
    /// Obtiene la configuración de branding del cliente actual
    /// </summary>
    public BrandingConfig GetBrandingConfig()
    {
        return GetCurrentClient().Branding ?? new BrandingConfig();
    }
    
    /// <summary>
    /// Obtiene el color primario del cliente
    /// </summary>
    public string GetPrimaryColor()
    {
        return GetBrandingConfig().PrimaryColor;
    }
    
    /// <summary>
    /// Obtiene el color secundario del cliente
    /// </summary>
    public string GetSecondaryColor()
    {
        return GetBrandingConfig().SecondaryColor;
    }
    
    /// <summary>
    /// Obtiene el nombre de la empresa del cliente
    /// </summary>
    public string GetCompanyName()
    {
        return GetBrandingConfig().CompanyName;
    }
    
    // ==================== CONFIGURACIÓN DE CONTACTO ====================
    
    /// <summary>
    /// Obtiene la configuración de contacto del cliente actual
    /// </summary>
    public ContactInfoConfig GetContactInfo()
    {
        return GetCurrentClient().ContactInfo ?? new ContactInfoConfig();
    }
    
    /// <summary>
    /// Obtiene el email de soporte del cliente
    /// </summary>
    public string GetSupportEmail()
    {
        return GetContactInfo().SupportEmail;
    }
    
    /// <summary>
    /// Obtiene el teléfono de soporte del cliente
    /// </summary>
    public string GetSupportPhone()
    {
        return GetContactInfo().SupportPhone;
    }
    
    // ==================== CONFIGURACIÓN DE LÍMITES ====================
    
    /// <summary>
    /// Obtiene los límites configurados del cliente actual
    /// </summary>
    public LimitsConfig GetLimits()
    {
        return GetCurrentClient().Limits ?? new LimitsConfig();
    }
    
    /// <summary>
    /// Obtiene el máximo de usuarios permitidos para el cliente
    /// </summary>
    public int GetMaxUsersAllowed()
    {
        return GetLimits().MaxUsersPerTenant;
    }
    
    /// <summary>
    /// Obtiene el máximo de almacenamiento en GB permitido para el cliente
    /// </summary>
    public int GetMaxStorageGB()
    {
        return GetLimits().MaxStorageGB;
    }
    
    /// <summary>
    /// Obtiene el máximo de peticiones API por minuto permitidas
    /// </summary>
    public int GetMaxApiRequestsPerMinute()
    {
        return GetLimits().MaxApiRequestsPerMinute;
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
    
    // Para deserializar el array de clientes
    public List<ClientConfig> Clients { get; set; } = new();
    
    // Configuración de base de datos
    public DatabaseConfig Database { get; set; } = new();
    
    // Configuración JWT
    public JwtConfig Jwt { get; set; } = new();
    
    // Configuración CORS
    public CorsConfig Cors { get; set; } = new();
    
    // Configuración de Email
    public EmailConfig Email { get; set; } = new();
    
    // Características del cliente
    public FeaturesConfig Features { get; set; } = new();
    
    // Branding personalizado
    public BrandingConfig Branding { get; set; } = new();
    
    // Información de contacto
    public ContactInfoConfig ContactInfo { get; set; } = new();
    
    // Límites del tenant
    public LimitsConfig Limits { get; set; } = new();
}

public class DatabaseConfig
{
    public string Schema { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
}

public class JwtConfig
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public int AccessMinutes { get; set; } = 15;
    public int RefreshDays { get; set; } = 14;
}

public class CorsConfig
{
    public List<string> Origins { get; set; } = new();
}

public class EmailConfig
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SmtpUser { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
}

public class FeaturesConfig
{
    public bool RequireEmailConfirmation { get; set; } = true;
    public bool AllowSelfRegistration { get; set; } = false;
    public int PasswordExpirationDays { get; set; } = 90;
    public int MaxLoginAttempts { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 30;
}

public class BrandingConfig
{
    public string CompanyName { get; set; } = string.Empty;
    public string PrimaryColor { get; set; } = "#0B8C99";
    public string SecondaryColor { get; set; } = "#0A7A85";
    public string LogoDark { get; set; } = string.Empty;
    public string LogoLight { get; set; } = string.Empty;
    public string Favicon { get; set; } = string.Empty;
}

public class ContactInfoConfig
{
    public string SupportEmail { get; set; } = string.Empty;
    public string SupportPhone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

public class LimitsConfig
{
    public int MaxUsersPerTenant { get; set; } = 100;
    public int MaxStorageGB { get; set; } = 50;
    public int MaxApiRequestsPerMinute { get; set; } = 1000;
}
