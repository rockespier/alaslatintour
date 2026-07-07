using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Common;

namespace AlasApp.Application.Competitors.Commands.DeleteCompetitor;

public sealed class DeleteCompetitorCommandHandler(
    ICompetitorRepository competitorRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteCompetitorCommand, bool>
{
    public async Task<bool> Handle(DeleteCompetitorCommand request, CancellationToken cancellationToken)
    {
        var competitor = await competitorRepository.GetEntityByIdAsync(request.CompetitorId, cancellationToken)
            ?? throw new NotFoundException("Competidor no encontrado.");

        competitorRepository.Remove(competitor);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
