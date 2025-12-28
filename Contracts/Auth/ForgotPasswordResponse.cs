namespace GestionTime.Api.Contracts.Auth;

public record ForgotPasswordResponse(
    bool Success,
    string? Message,
    string? Error
);
