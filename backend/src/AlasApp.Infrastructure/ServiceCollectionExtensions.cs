using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Infrastructure.Authentication;
using AlasApp.Infrastructure.Email;
using AlasApp.Infrastructure.PayPal;
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
        services.Configure<SmtpEmailOptions>(configuration.GetSection(SmtpEmailOptions.SectionName));
        services.Configure<BootstrapAdminOptions>(configuration.GetSection(BootstrapAdminOptions.SectionName));
        services.Configure<PayPalOptions>(configuration.GetSection(PayPalOptions.SectionName));

        services.AddScoped<ICircuitRepository, CircuitRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IEventCategoryRepository, EventCategoryRepository>();
        services.AddScoped<IEventResultRepository, EventResultRepository>();
        services.AddScoped<ICompetitorRepository, CompetitorRepository>();
        services.AddScoped<IInscriptionRepository, InscriptionRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IBeachTokenRepository, BeachTokenRepository>();
        services.AddScoped<IRankingRepository, RankingRepository>();
        services.AddScoped<IUserAccountRepository, UserAccountRepository>();
        services.AddScoped<IAdminDashboardRepository, AdminDashboardRepository>();
        services.AddScoped<IMembershipRepository, MembershipRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IAdminSettingsRepository, AdminSettingsRepository>();
        services.AddScoped<ISurfScoresGateway, SurfScoresGateway>();
        services.AddSingleton<SurfScoresTokenCache>();
        services.AddHttpClient<ISurfScoresImportGateway, SurfScoresImportGateway>();
        services.AddHttpClient<IWordPressService, WordPressService>(client =>
        {
            ConfigureWordPressClient(client, wordPressConfig, ResolveWordPressBaseUrl(wordPressConfig, wordPressConfig.PostsBaseUrl, "posts"));
        });
        services.AddHttpClient<WordPressMediaService>(client =>
        {
            ConfigureWordPressClient(client, wordPressConfig, ResolveWordPressBaseUrl(wordPressConfig, wordPressConfig.MediaBaseUrl, "media"));
        });
        services.AddHttpClient<IGalleryService, GalleryService>(client =>
        {
            ConfigureWordPressClient(client, wordPressConfig, ResolveWordPressBaseUrl(wordPressConfig, wordPressConfig.GalleriesBaseUrl, "gallery"));
        });
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<PayPalTokenCache>();
        var payPalOpts = configuration.GetSection(PayPalOptions.SectionName).Get<PayPalOptions>() ?? new PayPalOptions();
        services.AddHttpClient<IPayPalGateway, PayPalGateway>(client =>
        {
            if (!string.IsNullOrWhiteSpace(payPalOpts.BaseUrl))
            {
                client.BaseAddress = new Uri(payPalOpts.BaseUrl.TrimEnd('/') + "/");
            }
        });
        services.AddSingleton<IResetTokenService, ResetTokenService>();
        services.AddScoped<BootstrapAdminInitializer>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<AlasAppDbContext>());

        return services;
    }

    private static void ConfigureWordPressClient(HttpClient client, WordPressConfig wordPressConfig, string? baseUrl)
    {
        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        }

        if (!string.IsNullOrWhiteSpace(wordPressConfig.Username) &&
            !string.IsNullOrWhiteSpace(wordPressConfig.AppPassword))
        {
            var encodedAuth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{wordPressConfig.Username}:{wordPressConfig.AppPassword}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encodedAuth);
        }

        client.DefaultRequestHeaders.Add("User-Agent", "AlasBFF-DotNet9");
    }

    private static string ResolveWordPressBaseUrl(WordPressConfig config, string? explicitBaseUrl, string resource)
    {
        if (!string.IsNullOrWhiteSpace(explicitBaseUrl))
        {
            return explicitBaseUrl;
        }

        if (!string.IsNullOrWhiteSpace(config.BaseUrl))
        {
            return NormalizeWordPressResourceUrl(config.BaseUrl, resource);
        }

        return string.Empty;
    }

    // BaseUrl remains as backward-compatible fallback when specific resource URLs are not configured.
    private static string NormalizeWordPressResourceUrl(string baseUrl, string resource)
    {
        var uri = new Uri(baseUrl, UriKind.Absolute);
        var path = uri.AbsolutePath.Split('?', StringSplitOptions.RemoveEmptyEntries)[0].TrimEnd('/');
        var apiRoot = path.EndsWith("/posts", StringComparison.OrdinalIgnoreCase)
            ? path[..^"/posts".Length]
            : path;

        var builder = new UriBuilder(uri.Scheme, uri.Host, uri.Port, $"{apiRoot}/{resource}");
        return builder.Uri.ToString().TrimEnd('/');
    }
}
