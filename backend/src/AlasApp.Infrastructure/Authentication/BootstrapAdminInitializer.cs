using AlasApp.Application.Abstractions.Services;
using AlasApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AlasApp.Domain.Entities;
using AlasApp.Domain.Enums;

namespace AlasApp.Infrastructure.Authentication;

public sealed class BootstrapAdminInitializer(
    AlasAppDbContext dbContext,
    IPasswordHasher passwordHasher,
    IOptions<BootstrapAdminOptions> options,
    ILogger<BootstrapAdminInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var bootstrapOptions = options.Value;
        if (!bootstrapOptions.Enabled)
        {
            return;
        }

        if (await dbContext.UserAccounts.AnyAsync(x => x.AdminRole == AdminRole.SuperAdmin, cancellationToken))
        {
            logger.LogInformation("BootstrapAdmin omitido porque ya existe un Super Admin.");
            return;
        }

        if (!IsConfigured(bootstrapOptions))
        {
            logger.LogWarning("BootstrapAdmin está habilitado pero la configuración está incompleta. No se creó el Super Admin.");
            return;
        }

        var normalizedEmail = bootstrapOptions.Email.Trim().ToLowerInvariant();
        if (await dbContext.UserAccounts.AnyAsync(x => x.Email == normalizedEmail, cancellationToken))
        {
            logger.LogWarning(
                "BootstrapAdmin no pudo crear el Super Admin porque el email configurado '{Email}' ya existe y no hay ningún Super Admin registrado.",
                normalizedEmail);
            return;
        }

        var userAccount = UserAccount.Create(
            normalizedEmail,
            passwordHasher.Hash(bootstrapOptions.Password),
            bootstrapOptions.Nombre,
            bootstrapOptions.Apellido,
            UserType.Espectador,
            string.Empty,
            PreferredLanguage.Espanol,
            false,
            true,
            false,
            null,
            AdminRole.SuperAdmin);

        await dbContext.UserAccounts.AddAsync(userAccount, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Se creó automáticamente el usuario bootstrap Super Admin '{Email}'.", normalizedEmail);
    }

    private static bool IsConfigured(BootstrapAdminOptions options)
    {
        return !string.IsNullOrWhiteSpace(options.Email)
            && !string.IsNullOrWhiteSpace(options.Password)
            && !string.IsNullOrWhiteSpace(options.Nombre)
            && !string.IsNullOrWhiteSpace(options.Apellido);
    }
}
