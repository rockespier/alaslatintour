using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Common;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Application.Memberships.Commands.DeleteMembership;

public sealed class DeleteMembershipCommandHandler(
    IMembershipRepository membershipRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteMembershipCommand, bool>
{
    public async Task<bool> Handle(DeleteMembershipCommand request, CancellationToken cancellationToken)
    {
        if (request.MembershipId == Guid.Empty)
        {
            throw new ValidationException(
                "La solicitud contiene errores de validacion.",
                [new ValidationError("membershipId", "El identificador de la membresia es invalido.")]);
        }

        var membership = await membershipRepository.GetEntityByIdAsync(request.MembershipId, cancellationToken)
            ?? throw new NotFoundException("Membresia no encontrada.");

        membershipRepository.Remove(membership);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
