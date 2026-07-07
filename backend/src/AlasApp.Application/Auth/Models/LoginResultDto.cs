namespace AlasApp.Application.Auth.Models;

public sealed record LoginResultDto(
    string AccessToken,
    int ExpiresIn,
    AuthenticatedUserDto User);
