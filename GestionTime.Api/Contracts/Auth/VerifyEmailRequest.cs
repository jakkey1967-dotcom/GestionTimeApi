namespace GestionTime.Api.Contracts.Auth;

public record VerifyEmailRequest(
    string Email,
    string Token
);
