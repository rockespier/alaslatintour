using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.AdminUsers.Models;
using AlasApp.Application.Auth.Models;
using AlasApp.Domain.Entities;
using AlasApp.Domain.Enums;
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

    public async Task<IReadOnlyCollection<AdminUserDto>> ListAdminUsersAsync(CancellationToken cancellationToken)
    {
        var items = await dbContext.UserAccounts
            .AsNoTracking()
            .Where(x => x.AdminRole != null)
            .OrderBy(x => x.Nombre)
            .ThenBy(x => x.Apellido)
            .Select(x => new
            {
                x.Id,
                x.Nombre,
                x.Apellido,
                x.Email,
                x.AdminRole,
                x.IsActive,
                x.LastLoginAtUtc,
                x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return items.Select(x => new AdminUserDto(
            x.Id,
            BuildInitials(x.Nombre, x.Apellido),
            $"{x.Nombre} {x.Apellido}".Trim(),
            x.Email,
            x.AdminRole ?? AdminRole.Admin,
            x.IsActive ? AdminUserStatus.Activo : AdminUserStatus.Inactivo,
            x.LastLoginAtUtc,
            x.CreatedAtUtc))
            .ToList();
    }

    public async Task<AdminUserDto?> GetAdminUserByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var item = await dbContext.UserAccounts
            .AsNoTracking()
            .Where(x => x.Id == userId && x.AdminRole != null)
            .Select(x => new
            {
                x.Id,
                x.Nombre,
                x.Apellido,
                x.Email,
                x.AdminRole,
                x.IsActive,
                x.LastLoginAtUtc,
                x.CreatedAtUtc
            })
            .FirstOrDefaultAsync(cancellationToken);

        return item is null
            ? null
            : new AdminUserDto(
                item.Id,
                BuildInitials(item.Nombre, item.Apellido),
                $"{item.Nombre} {item.Apellido}".Trim(),
                item.Email,
                item.AdminRole ?? AdminRole.Admin,
                item.IsActive ? AdminUserStatus.Activo : AdminUserStatus.Inactivo,
                item.LastLoginAtUtc,
                item.CreatedAtUtc);
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

    public void Remove(UserAccount userAccount)
    {
        dbContext.UserAccounts.Remove(userAccount);
    }

    private static string BuildInitials(string nombre, string apellido)
    {
        var first = string.IsNullOrWhiteSpace(nombre) ? string.Empty : nombre.Trim()[0].ToString().ToUpperInvariant();
        var second = string.IsNullOrWhiteSpace(apellido) ? string.Empty : apellido.Trim()[0].ToString().ToUpperInvariant();
        return $"{first}{second}";
    }
}
