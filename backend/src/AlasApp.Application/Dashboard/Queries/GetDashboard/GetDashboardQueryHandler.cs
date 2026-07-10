using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Dashboard.Models;

namespace AlasApp.Application.Dashboard.Queries.GetDashboard;

public sealed class GetDashboardQueryHandler(IAdminDashboardRepository dashboardRepository)
    : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    public Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        return dashboardRepository.GetDashboardAsync(cancellationToken);
    }
}
