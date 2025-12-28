namespace GestionTime.Domain.Auth;

public sealed class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string FullName { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public bool EmailConfirmed { get; set; } = false;
    
    // Control de expiración de contraseñas
    public DateTime? PasswordChangedAt { get; set; }
    public bool MustChangePassword { get; set; } = false;
    public int PasswordExpirationDays { get; set; } = 90; // Por defecto 90 días

    public List<UserRole> UserRoles { get; set; } = new();
    public List<RefreshToken> RefreshTokens { get; set; } = new();
    
    // Relación 1:1 con UserProfile
    public UserProfile? Profile { get; set; }
    
    // Propiedades calculadas para control de contraseñas
    public bool IsPasswordExpired => PasswordChangedAt.HasValue && 
        PasswordChangedAt.Value.AddDays(PasswordExpirationDays) < DateTime.UtcNow;
    
    public bool ShouldChangePassword => MustChangePassword || IsPasswordExpired;
    
    public int DaysUntilPasswordExpires => PasswordChangedAt.HasValue 
        ? Math.Max(0, PasswordExpirationDays - (DateTime.UtcNow - PasswordChangedAt.Value).Days)
        : 0;
}

