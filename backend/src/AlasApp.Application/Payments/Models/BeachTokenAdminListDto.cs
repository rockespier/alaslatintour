using AlasApp.Application.Common;

namespace AlasApp.Application.Payments.Models;

public sealed record BeachTokenAdminListDto(
    IReadOnlyCollection<BeachTokenAdminDto> PendingRequests,
    IReadOnlyCollection<BeachTokenAdminDto> History,
    BeachTokenDailyStatsDto DailyStats,
    PaginationMetaDto Pagination);
