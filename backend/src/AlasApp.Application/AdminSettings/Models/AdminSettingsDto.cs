namespace AlasApp.Application.AdminSettings.Models;

public sealed record AdminSettingsDto(
    GeneralSettingsDto General,
    RankingSettingsDto Ranking,
    IntegrationSettingsDto Integrations,
    NotificationSettingsDto Notifications,
    LiveSettingsDto Live);

public sealed record GeneralSettingsDto(
    string OrganizationName,
    string ShortName,
    string ContactEmail,
    string Phone,
    string Website,
    string HeadquartersCountry,
    SocialLinksDto SocialLinks,
    SeasonSettingsDto Season);

public sealed record SocialLinksDto(
    string Instagram,
    string Facebook,
    string X,
    string YouTube);

public sealed record SeasonSettingsDto(
    int CurrentYear,
    string StartDate,
    string EndDate);

public sealed record RankingSettingsDto(
    int BestResultsCount,
    decimal DnsScorePercentage,
    int DsqPenaltyPoints,
    List<RankingPointsRowDto> PointsMatrix,
    List<PrizeDistributionSettingsDto> PrizeDistribution);

public sealed record RankingPointsRowDto(
    string Position,
    int Star1,
    int Star2,
    int Star3,
    int Star4,
    int Star5);

public sealed record PrizeDistributionSettingsDto(
    string PlaceLabel,
    decimal Star1Percent,
    decimal Star2Percent,
    decimal Star3Percent,
    decimal Star4Percent,
    decimal Star5Percent);

public sealed record IntegrationSettingsDto(
    SurfScoresSettingsDto SurfScores,
    WordPressSettingsDto WordPress);

public sealed record SurfScoresSettingsDto(
    string Endpoint,
    string Username,
    string OrganizacionId,
    bool TermsAccepted,
    int CacheMinutes);

public sealed record WordPressSettingsDto(
    string Endpoint,
    string Username);

public sealed record NotificationSettingsDto(
    int TokenValidityHours,
    string AdminEmail,
    List<string> AdditionalAdminEmails,
    string CompetitorTokenEmailTemplate,
    string SenderEmail,
    bool NotifyNewInscriptions,
    bool NotifyConfirmedPayments,
    bool NotifyExpiredTokens);

public sealed record LiveSettingsDto(
    YouTubeLiveSettingsDto YouTube,
    SurfScoresLiveSettingsDto SurfScores);

public sealed record YouTubeLiveSettingsDto(
    bool Active,
    Guid? EventId,
    string VideoIdOrUrl,
    string Privacy,
    int Width,
    int Height);

public sealed record SurfScoresLiveSettingsDto(
    bool Active,
    Guid? EventId,
    string EmbedUrl,
    int Width,
    int Height,
    int RefreshMinutes,
    bool LocalDisplaysOnly);

public sealed record IntegrationTestResultDto(
    string Provider,
    string Status,
    string Message,
    DateTimeOffset CheckedAtUtc);
