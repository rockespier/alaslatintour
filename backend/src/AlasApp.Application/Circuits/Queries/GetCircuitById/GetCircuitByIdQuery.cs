using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Circuits.Models;

namespace AlasApp.Application.Circuits.Queries.GetCircuitById;

public sealed record GetCircuitByIdQuery(Guid CircuitId) : IRequest<CircuitDto>;
