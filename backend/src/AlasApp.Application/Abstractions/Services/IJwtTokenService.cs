using AlasApp.Application.Auth.Models;

namespace AlasApp.Application.Abstractions.Services;

public interface IJwtTokenService
{
    LoginResultDto CreateAccessToken(AuthenticatedUserDto user, int tokenVersion, bool rememberMe);
}
