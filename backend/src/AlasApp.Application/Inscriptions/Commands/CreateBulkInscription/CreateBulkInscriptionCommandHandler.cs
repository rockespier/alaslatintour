using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.AdminSettings;
using AlasApp.Application.Common;
using AlasApp.Application.Inscriptions.Models;
using AlasApp.Domain.Entities;
using AlasApp.Domain.Enums;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Application.Inscriptions.Commands.CreateBulkInscription;

/// <summary>Crea las inscripciones del checkout en un único SaveChanges transaccional de EF.</summary>
public sealed class CreateBulkInscriptionCommandHandler(
    ICompetitorRepository competitorRepository,
    IInscriptionRepository inscriptionRepository,
    IAdminSettingsRepository adminSettingsRepository,
    IUnitOfWork unitOfWork,
    IClock clock)
    : IRequestHandler<CreateBulkInscriptionCommand, BulkInscriptionDto>
{
    public async Task<BulkInscriptionDto> Handle(CreateBulkInscriptionCommand request, CancellationToken cancellationToken)
    {
        Validate(request);
        var competitor = await competitorRepository.GetByIdAsync(request.CompetitorId, cancellationToken)
            ?? throw new NotFoundException("Competidor no encontrado.");
        if (competitor.License.Status != LicenseStatus.Activa)
        {
            throw new ValidationException("Solo los competidores verificados pueden inscribirse en una competencia.",
                [new ValidationError("competitorId", "El competidor debe estar verificado para inscribirse.")]);
        }

        var settings = AdminSettingsSerializer.DeserializeOrDefault(
            await adminSettingsRepository.GetJsonAsync(AdminSettingsDefaults.SettingsKey, cancellationToken));
        var groupId = Guid.NewGuid();
        var inscriptions = new List<Inscription>();
        var age = CalculateAge(competitor.FechaNacimiento, clock.UtcNow);

        foreach (var categoryId in request.CategoryIds.Distinct())
        {
            var pricing = await inscriptionRepository.GetPricingContextAsync(request.EventId, categoryId, cancellationToken)
                ?? throw new NotFoundException("Evento o categoria no encontrados para la inscripcion.");
            if (!IsGenderCompatible(competitor.Genero, pricing.CategoryGender))
            {
                throw new ValidationException("La categoria seleccionada no corresponde al genero del competidor.",
                    [new ValidationError("categoryIds", "Una categoria seleccionada no corresponde al genero del competidor.")]);
            }
            if (pricing.AgeRestriction && !((pricing.MinAge ?? int.MinValue) <= age && (pricing.MaxAge ?? int.MaxValue) >= age))
            {
                throw new ValidationException("La categoria seleccionada no corresponde a la edad del competidor.",
                    [new ValidationError("categoryIds", "Una categoria seleccionada no corresponde a la edad del competidor.")]);
            }
            if (await inscriptionRepository.ExistsDuplicateAsync(request.CompetitorId, request.EventId, categoryId, cancellationToken))
            {
                throw new ConflictException("El competidor ya esta inscrito en una de las categorias del evento.");
            }
            if (pricing.CategoryCapacity.HasValue && await inscriptionRepository.CountByEventCategoryAsync(request.EventId, categoryId, cancellationToken) >= pricing.CategoryCapacity.Value)
            {
                throw new ConflictException("El cupo de una de las categorias seleccionadas esta agotado.");
            }

            var baseAmount = pricing.UseCircuitTariffs
                ? pricing.CircuitTariffUsd ?? 0m
                : pricing.CustomTariffUsd ?? pricing.CircuitTariffUsd ?? 0m;
            var firstInGroup = inscriptions.Count == 0;
            var membershipFee = firstInGroup ? request.MembershipPlan switch
            {
                MembershipPlanOption.Anual => pricing.MembresiaAnualUsd,
                MembershipPlanOption.PorEvento => pricing.MembresiaPorEventoUsd,
                _ => 0m
            } : 0m;
            var administrativeFee = firstInGroup ? settings.General.AdministrativeFeeUsd : 0m;
            var inscription = Inscription.Create(
                request.CompetitorId, request.EventId, categoryId, request.ShirtNumber, request.PaymentMethod,
                baseAmount, administrativeFee, firstInGroup ? request.MembershipPlan : null, membershipFee,
                baseAmount + administrativeFee + membershipFee, request.Reglamento, request.RiesgosAceptados,
                request.UsoImagenAceptado, clock.UtcNow, groupId);
            inscription.SetCreated(clock.UtcNow);
            inscriptions.Add(inscription);
            await inscriptionRepository.AddAsync(inscription, cancellationToken);
        }

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DomainRuleException exception)
        {
            throw new ValidationException(exception.Message, [new ValidationError("body", exception.Message)]);
        }

        return new BulkInscriptionDto(groupId, inscriptions[0].Id, inscriptions.Select(x => x.Id).ToList(), inscriptions.Sum(x => x.MontoUsd));
    }

    private static void Validate(CreateBulkInscriptionCommand request)
    {
        var errors = new List<ValidationError>();
        if (request.CompetitorId == Guid.Empty) errors.Add(new ValidationError("competitorId", "El identificador del competidor es invalido."));
        if (request.EventId == Guid.Empty) errors.Add(new ValidationError("eventId", "El identificador del evento es invalido."));
        if (request.CategoryIds is null || request.CategoryIds.Count == 0 || request.CategoryIds.Any(x => x == Guid.Empty)) errors.Add(new ValidationError("categoryIds", "Debe seleccionar al menos una categoria valida."));
        if (!request.Reglamento) errors.Add(new ValidationError("reglamento", "El competidor debe aceptar el reglamento ALAS."));
        if (!request.RiesgosAceptados) errors.Add(new ValidationError("riesgosAceptados", "El competidor debe aceptar los riesgos de participar en una competencia de surf."));
        if (!request.UsoImagenAceptado) errors.Add(new ValidationError("usoImagenAceptado", "El competidor debe autorizar el uso de fotos y videos del evento."));
        if (errors.Count > 0) throw new ValidationException("La solicitud contiene errores de validacion.", errors);
    }

    private static int CalculateAge(DateTimeOffset fechaNacimiento, DateTimeOffset nowUtc)
    {
        var today = DateOnly.FromDateTime(nowUtc.UtcDateTime);
        var birthDate = DateOnly.FromDateTime(fechaNacimiento.UtcDateTime);
        var age = today.Year - birthDate.Year;
        if (today < birthDate.AddYears(age))
        {
            age--;
        }

        return age;
    }

    private static bool IsGenderCompatible(CompetitorGender competitorGender, CategoryGender categoryGender) =>
        categoryGender == CategoryGender.Ambos
        || (categoryGender == CategoryGender.Masculino && competitorGender == CompetitorGender.Masculino)
        || (categoryGender == CategoryGender.Femenino && competitorGender == CompetitorGender.Femenino);
}
