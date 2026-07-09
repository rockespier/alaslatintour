using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Infrastructure.Authentication;
using AlasApp.Infrastructure.Persistence;
using AlasApp.Infrastructure.Persistence.Repositories;
using AlasApp.Infrastructure.SurfScores;
using AlasApp.Infrastructure.WordPress;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using System.Text;

namespace AlasApp.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AlasApp")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=AlasApp;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";

        services.AddDbContext<AlasAppDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        var wordPressConfig = configuration.GetSection(WordPressConfig.SectionName).Get<WordPressConfig>() ?? new WordPressConfig();
        services.Configure<WordPressConfig>(configuration.GetSection(WordPressConfig.SectionName));

        services.AddScoped<ICircuitRepository, CircuitRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IEventCategoryRepository, EventCategoryRepository>();
        services.AddScoped<ICompetitorRepository, CompetitorRepository>();
        services.AddScoped<IInscriptionRepository, InscriptionRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IBeachTokenRepository, BeachTokenRepository>();
        services.AddScoped<IRankingRepository, RankingRepository>();
        services.AddScoped<IUserAccountRepository, UserAccountRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<ISurfScoresGateway, SurfScoresGateway>();
        services.AddHttpClient<IWordPressService, WordPressService>(client =>
        {
            ConfigureWordPressClient(client, wordPressConfig, "posts");
        });
        services.AddHttpClient<IGalleryService, GalleryService>(client =>
        {
            ConfigureWordPressClient(client, wordPressConfig, "gallery");
        });
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IResetTokenService, ResetTokenService>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<AlasAppDbContext>());

        return services;
    }

    private static void ConfigureWordPressClient(HttpClient client, WordPressConfig wordPressConfig, string resource)
    {
        if (!string.IsNullOrWhiteSpace(wordPressConfig.BaseUrl))
        {
            client.BaseAddress = new Uri(NormalizeWordPressResourceUrl(wordPressConfig.BaseUrl, resource));
        }

        if (!string.IsNullOrWhiteSpace(wordPressConfig.Username) &&
            !string.IsNullOrWhiteSpace(wordPressConfig.AppPassword))
        {
            var encodedAuth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{wordPressConfig.Username}:{wordPressConfig.AppPassword}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encodedAuth);
        }

        client.DefaultRequestHeaders.Add("User-Agent", "AlasBFF-DotNet9");
    }

    // wordPressConfig.BaseUrl points at the "posts" WP REST resource (e.g. .../wp-json/wp/v2/posts);
    // this swaps in whichever sibling resource (posts, gallery, ...) the caller needs.
    private static string NormalizeWordPressResourceUrl(string baseUrl, string resource)
    {
        var uri = new Uri(baseUrl, UriKind.Absolute);
        var path = uri.AbsolutePath.Split('?', StringSplitOptions.RemoveEmptyEntries)[0].TrimEnd('/');
        var apiRoot = path.EndsWith("/posts", StringComparison.OrdinalIgnoreCase)
            ? path[..^"/posts".Length]
            : path;

        var builder = new UriBuilder(uri.Scheme, uri.Host, uri.Port, $"{apiRoot}/{resource}");
        return builder.Uri.ToString().TrimEnd('/') + "/";
    }
}
