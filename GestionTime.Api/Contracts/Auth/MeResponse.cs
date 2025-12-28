namespace GestionTime.Api.Contracts.Auth;

public sealed record MeResponse(string Email, string[] Roles);
