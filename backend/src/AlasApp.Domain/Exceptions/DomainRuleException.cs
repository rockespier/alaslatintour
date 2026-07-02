namespace AlasApp.Domain.Exceptions;

public sealed class DomainRuleException(string message) : Exception(message);
