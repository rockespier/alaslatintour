using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Common;
using AlasApp.Application.Inscriptions.Models;
using AlasApp.Domain.Entities;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Application.Inscriptions.Commands.CreateInscription;

public sealed class CreateInscriptionCommandHandler(
    ICompetitorRepository competitorRepository,
    IInscriptionRepository inscriptionRepository,
    IUnitOfWork unitOfWork,
    IClock clock)
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

        try
        {
            var inscription = Inscription.Create(
                request.CompetitorId,
                request.EventId,
                request.CategoryId,
                request.ShirtNumber,
                request.PaymentMethod,
                montoUsd,
                request.Reglamento,
                clock.UtcNow);

            inscription.SetCreated(clock.UtcNow);

            await inscriptionRepository.AddAsync(inscription, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return await inscriptionRepository.GetByIdAsync(inscription.Id, cancellationToken)
                ?? throw new NotFoundException("Inscripcion no encontrada despues de crearla.");
        }
        catch (DomainRuleException exception)
        {
            throw new ValidationException(exception.Message, [new ValidationError("body", exception.Message)]);
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
