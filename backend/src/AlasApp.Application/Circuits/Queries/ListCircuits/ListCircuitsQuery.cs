using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Circuits.Models;
using AlasApp.Application.Common;

namespace AlasApp.Application.Circuits.Queries.ListCircuits;

public sealed record ListCircuitsQuery(CircuitListFilter Filter) : IRequest<PagedResult<CircuitDto>>;
