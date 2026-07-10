namespace AlasApp.Application.Dashboard.Models;

public sealed record DashboardDto(
    DashboardKpiDto Kpis,
    IReadOnlyCollection<DashboardActiveEventDto> ActiveEvents,
    IReadOnlyCollection<DashboardRecentInscriptionDto> RecentInscriptions);
