using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Auth.Models;
using AlasApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlasApp.Infrastructure.Persistence.Repositories;

public sealed class UserAccountRepository(AlasAppDbContext dbContext) : IUserAccountRepository
{
    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return dbContext.UserAccounts.AnyAsync(x => x.Email == normalizedEmail, cancellationToken);
    }

    public Task<UserAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return dbContext.UserAccounts.FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);
    }

    public Task<UserAccount?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return dbContext.UserAccounts.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
    }

    public async Task<AuthenticatedUserDto?> GetAuthenticatedUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var item = await dbContext.UserAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        return item is null
            ? null
            : new AuthenticatedUserDto(
                item.Id,
                item.Email,
                item.FullName,
                item.Tipo,
                item.AdminRole,
                item.CompetitorId);
    }

    public Task AddAsync(UserAccount userAccount, CancellationToken cancellationToken)
    {
        return dbContext.UserAccounts.AddAsync(userAccount, cancellationToken).AsTask();
    }
}
