namespace AlasApp.Application.Common;

public sealed class ValidationException(string message, IReadOnlyCollection<ValidationError> errors) : Exception(message)
{
    public IReadOnlyCollection<ValidationError> Errors { get; } = errors;
}
