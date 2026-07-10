using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.AdminUsers.Models;
using AlasApp.Application.Common;
using AlasApp.Domain.Enums;

namespace AlasApp.Application.AdminUsers.Commands.UpdateAdminUser;

public sealed class UpdateAdminUserCommandHandler(
    IUserAccountRepository userAccountRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateAdminUserCommand, AdminUserDto>
{
    public async Task<AdminUserDto> Handle(UpdateAdminUserCommand request, CancellationToken cancellationToken)
    {
        var userAccount = await userAccountRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (userAccount is null || userAccount.AdminRole is null)
        {
            throw new NotFoundException("No se encontró el usuario administrativo solicitado.");
        }

        if (request.Rol.HasValue)
        {
            userAccount.AssignAdminRole(request.Rol.Value);
        }

        if (request.Status.HasValue)
        {
            if (request.Status.Value == AdminUserStatus.Activo)
            {
                userAccount.Activate();
            }
            else
            {
                userAccount.Deactivate();
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await userAccountRepository.GetAdminUserByIdAsync(userAccount.Id, cancellationToken)
            ?? throw new InvalidOperationException("No se pudo recuperar el usuario administrativo actualizado.");
    }
}
