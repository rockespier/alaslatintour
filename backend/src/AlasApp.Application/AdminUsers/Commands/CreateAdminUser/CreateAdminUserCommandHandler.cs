using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.AdminUsers.Models;
using AlasApp.Application.Common;
using AlasApp.Domain.Entities;
using AlasApp.Domain.Enums;

namespace AlasApp.Application.AdminUsers.Commands.CreateAdminUser;

public sealed class CreateAdminUserCommandHandler(
    IUserAccountRepository userAccountRepository,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateAdminUserCommand, AdminUserDto>
{
    public async Task<AdminUserDto> Handle(CreateAdminUserCommand request, CancellationToken cancellationToken)
    {
        Validate(request);

        if (await userAccountRepository.EmailExistsAsync(request.Email, cancellationToken))
        {
            throw new ConflictException("El correo ya está en uso.");
        }

        var tempPassword = $"Tmp-{Guid.NewGuid():N}1!";
        var userAccount = UserAccount.Create(
            request.Email,
            passwordHasher.Hash(tempPassword),
            request.Nombre,
            request.Apellido,
            UserType.Espectador,
            string.Empty,
            PreferredLanguage.Espanol,
            false,
            true,
            false,
            null,
            request.Rol);

        await userAccountRepository.AddAsync(userAccount, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var created = await userAccountRepository.GetAdminUserByIdAsync(userAccount.Id, cancellationToken)
            ?? throw new InvalidOperationException("No se pudo recuperar el usuario administrativo creado.");

        return created;
    }

    private static void Validate(CreateAdminUserCommand request)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            errors.Add(new ValidationError("nombre", "El nombre es obligatorio."));
        }

        if (string.IsNullOrWhiteSpace(request.Apellido))
        {
            errors.Add(new ValidationError("apellido", "El apellido es obligatorio."));
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors.Add(new ValidationError("email", "El email es obligatorio."));
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("La solicitud contiene errores de validación.", errors);
        }
    }
}
