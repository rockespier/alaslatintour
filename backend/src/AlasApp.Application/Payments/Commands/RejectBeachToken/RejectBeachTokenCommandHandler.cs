using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Common;
using AlasApp.Application.Payments.Models;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Application.Payments.Commands.RejectBeachToken;

public sealed class RejectBeachTokenCommandHandler(
    IBeachTokenRepository beachTokenRepository,
    IUnitOfWork unitOfWork,
    IClock clock)
    : IRequestHandler<RejectBeachTokenCommand, BeachTokenAdminDto>
{
    public async Task<BeachTokenAdminDto> Handle(RejectBeachTokenCommand request, CancellationToken cancellationToken)
    {
        var token = await beachTokenRepository.GetEntityByIdAsync(request.TokenId, cancellationToken)
            ?? throw new NotFoundException("Solicitud de token no encontrada.");

        try
        {
            token.Reject(request.Reason);
            token.SetUpdated(clock.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return await beachTokenRepository.GetAdminByIdAsync(token.Id, clock.UtcNow, cancellationToken)
                ?? throw new NotFoundException("Token no encontrado despues de rechazarlo.");
        }
        catch (DomainRuleException exception)
        {
            if (exception.Message.Contains("al menos 10 caracteres", StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException(exception.Message, [new ValidationError("reason", exception.Message)]);
            }

            throw new ConflictException(exception.Message);
        }
    }
}
