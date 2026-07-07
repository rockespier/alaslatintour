namespace AlasApp.Application.Payments.Models;

public sealed record BeachTokenDailyStatsDto(
    int PendingCount,
    int ApprovedToday,
    int RejectedToday);
