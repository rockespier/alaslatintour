using AlasApp.Application.Auth.Models;
using AlasApp.Domain.Entities;

namespace AlasApp.Application.Abstractions.Persistence;

public interface IUserAccountRepository
{
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);

    Task<UserAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    Task<UserAccount?> GetByIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<AuthenticatedUserDto?> GetAuthenticatedUserAsync(Guid userId, CancellationToken cancellationToken);

    Task AddAsync(UserAccount userAccount, CancellationToken cancellationToken);
}
