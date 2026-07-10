using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.AdminUsers.Models;
using AlasApp.Application.Common;

namespace AlasApp.Application.AdminUsers.Queries.GetAdminUserById;

public sealed class GetAdminUserByIdQueryHandler(IUserAccountRepository userAccountRepository)
    : IRequestHandler<GetAdminUserByIdQuery, AdminUserDto>
{
    public async Task<AdminUserDto> Handle(GetAdminUserByIdQuery request, CancellationToken cancellationToken)
    {
        return await userAccountRepository.GetAdminUserByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("No se encontró el usuario administrativo solicitado.");
    }
}
