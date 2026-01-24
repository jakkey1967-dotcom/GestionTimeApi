using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace GestionTime.Api.Security;

public sealed class JwtService
{
    private readonly IConfiguration _config;
    private readonly ILogger<JwtService> _logger;

    public JwtService(IConfiguration config, ILogger<JwtService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public string CreateAccessToken(Guid userId, string email, IEnumerable<string> roles)
    {
        return CreateAccessToken(userId, email, roles, null);
    }

    public string CreateAccessToken(Guid userId, string email, IEnumerable<string> roles, Guid? sessionId)
    {
        var issuer = _config["Jwt:Issuer"]!;
        var audience = _config["Jwt:Audience"]!;
        var key = _config["Jwt:Key"]!;
        var minutes = int.Parse(_config["Jwt:AccessMinutes"] ?? "15");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, email)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        
        // Agregar sessionId si está presente (para tracking de presencia)
        if (sessionId.HasValue)
        {
            claims.Add(new Claim("sid", sessionId.Value.ToString()));
        }

        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(minutes),
            signingCredentials: creds);

        _logger.LogDebug("Access token generado para {UserId}, expira en {Minutes} minutos{SessionInfo}", 
            userId, minutes, sessionId.HasValue ? $" (session: {sessionId})" : "");

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
