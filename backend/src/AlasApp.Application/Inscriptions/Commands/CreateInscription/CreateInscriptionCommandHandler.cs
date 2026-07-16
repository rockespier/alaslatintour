using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.AdminSettings;
using AlasApp.Application.Common;
using AlasApp.Application.Competitors.Models;
using AlasApp.Application.Emails;
using AlasApp.Application.Inscriptions.Models;
using AlasApp.Domain.Entities;
using AlasApp.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace AlasApp.Application.Inscriptions.Commands.CreateInscription;

public sealed class CreateInscriptionCommandHandler(
    ICompetitorRepository competitorRepository,
    IInscriptionRepository inscriptionRepository,
    IAdminSettingsRepository adminSettingsRepository,
    IEmailSender emailSender,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<CreateInscriptionCommandHandler> logger)
    : IRequestHandler<CreateInscriptionCommand, InscriptionDto>
{
    public async Task<InscriptionDto> Handle(CreateInscriptionCommand request, CancellationToken cancellationToken)
    {
        Validate(request);

        var competitor = await competitorRepository.GetByIdAsync(request.CompetitorId, cancellationToken)
            ?? throw new NotFoundException("Competidor no encontrado.");

        var pricingContext = await inscriptionRepository.GetPricingContextAsync(request.EventId, request.CategoryId, cancellationToken)
            ?? throw new NotFoundException("Evento o categoria no encontrados para la inscripcion.");

        if (!IsGenderCompatible(competitor.Genero, pricingContext.CategoryGender))
        {
            throw new ValidationException(
                "La categoria seleccionada no corresponde al genero del competidor.",
                [new ValidationError("categoryId", "La categoria seleccionada no corresponde al genero del competidor.")]);
        }

        if (await inscriptionRepository.ExistsDuplicateAsync(request.CompetitorId, request.EventId, request.CategoryId, cancellationToken))
        {
            throw new ConflictException("El competidor ya esta inscrito en esta categoria del evento.");
        }

        if (pricingContext.CategoryCapacity.HasValue)
        {
            var enrolled = await inscriptionRepository.CountByEventCategoryAsync(request.EventId, request.CategoryId, cancellationToken);
            if (enrolled >= pricingContext.CategoryCapacity.Value)
            {
                throw new ConflictException("El cupo de la categoria para este evento esta agotado.");
            }
        }

        var montoUsd = pricingContext.UseCircuitTariffs
            ? pricingContext.CircuitTariffUsd ?? 0m
            : pricingContext.CustomTariffUsd ?? pricingContext.CircuitTariffUsd ?? 0m;

        var settingsJson = await adminSettingsRepository.GetJsonAsync(AdminSettingsDefaults.SettingsKey, cancellationToken);
        var settings = AdminSettingsSerializer.DeserializeOrDefault(settingsJson);
        var administrativeFeeUsd = settings.General.AdministrativeFeeUsd;
        var totalMontoUsd = montoUsd + administrativeFeeUsd;

        try
        {
            var inscription = Inscription.Create(
                request.CompetitorId,
                request.EventId,
                request.CategoryId,
                request.ShirtNumber,
                request.PaymentMethod,
                montoUsd,
                administrativeFeeUsd,
                totalMontoUsd,
                request.Reglamento,
                request.RiesgosAceptados,
                request.UsoImagenAceptado,
                clock.UtcNow);

            inscription.SetCreated(clock.UtcNow);

            await inscriptionRepository.AddAsync(inscription, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var dto = await inscriptionRepository.GetByIdAsync(inscription.Id, cancellationToken)
                ?? throw new NotFoundException("Inscripcion no encontrada despues de crearla.");

            await NotifyAdminAsync(competitor, dto, cancellationToken);
            return dto;
        }
        catch (DomainRuleException exception)
        {
            throw new ValidationException(exception.Message, [new ValidationError("body", exception.Message)]);
        }
    }

    private async Task NotifyAdminAsync(CompetitorDto competitor, InscriptionDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var settingsJson = await adminSettingsRepository.GetJsonAsync(AdminSettingsDefaults.SettingsKey, cancellationToken);
            var settings = AdminSettingsSerializer.DeserializeOrDefault(settingsJson);

            if (!settings.Notifications.NotifyNewInscriptions)
            {
                return;
            }

            var recipients = new List<string> { settings.Notifications.AdminEmail };
            recipients.AddRange(settings.Notifications.AdditionalAdminEmails ?? []);

            var subject = $"Nueva inscripción — {dto.Event.Nombre}";
            var competitorName = $"{competitor.Nombre} {competitor.Apellido}";

            var html = TransactionalEmailTemplate.Render(
                "Inscripción",
                "Nueva inscripción recibida",
                $"El competidor {competitorName} se ha inscrito a {dto.Event.Nombre}.",
                "Competidor",
                competitorName,
                [
                    new EmailDetail("Evento", dto.Event.Nombre),
                    new EmailDetail("Categoría", dto.Category.Nombre),
                    new EmailDetail("País", competitor.Pais),
                    new EmailDetail("Método de pago", dto.PaymentMethod.ToString()),
                    new EmailDetail("Monto", $"USD {dto.MontoUsd:0.##}"),
                ],
                "Revisa el panel de Inscritos para gestionar el pago y validar la inscripción.",
                "Notificación automática de ALAS Latin Tour.");

            var textBody = $"Nueva inscripción: {competitorName} → {dto.Event.Nombre} / {dto.Category.Nombre}. Monto: USD {dto.MontoUsd:0.##}. Método: {dto.PaymentMethod}.";

            foreach (var to in recipients.Where(e => !string.IsNullOrWhiteSpace(e)))
            {
                await emailSender.SendAsync(new EmailMessage(to, subject, textBody, html), cancellationToken);
            }
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "No se pudo enviar la notificación de nueva inscripción.");
        }
    }

    private static void Validate(CreateInscriptionCommand request)
    {
        var errors = new List<ValidationError>();

        if (request.CompetitorId == Guid.Empty)
        {
            errors.Add(new ValidationError("competitorId", "El identificador del competidor es invalido."));
        }

        if (request.EventId == Guid.Empty)
        {
            errors.Add(new ValidationError("eventId", "El identificador del evento es invalido."));
        }

        if (request.CategoryId == Guid.Empty)
        {
            errors.Add(new ValidationError("categoryId", "El identificador de la categoria es invalido."));
        }

        if (!request.Reglamento)
        {
            errors.Add(new ValidationError("reglamento", "El competidor debe aceptar el reglamento ALAS."));
        }

        if (!request.RiesgosAceptados)
        {
            errors.Add(new ValidationError("riesgosAceptados", "El competidor debe aceptar los riesgos de participar en una competencia de surf."));
        }

        if (!request.UsoImagenAceptado)
        {
            errors.Add(new ValidationError("usoImagenAceptado", "El competidor debe autorizar el uso de fotos y videos del evento."));
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("La solicitud contiene errores de validacion.", errors);
        }
    }

    private static bool IsGenderCompatible(Domain.Enums.CompetitorGender competitorGender, Domain.Enums.CategoryGender categoryGender)
    {
        return categoryGender == Domain.Enums.CategoryGender.Ambos
            || (categoryGender == Domain.Enums.CategoryGender.Masculino && competitorGender == Domain.Enums.CompetitorGender.Masculino)
            || (categoryGender == Domain.Enums.CategoryGender.Femenino && competitorGender == Domain.Enums.CompetitorGender.Femenino);
    }
}
