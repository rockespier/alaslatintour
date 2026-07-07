using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlasApp.Infrastructure.Persistence.Repositories;

public sealed class PasswordResetTokenRepository(AlasAppDbContext dbContext) : IPasswordResetTokenRepository
{
    public Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken)
    {
        return dbContext.PasswordResetTokens.AddAsync(token, cancellationToken).AsTask();
    }

    public Task<PasswordResetToken?> GetActiveByHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        return dbContext.PasswordResetTokens
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash && x.UsedAtUtc == null, cancellationToken);
    }

    public async Task ExpireActiveTokensAsync(Guid userId, CancellationToken cancellationToken)
    {
        var activeTokens = await dbContext.PasswordResetTokens
            .Where(x => x.UserAccountId == userId && x.UsedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.MarkUsed(DateTimeOffset.UtcNow);
            token.SetUpdated(DateTimeOffset.UtcNow);
        }
    }
}
