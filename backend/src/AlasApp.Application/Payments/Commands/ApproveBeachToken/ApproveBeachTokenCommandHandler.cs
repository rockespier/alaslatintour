using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Common;
using AlasApp.Application.Payments.Models;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Application.Payments.Commands.ApproveBeachToken;

public sealed class ApproveBeachTokenCommandHandler(
    IBeachTokenRepository beachTokenRepository,
    IUnitOfWork unitOfWork,
    IClock clock)
    : IRequestHandler<ApproveBeachTokenCommand, BeachTokenAdminDto>
{
    public async Task<BeachTokenAdminDto> Handle(ApproveBeachTokenCommand request, CancellationToken cancellationToken)
    {
        var token = await beachTokenRepository.GetEntityByIdAsync(request.TokenId, cancellationToken)
            ?? throw new NotFoundException("Solicitud de token no encontrada.");

        try
        {
            if (token.ExpirationAt.HasValue && token.ExpirationAt.Value <= clock.UtcNow)
            {
                token.MarkExpired();
            }

            token.Approve(GenerateTokenCode(), clock.UtcNow, clock.UtcNow.AddHours(24));
            token.SetUpdated(clock.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return await beachTokenRepository.GetAdminByIdAsync(token.Id, clock.UtcNow, cancellationToken)
                ?? throw new NotFoundException("Token no encontrado despues de aprobarlo.");
        }
        catch (DomainRuleException exception)
        {
            throw new ConflictException(exception.Message);
        }
    }

    private static string GenerateTokenCode()
    {
        var raw = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        return $"{raw[..4]}-{raw[4..]}";
    }
}
