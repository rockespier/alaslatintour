using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Infrastructure.Authentication;
using AlasApp.Infrastructure.Persistence;
using AlasApp.Infrastructure.Persistence.Repositories;
using AlasApp.Infrastructure.SurfScores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IResetTokenService, ResetTokenService>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<AlasAppDbContext>());

        return services;
    }
}
