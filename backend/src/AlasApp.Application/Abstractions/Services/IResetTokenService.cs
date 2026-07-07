namespace AlasApp.Application.Abstractions.Services;

public interface IResetTokenService
{
    string GenerateToken();

    string HashToken(string token);
}
