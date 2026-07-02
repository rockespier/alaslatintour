using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Infrastructure.Persistence;
using AlasApp.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AlasApp.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AlasApp") ?? "Data Source=alasapp.db";

        services.AddDbContext<AlasAppDbContext>(options =>
        {
            options.UseSqlite(connectionString);
        });

        services.AddScoped<ICircuitRepository, CircuitRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IEventCategoryRepository, EventCategoryRepository>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<AlasAppDbContext>());

        return services;
    }
}
