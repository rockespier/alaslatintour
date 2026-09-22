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
                "ALAS Global Tour",
                "ALAS",
                "info@alasglobaltour.com",
                "+57 310 000 0000",
                "www.alaslatintour.com",
                "Colombia",
                0m,
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
                BuildDefaultPointsMatrix(),
                [
                    new PrizeDistributionSettingsDto("1°", 45, 45, 45, 45, 45, 45, 45),
                    new PrizeDistributionSettingsDto("2°", 25, 25, 25, 25, 25, 25, 25),
                    new PrizeDistributionSettingsDto("3°", 15, 15, 15, 15, 15, 15, 15),
                    new PrizeDistributionSettingsDto("4°", 10, 10, 10, 10, 10, 10, 10),
                    new PrizeDistributionSettingsDto("5°", 5, 5, 5, 5, 5, 5, 5)
                ], false, false),
            new IntegrationSettingsDto(
                new SurfScoresSettingsDto(
                    "https://surfscores.com/api/v1/",
                    string.Empty,
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
                new SurfScoresLiveSettingsDto(false, null, string.Empty, 100, 600, MinimumSurfScoresRefreshMinutes, true),
                string.Empty));
    }

    /// <summary>
    /// Puntos por puesto (1 a 90, sin rangos) según "TABLA DE PUNTOS TODAS LAS CATEGORÍAS" del
    /// reglamento ALAS 2026 (documentacion/reglamentoAlas2026.md), columnas de 1 a 6 estrellas.
    /// La 7ma columna ("Prime") no está en esa tabla: se calcula con el bono del 10% que el
    /// reglamento define para eventos PRIME ("Ej. 6 Estrellas puntos máximos = 6600" = 6000 x 1.10).
    /// </summary>
    private static List<RankingPointsRowDto> BuildDefaultPointsMatrix()
    {
        return new (int Puesto, int Star1, int Star2, int Star3, int Star4, int Star5, int Star6)[]
        {
            (1, 1000, 2000, 3000, 4000, 5000, 6000),
            (2, 860, 1720, 2580, 3440, 4300, 5160),
            (3, 730, 1460, 2190, 2920, 3650, 4380),
            (4, 670, 1340, 2010, 2680, 3350, 4020),
            (5, 610, 1220, 1830, 2440, 3050, 3660),
            (6, 583, 1166, 1749, 2332, 2915, 3498),
            (7, 555, 1110, 1665, 2220, 2775, 3330),
            (8, 528, 1056, 1584, 2112, 2640, 3168),
            (9, 500, 1000, 1500, 2000, 2500, 3000),
            (10, 488, 976, 1464, 1952, 2440, 2928),
            (11, 475, 950, 1425, 1900, 2375, 2850),
            (12, 462, 924, 1386, 1848, 2310, 2772),
            (13, 450, 900, 1350, 1800, 2250, 2700),
            (14, 438, 876, 1314, 1752, 2190, 2628),
            (15, 425, 850, 1275, 1700, 2125, 2550),
            (16, 413, 826, 1239, 1652, 2065, 2478),
            (17, 400, 800, 1200, 1600, 2000, 2400),
            (18, 395, 790, 1185, 1580, 1975, 2370),
            (19, 390, 780, 1170, 1560, 1950, 2340),
            (20, 385, 770, 1155, 1540, 1925, 2310),
            (21, 380, 760, 1140, 1520, 1900, 2280),
            (22, 375, 750, 1125, 1500, 1875, 2250),
            (23, 370, 740, 1110, 1480, 1850, 2220),
            (24, 365, 730, 1095, 1460, 1825, 2190),
            (25, 360, 720, 1080, 1440, 1800, 2160),
            (26, 355, 710, 1065, 1420, 1775, 2130),
            (27, 350, 700, 1050, 1400, 1750, 2100),
            (28, 345, 690, 1035, 1380, 1725, 2070),
            (29, 340, 680, 1020, 1360, 1700, 2040),
            (30, 335, 670, 1005, 1340, 1675, 2010),
            (31, 330, 660, 990, 1320, 1650, 1980),
            (32, 325, 650, 975, 1300, 1625, 1950),
            (33, 320, 640, 960, 1280, 1600, 1920),
            (34, 315, 630, 945, 1260, 1575, 1890),
            (35, 310, 620, 930, 1240, 1550, 1860),
            (36, 305, 610, 915, 1220, 1525, 1830),
            (37, 300, 600, 900, 1200, 1500, 1800),
            (38, 295, 590, 885, 1180, 1475, 1770),
            (39, 290, 580, 870, 1160, 1450, 1740),
            (40, 285, 570, 855, 1140, 1425, 1710),
            (41, 280, 560, 840, 1120, 1400, 1680),
            (42, 275, 550, 825, 1100, 1375, 1650),
            (43, 270, 540, 810, 1080, 1350, 1620),
            (44, 265, 530, 795, 1060, 1325, 1590),
            (45, 260, 520, 780, 1040, 1300, 1560),
            (46, 255, 510, 765, 1020, 1275, 1530),
            (47, 250, 500, 750, 1000, 1250, 1500),
            (48, 245, 490, 735, 980, 1225, 1470),
            (49, 240, 480, 720, 960, 1200, 1440),
            (50, 235, 470, 705, 940, 1175, 1410),
            (51, 230, 460, 690, 920, 1150, 1380),
            (52, 225, 450, 675, 900, 1125, 1350),
            (53, 220, 440, 660, 880, 1100, 1320),
            (54, 215, 430, 645, 860, 1075, 1290),
            (55, 210, 420, 630, 840, 1050, 1260),
            (56, 205, 410, 615, 820, 1025, 1230),
            (57, 200, 400, 600, 800, 1000, 1200),
            (58, 195, 390, 585, 780, 975, 1170),
            (59, 190, 380, 570, 760, 950, 1140),
            (60, 185, 370, 555, 740, 925, 1110),
            (61, 180, 360, 540, 720, 900, 1080),
            (62, 175, 350, 525, 700, 875, 1050),
            (63, 170, 340, 510, 680, 850, 1020),
            (64, 165, 330, 495, 660, 825, 990),
            (65, 160, 320, 480, 640, 800, 960),
            (66, 158, 316, 474, 632, 790, 948),
            (67, 156, 312, 468, 624, 780, 936),
            (68, 154, 308, 462, 616, 770, 924),
            (69, 152, 304, 456, 608, 760, 912),
            (70, 150, 300, 450, 600, 750, 900),
            (71, 148, 296, 444, 592, 740, 888),
            (72, 146, 292, 438, 584, 730, 876),
            (73, 144, 288, 432, 576, 720, 864),
            (74, 142, 284, 426, 568, 710, 852),
            (75, 140, 280, 420, 560, 700, 840),
            (76, 138, 276, 414, 552, 690, 828),
            (77, 136, 272, 408, 544, 680, 816),
            (78, 134, 268, 402, 536, 670, 804),
            (79, 132, 264, 396, 528, 660, 792),
            (80, 130, 260, 390, 520, 650, 780),
            (81, 128, 256, 384, 512, 640, 768),
            (82, 126, 252, 378, 504, 630, 756),
            (83, 124, 248, 372, 496, 620, 744),
            (84, 122, 244, 366, 488, 610, 732),
            (85, 120, 240, 360, 480, 600, 720),
            (86, 118, 236, 354, 472, 590, 708),
            (87, 116, 232, 348, 464, 580, 696),
            (88, 114, 228, 342, 456, 570, 684),
            (89, 112, 224, 336, 448, 560, 672),
            (90, 110, 220, 330, 440, 550, 660),
        }
        .Select(row => new RankingPointsRowDto(
            row.Puesto.ToString(),
            row.Star1,
            row.Star2,
            row.Star3,
            row.Star4,
            row.Star5,
            row.Star6,
            (int)Math.Round(row.Star6 * 1.10m, MidpointRounding.AwayFromZero)))
        .ToList();
    }
}
