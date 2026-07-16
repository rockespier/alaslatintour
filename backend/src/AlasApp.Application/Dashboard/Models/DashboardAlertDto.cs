namespace AlasApp.Application.Dashboard.Models;

public sealed record DashboardAlertDto(
    string Module,
    string Level,
    string Title,
    string Message,
    int Count);
