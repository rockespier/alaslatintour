using AlasApp.Domain.Entities;

namespace AlasApp.Application.Abstractions.Persistence;

public interface IPasswordResetTokenRepository
{
    Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken);

    Task<PasswordResetToken?> GetActiveByHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task ExpireActiveTokensAsync(Guid userId, CancellationToken cancellationToken);
}
