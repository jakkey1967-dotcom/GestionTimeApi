namespace GestionTime.Api.Contracts.Auth;

public record ChangePasswordRequest
{
    public string Email { get; init; } = "";
    public string CurrentPassword { get; init; } = "";
    public string NewPassword { get; init; } = "";
}

public record ChangePasswordResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = "";
    public string? Error { get; init; }
}