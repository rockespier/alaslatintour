using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Common;
using AlasApp.Application.Memberships.Models;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Application.Memberships.Commands.UpdateMembership;

public sealed class UpdateMembershipCommandHandler(
    IMembershipRepository membershipRepository,
    IUnitOfWork unitOfWork,
    IClock clock)
    : IRequestHandler<UpdateMembershipCommand, MembershipDto>
{
    public async Task<MembershipDto> Handle(UpdateMembershipCommand request, CancellationToken cancellationToken)
    {
        Validate(request);

        var membership = await membershipRepository.GetEntityByIdAsync(request.MembershipId, cancellationToken)
            ?? throw new NotFoundException("Membresia no encontrada.");

        try
        {
            membership.Update(
                request.ClubFederacion,
                request.Pais,
                request.Plan,
                request.InicioVigencia,
                request.Vencimiento,
                request.EmailContacto);

            membership.SetUpdated(clock.UtcNow);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return await membershipRepository.GetByIdAsync(membership.Id, cancellationToken)
                ?? throw new NotFoundException("Membresia no encontrada despues de actualizarla.");
        }
        catch (DomainRuleException exception)
        {
            throw new ValidationException(exception.Message, [new ValidationError("body", exception.Message)]);
        }
    }

    private static void Validate(UpdateMembershipCommand request)
    {
        var errors = new List<ValidationError>();

        if (request.MembershipId == Guid.Empty)
        {
            errors.Add(new ValidationError("membershipId", "El identificador de la membresia es invalido."));
        }

        if (string.IsNullOrWhiteSpace(request.ClubFederacion))
        {
            errors.Add(new ValidationError("clubFederacion", "El club o federacion es obligatorio."));
        }

        if (string.IsNullOrWhiteSpace(request.Pais))
        {
            errors.Add(new ValidationError("pais", "El pais es obligatorio."));
        }

        if (string.IsNullOrWhiteSpace(request.EmailContacto))
        {
            errors.Add(new ValidationError("emailContacto", "El email de contacto es obligatorio."));
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("La solicitud contiene errores de validacion.", errors);
        }
    }
}
