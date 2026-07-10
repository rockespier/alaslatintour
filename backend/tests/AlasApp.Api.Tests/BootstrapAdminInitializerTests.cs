using AlasApp.Application.Abstractions.Services;
using AlasApp.Domain.Enums;
using AlasApp.Infrastructure.Authentication;
using AlasApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AlasApp.Api.Tests;

public sealed class BootstrapAdminInitializerTests
{
    [Fact]
    public async Task InitializeAsync_ShouldCreateSuperAdmin_WhenEnabledAndMissing()
    {
        await using var dbContext = CreateDbContext();
        IPasswordHasher passwordHasher = new Pbkdf2PasswordHasher();
        var options = Options.Create(new BootstrapAdminOptions
        {
            Enabled = true,
            Email = "superadmin@alas.test",
            Password = "Password1!",
            Nombre = "Super",
            Apellido = "Admin"
        });

        var initializer = new BootstrapAdminInitializer(dbContext, passwordHasher, options, NullLogger<BootstrapAdminInitializer>.Instance);

        await initializer.InitializeAsync(CancellationToken.None);

        var user = await dbContext.UserAccounts.SingleAsync();
        Assert.Equal("superadmin@alas.test", user.Email);
        Assert.Equal(AdminRole.SuperAdmin, user.AdminRole);
        Assert.True(passwordHasher.Verify("Password1!", user.PasswordHash));
    }

    [Fact]
    public async Task InitializeAsync_ShouldBeIdempotent_WhenSuperAdminAlreadyExists()
    {
        await using var dbContext = CreateDbContext();
        IPasswordHasher passwordHasher = new Pbkdf2PasswordHasher();
        var options = Options.Create(new BootstrapAdminOptions
        {
            Enabled = true,
            Email = "superadmin@alas.test",
            Password = "Password1!",
            Nombre = "Super",
            Apellido = "Admin"
        });

        var initializer = new BootstrapAdminInitializer(dbContext, passwordHasher, options, NullLogger<BootstrapAdminInitializer>.Instance);

        await initializer.InitializeAsync(CancellationToken.None);
        await initializer.InitializeAsync(CancellationToken.None);

        Assert.Equal(1, await dbContext.UserAccounts.CountAsync());
    }

    private static AlasAppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AlasAppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AlasAppDbContext(options);
    }
}
