namespace AlasApp.Application.Common;

public sealed record ValidationError(string Field, string Message);
