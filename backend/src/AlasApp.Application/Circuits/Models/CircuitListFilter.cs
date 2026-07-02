using AlasApp.Domain.Enums;

namespace AlasApp.Application.Circuits.Models;

public sealed record CircuitListFilter(
    int Page,
    int Limit,
    CircuitStatus? Status,
    int? Year,
    CircuitModalidad? Modalidad);
