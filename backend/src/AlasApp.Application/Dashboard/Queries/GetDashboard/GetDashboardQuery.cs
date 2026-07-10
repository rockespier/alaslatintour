using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Dashboard.Models;

namespace AlasApp.Application.Dashboard.Queries.GetDashboard;

public sealed record GetDashboardQuery() : IRequest<DashboardDto>;
