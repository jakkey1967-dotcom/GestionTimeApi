using Microsoft.Extensions.Caching.Memory;

namespace GestionTime.Api.Services;

public class ResetTokenService
{
    private readonly IMemoryCache _cache;
    private readonly Random _random = new();

    public ResetTokenService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public string GenerateToken()
    {
        return _random.Next(100000, 999999).ToString();
    }

    public void SaveToken(string token, Guid userId)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        };
        _cache.Set($"reset:{token}", userId, options);
    }

    public void SaveToken(string key, string value)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        };
        _cache.Set(key, value, options);
    }

    public Guid? ValidateAndGetUserId(string token)
    {
        if (_cache.TryGetValue($"reset:{token}", out Guid userId))
        {
            return userId;
        }
        return null;
    }

    public void RemoveToken(string token)
    {
        _cache.Remove($"reset:{token}");
    }

    // Métodos adicionales para registro
    public void SaveTokenWithData(string key, string jsonData)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        };
        _cache.Set(key, jsonData, options);
    }

    public string? GetToken(string key)
    {
        if (_cache.TryGetValue(key, out string? data))
        {
            return data;
        }
        return null;
    }

    public string? GetTokenData(string key)
    {
        if (_cache.TryGetValue(key, out string? data))
        {
            return data;
        }
        return null;
    }

    public void RemoveTokenByKey(string key)
    {
        _cache.Remove(key);
    }
}
