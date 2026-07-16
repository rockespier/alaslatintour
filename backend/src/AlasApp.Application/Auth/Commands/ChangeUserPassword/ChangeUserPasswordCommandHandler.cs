using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Common;

namespace AlasApp.Application.Auth.Commands.ChangeUserPassword;

public sealed class ChangeUserPasswordCommandHandler(
    IUserAccountRepository userAccountRepository,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    IClock clock)
    : IRequestHandler<ChangeUserPasswordCommand, bool>
{
    public async Task<bool> Handle(ChangeUserPasswordCommand request, CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();

        if (request.UserId == Guid.Empty)
        {
            errors.Add(new ValidationError("userId", "El identificador del usuario es inválido."));
        }

        if (!PasswordPolicy.IsValid(request.NewPassword))
        {
            errors.Add(new ValidationError("newPassword", PasswordPolicy.Message));
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("La solicitud contiene errores de validacion.", errors);
        }

        var user = await userAccountRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("Usuario no encontrado.");

        user.ChangePassword(passwordHasher.Hash(request.NewPassword), clock.UtcNow);
        user.SetUpdated(clock.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
