namespace AlasApp.Application.Payments.Models;

public sealed record BeachTokenPendingDto(
    Guid RequestId,
    string Status,
    string Message);
