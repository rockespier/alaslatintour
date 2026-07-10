using AlasApp.Application.Dashboard.Models;

namespace AlasApp.Application.Abstractions.Persistence;

public interface IAdminDashboardRepository
{
    Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken);
}
