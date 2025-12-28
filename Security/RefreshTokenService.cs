using System.Security.Cryptography;
using System.Text;

namespace GestionTime.Api.Security;

public sealed class RefreshTokenService
{
    private readonly IConfiguration _config;
    private readonly ILogger<RefreshTokenService> _logger;

    public RefreshTokenService(IConfiguration config, ILogger<RefreshTokenService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public (string RawToken, string TokenHash, DateTime ExpiresAtUtc) Create()
    {
        var days = int.Parse(_config["Jwt:RefreshDays"] ?? "14");

        // Token aleatorio fuerte (no GUID)
        var bytes = RandomNumberGenerator.GetBytes(64);
        var raw = Base64UrlEncode(bytes);

        var hash = Sha256Hex(raw);
        var expires = DateTime.UtcNow.AddDays(days);

        _logger.LogDebug("Refresh token generado, expira en {Days} días (hash: {HashPrefix}...)", days, hash[..8]);

        return (raw, hash, expires);
    }

    public static string Hash(string rawToken) => Sha256Hex(rawToken);

    private static string Sha256Hex(string input)
    {
        using var sha = SHA256.Create();
        var data = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(data).ToLowerInvariant(); // 64 chars
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        var s = Convert.ToBase64String(bytes);
        return s.Replace("+", "-").Replace("/", "_").Replace("=", "");
    }
}
