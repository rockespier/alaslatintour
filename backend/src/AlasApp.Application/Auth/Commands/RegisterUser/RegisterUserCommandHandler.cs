using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Auth.Models;
using AlasApp.Application.Common;
using AlasApp.Domain.Entities;
using AlasApp.Domain.Enums;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Application.Auth.Commands.RegisterUser;

public sealed class RegisterUserCommandHandler(
    IUserAccountRepository userAccountRepository,
    ICompetitorRepository competitorRepository,
    IPasswordHasher passwordHasher,
    IIdentityDocumentStorage identityDocumentStorage,
    IUnitOfWork unitOfWork,
    IClock clock)
    : IRequestHandler<RegisterUserCommand, RegisterResultDto>
{
    public async Task<RegisterResultDto> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        Validate(request);

        if (await userAccountRepository.EmailExistsAsync(request.Email, cancellationToken) ||
            await competitorRepository.EmailExistsAsync(request.Email, null, cancellationToken))
        {
            throw new ConflictException("El correo electrónico ya está registrado.");
        }

        Guid? competitorId = null;
        LicenseStatus? licenseStatus = null;

        if (request.Tipo == UserType.Competidor)
        {
            try
            {
                var competitor = Competitor.Create(
                    request.Nombre,
                    request.Apellido,
                    request.Email,
                    request.FechaNacimiento!.Value,
                    request.Genero!.Value,
                    request.Pais,
                    request.Telefono,
                    request.Club,
                    request.Postura!.Value,
                    request.TallaCamiseta!.Value,
                    string.Empty,
                    request.Patrocinadores,
                    request.Federacion);

                var blobName = await identityDocumentStorage.UploadAsync(competitor.Id, request.IdentityDocument!, cancellationToken);
                competitor.AttachIdentityDocument(blobName, clock.UtcNow);
                competitor.SetCreated(clock.UtcNow);
                await competitorRepository.AddAsync(competitor, cancellationToken);

                competitorId = competitor.Id;
                licenseStatus = competitor.LicenseStatus;
            }
            catch (DomainRuleException exception)
            {
                throw new ValidationException(exception.Message, [new ValidationError("body", exception.Message)]);
            }
        }

        UserAccount userAccount;

        try
        {
            userAccount = UserAccount.Create(
                request.Email,
                passwordHasher.Hash(request.Password),
                request.Nombre,
                request.Apellido,
                request.Tipo,
                request.Pais,
                request.IdiomaPreferido,
                request.Newsletter,
                request.Terminos,
                request.Reglamento,
                competitorId);
        }
        catch (DomainRuleException exception)
        {
            throw new ValidationException(exception.Message, [new ValidationError("body", exception.Message)]);
        }

        userAccount.SetCreated(clock.UtcNow);
        await userAccountRepository.AddAsync(userAccount, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegisterResultDto(
            userAccount.Id,
            userAccount.Email,
            userAccount.Tipo,
            licenseStatus,
            request.Tipo == UserType.Competidor
                ? "Registro exitoso. Tu licencia será validada en las próximas 48 horas."
                : "Registro exitoso.");
    }

    private static void Validate(RegisterUserCommand request)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors.Add(new ValidationError("email", "El email es obligatorio."));
        }

        if (!PasswordPolicy.IsValid(request.Password))
        {
            errors.Add(new ValidationError("password", PasswordPolicy.Message));
        }

        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            errors.Add(new ValidationError("nombre", "El nombre es obligatorio."));
        }

        if (string.IsNullOrWhiteSpace(request.Apellido))
        {
            errors.Add(new ValidationError("apellido", "El apellido es obligatorio."));
        }

        if (!request.Terminos)
        {
            errors.Add(new ValidationError("terminos", "Debes aceptar los términos."));
        }

        if (request.Tipo == UserType.Competidor)
        {
            if (!request.Reglamento)
            {
                errors.Add(new ValidationError("reglamento", "Debes aceptar el reglamento."));
            }

            if (!request.FechaNacimiento.HasValue || request.FechaNacimiento.Value == default)
            {
                errors.Add(new ValidationError("fechaNacimiento", "La fecha de nacimiento es obligatoria."));
            }

            if (!request.Genero.HasValue)
            {
                errors.Add(new ValidationError("genero", "El género es obligatorio."));
            }

            if (!request.Postura.HasValue)
            {
                errors.Add(new ValidationError("postura", "La postura es obligatoria."));
            }

            if (!request.TallaCamiseta.HasValue)
            {
                errors.Add(new ValidationError("tallaCamiseta", "La talla de camiseta es obligatoria."));
            }

            if (string.IsNullOrWhiteSpace(request.Pais))
            {
                errors.Add(new ValidationError("pais", "El país es obligatorio."));
            }

            if (request.IdentityDocument is null || request.IdentityDocument.Length <= 0)
            {
                errors.Add(new ValidationError("identityDocument", "El documento de identidad es obligatorio para competidores."));
            }
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("La solicitud contiene errores de validacion.", errors);
        }
    }
}
