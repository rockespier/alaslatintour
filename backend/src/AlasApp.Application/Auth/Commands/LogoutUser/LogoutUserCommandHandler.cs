using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Common;

namespace AlasApp.Application.Auth.Commands.LogoutUser;

public sealed class LogoutUserCommandHandler(
    IUserAccountRepository userAccountRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<LogoutUserCommand, bool>
{
    public async Task<bool> Handle(LogoutUserCommand request, CancellationToken cancellationToken)
    {
        var userAccount = await userAccountRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("No se encontró el usuario autenticado.");

        userAccount.InvalidateActiveTokens();
        userAccount.SetUpdated(DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
