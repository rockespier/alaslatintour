using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Common;
using AlasApp.Application.Rankings.Models;
using AlasApp.Domain.Entities;

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

        var snapshots = externalSnapshots
            .Select(snapshot => ToDomainSnapshot(request.CircuitId, snapshot, syncedAt))
            .ToList();

        await rankingRepository.ReplaceCircuitSnapshotsAsync(request.CircuitId, snapshots, cancellationToken);

        circuit.MarkSynced(syncedAt);
        circuit.SetUpdated(syncedAt);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new SurfScoresSyncResultDto(
            string.IsNullOrWhiteSpace(circuit.SurfScoresCode) ? circuit.Id.ToString() : circuit.SurfScoresCode,
            snapshots.Sum(x => x.Entries.Count),
            syncedAt);
    }

    private static RankingSnapshot ToDomainSnapshot(Guid circuitId, SurfScoresRankingSnapshotDto dto, DateTimeOffset syncedAt)
    {
        var snapshot = RankingSnapshot.Create(circuitId, dto.CategoryId, dto.CategoryName, dto.Year, syncedAt);
        snapshot.SetCreated(syncedAt);

        foreach (var entry in dto.Entries)
        {
            snapshot.AddEntry(entry.Name, entry.Country, entry.Pos, entry.Points, entry.Events, entry.Variation);
        }

        return snapshot;
    }
}
