using AlasApp.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AlasApp.Api.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string TestDatabaseName = "AlasAppTests";
    private const string DefaultAdminConnectionString = "Server=.\\SQLEXPRESS;Initial Catalog=master;Integrated Security=True;TrustServerCertificate=True;Encrypt=True;MultipleActiveResultSets=True;Max Pool Size=200;";

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
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Persistence:Provider"] = "SqlServer",
                ["ConnectionStrings:AlasAppAdmin"] = DefaultAdminConnectionString
            });
        });
        builder.ConfigureServices((_, services) =>
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:AlasAppAdmin"] = DefaultAdminConnectionString
                })
                .Build();

            var connectionString = BuildTestDatabaseConnectionString(configuration);

            services.RemoveAll<DbContextOptions<AlasAppDbContext>>();
            services.AddDbContext<AlasAppDbContext>(options => options.UseSqlServer(connectionString));
            ConfigureTestServices(services);
        });
    }

    protected virtual void ConfigureTestServices(IServiceCollection services)
    {
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
            DELETE FROM [UserAccounts];
            DELETE FROM [RankingSnapshotEntries];
            DELETE FROM [RankingSnapshots];
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
}
