using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.AdminUsers.Models;
using AlasApp.Application.Common;
using AlasApp.Application.Emails;
using AlasApp.Domain.Entities;
using AlasApp.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace AlasApp.Application.AdminUsers.Commands.CreateAdminUser;

public sealed class CreateAdminUserCommandHandler(
    IUserAccountRepository userAccountRepository,
    IPasswordHasher passwordHasher,
    IEmailSender emailSender,
    IUnitOfWork unitOfWork,
    ILogger<CreateAdminUserCommandHandler> logger)
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

        if (request.SendInvitationEmail)
        {
            await SendInvitationEmailAsync(request, tempPassword, cancellationToken);
        }

        return created;
    }

    private async Task SendInvitationEmailAsync(CreateAdminUserCommand request, string tempPassword, CancellationToken cancellationToken)
    {
        try
        {
            var rolLabel = request.Rol switch
            {
                AdminRole.SuperAdmin => "Super Administrador",
                AdminRole.Admin      => "Administrador",
                AdminRole.Arbitro    => "Árbitro",
                AdminRole.Revisor    => "Revisor",
                _                   => request.Rol.ToString()
            };

            var html = TransactionalEmailTemplate.Render(
                "Invitación",
                $"Bienvenido al panel de ALAS Global Tour, {request.Nombre}",
                $"Hola {request.Nombre}, se ha creado una cuenta de administrador para ti en la plataforma ALAS Global Tour. Usa las credenciales a continuación para acceder.",
                "Contraseña temporal",
                tempPassword,
                [
                    new EmailDetail("Correo de acceso", request.Email),
                    new EmailDetail("Rol asignado", rolLabel),
                ],
                "Por seguridad, cambia tu contraseña inmediatamente después del primer inicio de sesión. Esta contraseña es de un solo uso.",
                "Este mensaje fue enviado automáticamente por ALAS Global Tour.");

            await emailSender.SendAsync(
                new EmailMessage(
                    request.Email,
                    "Invitación al panel administrativo — ALAS Latin Tour",
                    $"Hola {request.Nombre}, tu cuenta de administrador fue creada. Email: {request.Email} | Contraseña temporal: {tempPassword}. Cámbiala en tu primer inicio de sesión.",
                    html),
                cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "No se pudo enviar el correo de invitación al usuario {Email}.", request.Email);
        }
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
