using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Common;

namespace AlasApp.Application.AdminUsers.Commands.DeleteAdminUser;

public sealed class DeleteAdminUserCommandHandler(
    IUserAccountRepository userAccountRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteAdminUserCommand, bool>
{
    public async Task<bool> Handle(DeleteAdminUserCommand request, CancellationToken cancellationToken)
    {
        if (request.CurrentUserId.HasValue && request.CurrentUserId.Value == request.UserId)
        {
            throw new ConflictException("No se puede eliminar el propio usuario autenticado.");
        }

        var userAccount = await userAccountRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (userAccount is null || userAccount.AdminRole is null)
        {
            throw new NotFoundException("No se encontró el usuario administrativo solicitado.");
        }

        userAccountRepository.Remove(userAccount);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
