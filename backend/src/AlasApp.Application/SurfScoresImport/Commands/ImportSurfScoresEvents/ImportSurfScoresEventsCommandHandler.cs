using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.AdminSettings;
using AlasApp.Application.Categories.Models;
using AlasApp.Application.Common;
using AlasApp.Application.EventCategories.Models;
using AlasApp.Application.Events.Models;
using AlasApp.Application.SurfScoresImport.Models;
using AlasApp.Domain.Entities;
using AlasApp.Domain.Enums;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Application.SurfScoresImport.Commands.ImportSurfScoresEvents;

public sealed class ImportSurfScoresEventsCommandHandler(
    ICircuitRepository circuitRepository,
    IEventRepository eventRepository,
    ICategoryRepository categoryRepository,
    IEventCategoryRepository eventCategoryRepository,
    IAdminSettingsRepository adminSettingsRepository,
    ISurfScoresImportGateway gateway,
    IUnitOfWork unitOfWork,
    IClock clock)
    : IRequestHandler<ImportSurfScoresEventsCommand, SurfScoresImportResultDto>
{
    private const string DefaultCiudad = "Por definir";
    private const string DefaultPaisPlaya = "Por definir";
    private const int DefaultStars = 1;
    private const int DefaultCapacidadMaxima = 0;

    public async Task<SurfScoresImportResultDto> Handle(ImportSurfScoresEventsCommand request, CancellationToken cancellationToken)
    {
        if (request.CircuitId == Guid.Empty)
        {
            throw new ValidationException(
                "La solicitud contiene errores de validacion.",
                [new ValidationError("circuitId", "El circuito es obligatorio.")]);
        }

        if (await circuitRepository.GetEntityByIdAsync(request.CircuitId, cancellationToken) is null)
        {
            throw new NotFoundException("Circuito no encontrado.");
        }

        var settingsJson = await adminSettingsRepository.GetJsonAsync(AdminSettingsDefaults.SettingsKey, cancellationToken);
        var settings = AdminSettingsSerializer.DeserializeOrDefault(settingsJson).Integrations.SurfScores;

        if (string.IsNullOrWhiteSpace(settings.Endpoint) || string.IsNullOrWhiteSpace(settings.Username) || string.IsNullOrWhiteSpace(settings.Password) || string.IsNullOrWhiteSpace(settings.OrganizacionId))
        {
            throw new ValidationException(
                "La integracion con SurfScores no esta configurada.",
                [new ValidationError("integrations.surfScores", "Configura el endpoint, usuario, contrasena y organizacion de SurfScores antes de importar.")]);
        }

        var remoteEvents = await gateway.GetOrganizationEventsAsync(settings, cancellationToken);

        var existingEvents = await eventRepository.ListAsync(
            new EventListFilter(1, int.MaxValue, request.CircuitId, null, null, null, null),
            cancellationToken);
        var knownNames = existingEvents.Items.Select(x => x.Nombre).ToList();

        var categoryCodeMap = (await categoryRepository.ListAsync(new CategoryListFilter(null), cancellationToken))
            .Where(x => !string.IsNullOrWhiteSpace(x.SurfScoresCode))
            .GroupBy(x => NormalizeCode(x.SurfScoresCode!))
            .ToDictionary(g => g.Key, g => g.First().Id);

        var created = new List<SurfScoresImportedEventDto>();
        var skipped = new List<SurfScoresImportSkippedEventDto>();

        foreach (var remoteEvent in remoteEvents)
        {
            var nombre = string.IsNullOrWhiteSpace(remoteEvent.Name) ? $"Evento SurfScores {remoteEvent.Id}" : remoteEvent.Name.Trim();

            var duplicate = knownNames.FirstOrDefault(existing => AreNamesSimilar(existing, nombre));
            if (duplicate is not null)
            {
                skipped.Add(new SurfScoresImportSkippedEventDto(nombre, remoteEvent.Id, $"Ya existe un evento similar en este circuito: \"{duplicate}\"."));
                continue;
            }

            try
            {
                var fechaInicio = remoteEvent.StartDate ?? clock.UtcNow;
                var fechaFin = remoteEvent.EndDate ?? fechaInicio;

                var @event = Event.Create(
                    request.CircuitId,
                    nombre,
                    fechaInicio,
                    fechaFin,
                    string.IsNullOrWhiteSpace(remoteEvent.Country) ? DefaultPaisPlaya : remoteEvent.Country.Trim(),
                    DefaultCiudad,
                    string.IsNullOrWhiteSpace(remoteEvent.Place) ? DefaultPaisPlaya : remoteEvent.Place.Trim(),
                    DefaultStars,
                    DefaultCapacidadMaxima,
                    0m,
                    null,
                    remoteEvent.Id,
                    EventAccessType.Abierto,
                    EventStatusAdmin.Borrador);

                @event.SetCreated(clock.UtcNow);

                var remoteCategories = await gateway.GetEventCategoriesAsync(settings, remoteEvent.Id, cancellationToken);

                var matchedCategoryIds = new List<Guid>();
                var unmatchedCodes = new List<string>();

                foreach (var remoteCategory in remoteCategories)
                {
                    if (categoryCodeMap.TryGetValue(NormalizeCode(remoteCategory.Id), out var categoryId))
                    {
                        matchedCategoryIds.Add(categoryId);
                    }
                    else
                    {
                        unmatchedCodes.Add(string.IsNullOrWhiteSpace(remoteCategory.Name) ? remoteCategory.Id : $"{remoteCategory.Name} ({remoteCategory.Id})");
                    }
                }

                if (matchedCategoryIds.Count > 0)
                {
                    var items = matchedCategoryIds
                        .Select(categoryId => new EventCategoryUpsertItem(categoryId, null, null, null, null))
                        .ToList();

                    var assignments = await eventCategoryRepository.BuildAssignmentsAsync(@event.Id, items, cancellationToken);
                    @event.ReplaceCategories(assignments, true);
                }

                await eventRepository.AddAsync(@event, cancellationToken);

                knownNames.Add(nombre);
                created.Add(new SurfScoresImportedEventDto(@event.Id, nombre, remoteEvent.Id, matchedCategoryIds.Count, unmatchedCodes));
            }
            catch (DomainRuleException exception)
            {
                skipped.Add(new SurfScoresImportSkippedEventDto(nombre, remoteEvent.Id, exception.Message));
            }
            catch (NotFoundException exception)
            {
                skipped.Add(new SurfScoresImportSkippedEventDto(nombre, remoteEvent.Id, exception.Message));
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new SurfScoresImportResultDto(remoteEvents.Count, created, skipped);
    }

    private static string NormalizeCode(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    private static bool AreNamesSimilar(string a, string b)
    {
        var normalizedA = NormalizeName(a);
        var normalizedB = NormalizeName(b);

        if (normalizedA.Length == 0 || normalizedB.Length == 0)
        {
            return false;
        }

        return normalizedA == normalizedB
            || normalizedA.Contains(normalizedB, StringComparison.Ordinal)
            || normalizedB.Contains(normalizedA, StringComparison.Ordinal);
    }

    private static string NormalizeName(string value)
    {
        return string.Join(' ', value.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
