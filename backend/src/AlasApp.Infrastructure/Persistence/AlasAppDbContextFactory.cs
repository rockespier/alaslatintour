using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AlasApp.Infrastructure.Persistence;

public sealed class AlasAppDbContextFactory : IDesignTimeDbContextFactory<AlasAppDbContext>
{
    public AlasAppDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();
        var connectionString = configuration.GetConnectionString("AlasApp");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "No se encontró la cadena de conexión 'ConnectionStrings:AlasApp' para ejecutar migraciones de EF.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<AlasAppDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new AlasAppDbContext(optionsBuilder.Options);
    }

    private static IConfiguration BuildConfiguration()
    {
        var basePath = ResolveConfigurationBasePath();

        return new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
    }

    private static string ResolveConfigurationBasePath()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var apiDirectory = Path.GetFullPath(Path.Combine(currentDirectory, "..", "AlasApp.Api"));

        if (File.Exists(Path.Combine(apiDirectory, "appsettings.json")))
        {
            return apiDirectory;
        }

        if (File.Exists(Path.Combine(currentDirectory, "appsettings.json")))
        {
            return currentDirectory;
        }

        throw new DirectoryNotFoundException(
            $"No se pudo localizar appsettings.json para el diseño de EF. Directorio actual: '{currentDirectory}'.");
    }
}
