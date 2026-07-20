using AlasApp.Infrastructure.Persistence;
using AlasApp.Application.Abstractions.Services;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlasApp.Api.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string TestDatabaseName = "AlasAppTests";

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        if (UseRelationalDatabaseInitialization)
        {
            using var scope = host.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AlasAppDbContext>();
            EnsureTestDatabaseExists(dbContext);
            dbContext.Database.Migrate();
            ResetDatabase(dbContext);
        }

        return host;
    }

    protected virtual bool UseRelationalDatabaseInitialization => true;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddJsonFile(
                Path.Combine(AppContext.BaseDirectory, "appsettings.Testing.json"),
                optional: false,
                reloadOnChange: false);
        });
        builder.ConfigureServices((context, services) =>
        {
            var connectionString = BuildTestDatabaseConnectionString(context.Configuration);

            services.RemoveAll<DbContextOptions<AlasAppDbContext>>();
            services.RemoveAll<IEmailSender>();
            services.RemoveAll<IIdentityDocumentStorage>();
            services.AddDbContext<AlasAppDbContext>(options => options.UseSqlServer(connectionString));
            services.AddSingleton<IEmailSender, NoOpEmailSender>();
            services.AddSingleton<IIdentityDocumentStorage, NoOpIdentityDocumentStorage>();
            ConfigureTestServices(services);
        });
    }

    protected virtual void ConfigureTestServices(IServiceCollection services)
    {
    }

    protected static void RemoveDbContextRegistrations(IServiceCollection services)
    {
        services.RemoveAll<AlasAppDbContext>();
        services.RemoveAll<DbContextOptions<AlasAppDbContext>>();
        services.RemoveAll<DbContextOptions>();
        services.RemoveAll<IConfigureOptions<DbContextOptions<AlasAppDbContext>>>();
        services.RemoveAll<IConfigureOptions<DbContextOptions>>();
        services.RemoveAll<IDbContextOptionsConfiguration<AlasAppDbContext>>();
    }

    private static string BuildTestDatabaseConnectionString(IConfiguration configuration)
    {
        var baseConnectionString =
            Environment.GetEnvironmentVariable("ALASAPP_TEST_SQLSERVER_CONNECTION")
            ?? configuration.GetConnectionString("AlasAppAdmin")
            ?? throw new InvalidOperationException(
                "No se encontro la cadena de conexion administrativa de SQL Server para testing.");

        var builder = new SqlConnectionStringBuilder(baseConnectionString)
        {
            InitialCatalog = TestDatabaseName
        };

        builder.MultipleActiveResultSets = true;

        if (builder.MaxPoolSize < 200)
        {
            builder.MaxPoolSize = 200;
        }

        return builder.ConnectionString;
    }

    private static void EnsureTestDatabaseExists(AlasAppDbContext dbContext)
    {
        var builder = new SqlConnectionStringBuilder(dbContext.Database.GetConnectionString())
        {
            InitialCatalog = "master"
        };

        using var connection = new SqlConnection(builder.ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            $"""
             IF DB_ID(N'{TestDatabaseName}') IS NULL
             BEGIN
                 CREATE DATABASE [{TestDatabaseName}];
             END
             """;
        command.ExecuteNonQuery();
    }

    private static void ResetDatabase(AlasAppDbContext dbContext)
    {
        dbContext.Database.ExecuteSqlRaw(
            """
            DELETE FROM [PasswordResetTokens];
            DELETE FROM [SystemSettings];
            DELETE FROM [UserAccounts];
            DELETE FROM [EventResults];
            DELETE FROM [RankingSnapshotEntries];
            DELETE FROM [RankingSnapshots];
            DELETE FROM [Memberships];
            DELETE FROM [EventCategories];
            DELETE FROM [BeachTokens];
            DELETE FROM [Payments];
            DELETE FROM [Inscriptions];
            DELETE FROM [CategoryTariffs];
            DELETE FROM [CompetitorLicenseCategories];
            DELETE FROM [Competitors];
            DELETE FROM [Events];
            DELETE FROM [Categories];
            DELETE FROM [Circuits];
            """);
    }

    private sealed class NoOpEmailSender : IEmailSender
    {
        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class NoOpIdentityDocumentStorage : IIdentityDocumentStorage
    {
        public Task<string> UploadAsync(
            Guid competitorId,
            AlasApp.Application.IdentityDocuments.IdentityDocumentUpload document,
            CancellationToken cancellationToken)
            => Task.FromResult($"tests/{competitorId:N}/{document.FileName}");
    }
}
