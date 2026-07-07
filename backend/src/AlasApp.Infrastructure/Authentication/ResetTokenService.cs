using AlasApp.Application.Abstractions.Services;
using System.Security.Cryptography;
using System.Text;

namespace AlasApp.Infrastructure.Authentication;

public sealed class ResetTokenService : IResetTokenService
{
    public string GenerateToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    public string HashToken(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim()));
        return Convert.ToHexString(bytes);
    }
}
