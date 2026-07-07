using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Rankings.Models;

namespace AlasApp.Application.Rankings.Commands.SyncSurfScoresCircuit;

public sealed record SyncSurfScoresCircuitCommand(Guid CircuitId) : IRequest<SurfScoresSyncResultDto>;
