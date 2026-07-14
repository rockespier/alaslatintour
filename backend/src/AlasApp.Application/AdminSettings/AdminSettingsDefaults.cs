using AlasApp.Application.AdminSettings.Models;

namespace AlasApp.Application.AdminSettings;

public static class AdminSettingsDefaults
{
    public const string SettingsKey = "admin-settings";
    public const int TokenValidityHours = 24;
    public const int MinimumSurfScoresRefreshMinutes = 5;

    public static AdminSettingsDto Create()
    {
        return new AdminSettingsDto(
            new GeneralSettingsDto(
                "ALAS Latin Tour",
                "ALAS",
                "info@alasglobaltour.com",
                "+57 310 000 0000",
                "www.alaslatintour.com",
                "Colombia",
                new SocialLinksDto(
                    "@alasglobaltour",
                    "facebook.com/alaslatintour",
                    "@alasglobaltour",
                    "youtube.com/@alasglobaltour"),
                new SeasonSettingsDto(
                    2026,
                    "2026-01-01",
                    "2026-12-31")),
            new RankingSettingsDto(
                5,
                0,
                -50,
                [
                    new RankingPointsRowDto("1", 500, 750, 1000, 1500, 2000),
                    new RankingPointsRowDto("2", 380, 580, 760, 1140, 1500),
                    new RankingPointsRowDto("3", 280, 440, 580, 860, 1140),
                    new RankingPointsRowDto("4", 200, 300, 420, 620, 840),
                    new RankingPointsRowDto("5", 150, 220, 300, 440, 600),
                    new RankingPointsRowDto("6-8", 100, 140, 200, 300, 400),
                    new RankingPointsRowDto("9-16", 50, 80, 110, 160, 220),
                    new RankingPointsRowDto("17-32", 10, 20, 30, 40, 60)
                ],
                [
                    new PrizeDistributionSettingsDto("1°", 45, 45, 45, 45, 45),
                    new PrizeDistributionSettingsDto("2°", 25, 25, 25, 25, 25),
                    new PrizeDistributionSettingsDto("3°", 15, 15, 15, 15, 15),
                    new PrizeDistributionSettingsDto("4°", 10, 10, 10, 10, 10),
                    new PrizeDistributionSettingsDto("5°", 5, 5, 5, 5, 5)
                ]),
            new IntegrationSettingsDto(
                new SurfScoresSettingsDto(
                    "https://surfscores.com/api/v1/",
                    string.Empty,
                    string.Empty,
                    true,
                    5),
                new WordPressSettingsDto(
                    "https://alasglobaltour.rtres.net/wp-json/wp/v2/",
                    string.Empty)),
            new NotificationSettingsDto(
                TokenValidityHours,
                "rtres.info@gmail.com",
                ["mgonzalez@alasglobaltour.com"],
                "Tu token de pago para [EVENTO] es: [TOKEN]. Valido por 24 horas. Acercate al kiosco oficial del evento para realizar el pago en efectivo.",
                "noreply@alasglobaltour.com",
                true,
                true,
                true),
            new LiveSettingsDto(
                new YouTubeLiveSettingsDto(false, null, string.Empty, "public", 100, 480),
                new SurfScoresLiveSettingsDto(false, null, string.Empty, 100, 600, MinimumSurfScoresRefreshMinutes, true)));
    }
}
