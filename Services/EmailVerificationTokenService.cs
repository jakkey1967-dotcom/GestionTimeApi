using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text;

namespace GestionTime.Api.Services;

/// <summary>
/// Servicio mejorado para gestión de tokens de verificación de email
/// </summary>
public class EmailVerificationTokenService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<EmailVerificationTokenService> _logger;
    
    // Configuración de expiración
    private readonly TimeSpan _tokenExpiration = TimeSpan.FromHours(24);

    public EmailVerificationTokenService(
        IMemoryCache cache, 
        ILogger<EmailVerificationTokenService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Genera un token seguro de verificación para un usuario
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <param name="email">Email del usuario</param>
    /// <returns>Token único y seguro</returns>
    public string GenerateVerificationToken(Guid userId, string email)
    {
        try
        {
            // Crear datos únicos combinando userId, email y timestamp
            var data = $"{userId}|{email.ToLowerInvariant()}|{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
            var dataBytes = Encoding.UTF8.GetBytes(data);

            // Generar hash SHA256
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(dataBytes);

            // Convertir a Base64 URL-safe
            var token = Convert.ToBase64String(hashBytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", "");

            // Guardar en cache con expiración
            var cacheKey = $"email_verification:{token}";
            var tokenData = new VerificationTokenData
            {
                UserId = userId,
                Email = email.ToLowerInvariant(),
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.Add(_tokenExpiration)
            };

            _cache.Set(cacheKey, tokenData, _tokenExpiration);

            _logger.LogInformation("Token de verificación generado para usuario {UserId} ({Email}). Expira en {Hours}h", 
                userId, email, _tokenExpiration.TotalHours);

            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando token de verificación para {UserId} ({Email})", userId, email);
            throw;
        }
    }

    /// <summary>
    /// Valida un token de verificación y retorna los datos del usuario
    /// </summary>
    /// <param name="token">Token a validar</param>
    /// <returns>Datos del token si es válido, null si es inválido o expirado</returns>
    public VerificationTokenData? ValidateVerificationToken(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("Token de verificación vacío o nulo");
            return null;
        }

        try
        {
            var cacheKey = $"email_verification:{token}";
            
            if (_cache.TryGetValue(cacheKey, out VerificationTokenData? tokenData))
            {
                if (tokenData != null && tokenData.ExpiresAt > DateTimeOffset.UtcNow)
                {
                    _logger.LogInformation("Token de verificación válido para usuario {UserId} ({Email})", 
                        tokenData.UserId, tokenData.Email);
                    return tokenData;
                }
                else
                {
                    _logger.LogWarning("Token de verificación expirado: {Token}", token[..8] + "...");
                    // Remover token expirado
                    _cache.Remove(cacheKey);
                }
            }
            else
            {
                _logger.LogWarning("Token de verificación no encontrado o inválido: {Token}", token[..8] + "...");
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validando token de verificación: {Token}", token[..8] + "...");
            return null;
        }
    }

    /// <summary>
    /// Consume (elimina) un token después de usarlo exitosamente
    /// </summary>
    /// <param name="token">Token a consumir</param>
    public void ConsumeToken(string token)
    {
        try
        {
            var cacheKey = $"email_verification:{token}";
            _cache.Remove(cacheKey);
            
            _logger.LogInformation("Token de verificación consumido: {Token}", token[..8] + "...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consumiendo token: {Token}", token[..8] + "...");
        }
    }

    /// <summary>
    /// Genera una URL completa de activación
    /// </summary>
    /// <param name="baseUrl">URL base del backend (ej: https://localhost:2501)</param>
    /// <param name="token">Token de verificación</param>
    /// <returns>URL completa de activación</returns>
    public string GenerateActivationUrl(string baseUrl, string token)
    {
        var cleanBaseUrl = baseUrl.TrimEnd('/');
        return $"{cleanBaseUrl}/api/v1/auth/activate/{token}";
    }

    /// <summary>
    /// Genera una URL completa para el logo del email
    /// </summary>
    /// <param name="baseUrl">URL base del backend</param>
    /// <returns>URL completa del logo</returns>
    public string GenerateLogoUrl(string baseUrl)
    {
        var cleanBaseUrl = baseUrl.TrimEnd('/');
        return $"{cleanBaseUrl}/images/LogoOscuro.png";
    }
}

/// <summary>
/// Datos almacenados para cada token de verificación
/// </summary>
public record VerificationTokenData
{
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
}