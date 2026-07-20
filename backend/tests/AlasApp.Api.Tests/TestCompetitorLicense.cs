using AlasApp.Domain.Enums;
using AlasApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AlasApp.Api.Tests;

internal static class TestCompetitorLicense
{
    public static async Task ActivateAsync(IServiceProvider services, string competitorId)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AlasAppDbContext>();
        var competitor = await dbContext.Competitors.SingleAsync(x => x.Id == Guid.Parse(competitorId));

        competitor.UpdateLicense(
            LicenseStatus.Activa,
            $"TEST-{competitor.Id:N}"[..20],
            $"TEST-{competitor.Id:N}-2027",
            new DateTimeOffset(2027, 12, 31, 0, 0, 0, TimeSpan.Zero),
            []);

        await dbContext.SaveChangesAsync();
    }
}
