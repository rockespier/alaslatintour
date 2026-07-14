using AlasApp.Application.AdminSettings.Models;
using System.Text.Json;

namespace AlasApp.Application.AdminSettings;

public static class AdminSettingsSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static AdminSettingsDto DeserializeOrDefault(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return AdminSettingsDefaults.Create();
        }

        var settings = JsonSerializer.Deserialize<AdminSettingsDto>(json, JsonOptions)
            ?? AdminSettingsDefaults.Create();

        return Normalize(settings);
    }

    public static string Serialize(AdminSettingsDto settings)
    {
        return JsonSerializer.Serialize(settings, JsonOptions);
    }

    public static string Serialize(IntegrationTestResultDto result)
    {
        return JsonSerializer.Serialize(result, JsonOptions);
    }

    public static AdminSettingsDto Normalize(AdminSettingsDto settings)
    {
        var defaults = AdminSettingsDefaults.Create();

        settings = settings with
        {
            General = settings.General ?? defaults.General,
            Ranking = settings.Ranking ?? defaults.Ranking,
            Integrations = settings.Integrations ?? defaults.Integrations,
            Notifications = settings.Notifications ?? defaults.Notifications,
            Live = settings.Live ?? defaults.Live
        };

        if (settings.Ranking.PointsMatrix is null)
        {
            settings = settings with
            {
                Ranking = settings.Ranking with { PointsMatrix = defaults.Ranking.PointsMatrix }
            };
        }

        if (settings.Ranking.PrizeDistribution is null || settings.Ranking.PrizeDistribution.Count == 0)
        {
            settings = settings with
            {
                Ranking = settings.Ranking with { PrizeDistribution = defaults.Ranking.PrizeDistribution }
            };
        }

        if (settings.Notifications.AdditionalAdminEmails is null)
        {
            settings = settings with
            {
                Notifications = settings.Notifications with { AdditionalAdminEmails = [] }
            };
        }

        return settings;
    }
}
