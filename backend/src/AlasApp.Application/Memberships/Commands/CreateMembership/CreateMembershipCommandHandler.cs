using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Common;
using AlasApp.Application.Memberships.Models;
using AlasApp.Domain.Entities;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Application.Memberships.Commands.CreateMembership;

public sealed class CreateMembershipCommandHandler(
    IMembershipRepository membershipRepository,
    IUnitOfWork unitOfWork,
    IClock clock)
    : IRequestHandler<CreateMembershipCommand, MembershipDto>
{
    public async Task<MembershipDto> Handle(CreateMembershipCommand request, CancellationToken cancellationToken)
    {
        Validate(request);

        try
        {
            var membership = Membership.Create(
                request.ClubFederacion,
                request.Pais,
                request.Plan,
                request.InicioVigencia,
                request.Vencimiento,
                request.EmailContacto);

            membership.SetCreated(clock.UtcNow);

            await membershipRepository.AddAsync(membership, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return await membershipRepository.GetByIdAsync(membership.Id, cancellationToken)
                ?? throw new NotFoundException("Membresia no encontrada despues de crearla.");
        }
        catch (DomainRuleException exception)
        {
            throw new ValidationException(exception.Message, [new ValidationError("body", exception.Message)]);
        }
    }

    private static void Validate(CreateMembershipCommand request)
    {
        var errors = new List<ValidationError>();

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
