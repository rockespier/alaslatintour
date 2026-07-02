using AlasApp.Application.Abstractions.Messaging;

namespace AlasApp.Application.Circuits.Commands.DeleteCircuit;

public sealed record DeleteCircuitCommand(Guid CircuitId) : IRequest<bool>;
