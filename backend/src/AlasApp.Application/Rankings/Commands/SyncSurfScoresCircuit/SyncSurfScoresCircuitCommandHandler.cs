using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Common;
using AlasApp.Application.Rankings.Models;

namespace AlasApp.Application.Rankings.Commands.SyncSurfScoresCircuit;

public sealed class SyncSurfScoresCircuitCommandHandler(
    ICircuitRepository circuitRepository,
    IRankingRepository rankingRepository,
    ISurfScoresGateway surfScoresGateway,
    IUnitOfWork unitOfWork,
    IClock clock)
    : IRequestHandler<SyncSurfScoresCircuitCommand, SurfScoresSyncResultDto>
{
    public async Task<SurfScoresSyncResultDto> Handle(SyncSurfScoresCircuitCommand request, CancellationToken cancellationToken)
    {
        var circuit = await circuitRepository.GetEntityByIdAsync(request.CircuitId, cancellationToken)
            ?? throw new NotFoundException("Circuito no encontrado.");

        var syncedAt = clock.UtcNow;
        var externalSnapshots = await surfScoresGateway.BuildCircuitRankingCacheAsync(request.CircuitId, cancellationToken);

        var snapshots = RankingSnapshotFactory.Build(request.CircuitId, externalSnapshots, syncedAt);

        await rankingRepository.ReplaceCircuitSnapshotsAsync(request.CircuitId, snapshots, cancellationToken);

        circuit.MarkSynced(syncedAt);
        circuit.SetUpdated(syncedAt);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new SurfScoresSyncResultDto(
            string.IsNullOrWhiteSpace(circuit.SurfScoresCode) ? circuit.Id.ToString() : circuit.SurfScoresCode,
            snapshots.Sum(x => x.Entries.Count),
            syncedAt);
    }
}
