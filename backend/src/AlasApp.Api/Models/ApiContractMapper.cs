using AlasApp.Application.Circuits.Commands.CreateCircuit;
using AlasApp.Application.Circuits.Commands.UpdateCircuit;
using AlasApp.Application.Circuits.Models;
using AlasApp.Application.Common;
using AlasApp.Application.Categories.Commands.CreateCategory;
using AlasApp.Application.Categories.Commands.UpdateCategory;
using AlasApp.Application.Categories.Models;
using AlasApp.Application.EventCategories.Commands.UpdateEventCategories;
using AlasApp.Application.EventCategories.Models;
using AlasApp.Application.Events.Commands.CreateEvent;
using AlasApp.Application.Events.Commands.UpdateEvent;
using AlasApp.Application.Events.Models;
using AlasApp.Domain.Enums;
using Generated = AlasApp.AlasApi.Api.Controllers;

namespace AlasApp.Api.Models;

public static class ApiContractMapper
{
    public static Generated.CircuitListResponse ToContract(PagedResult<CircuitDto> result)
    {
        return new Generated.CircuitListResponse(
            result.Items.Select(ToContract).ToList(),
            new Generated.PaginationMeta(result.CurrentPage, result.ItemsPerPage, result.TotalItems, result.TotalPages));
    }

    public static Generated.CircuitResponse ToContract(CircuitDto dto)
    {
        return new Generated.CircuitResponse(
            dto.CompetidoresCount,
            dto.CreatedAtUtc,
            dto.Descripcion ?? string.Empty,
            ToGeneratedCircuitStatus(dto.Estado),
            dto.EventsCount,
            dto.Id.ToString(),
            dto.LastSyncAt,
            ToGeneratedCircuitModalidad(dto.Modalidad),
            dto.Nombre,
            ToGeneratedCircuitRegion(dto.Region),
            dto.SurfScoresCode ?? string.Empty,
            dto.Temporada,
            (float)dto.TotalPrizeUsd,
            dto.UpdatedAtUtc);
    }

    public static Generated.EventListResponse ToContract(PagedResult<EventDto> result)
    {
        return new Generated.EventListResponse(
            result.Items.Select(ToContract).ToList(),
            new Generated.PaginationMeta(result.CurrentPage, result.ItemsPerPage, result.TotalItems, result.TotalPages));
    }

    public static Generated.EventResponse ToContract(EventDto dto)
    {
        return new Generated.EventResponse(
            ToGeneratedEventAccessType(dto.AccessType),
            dto.CapacidadMaxima,
            dto.CircuitId.ToString(),
            dto.Ciudad,
            dto.CreatedAtUtc,
            dto.EnrolledCount,
            ToGeneratedEventStatusAdmin(dto.Estado),
            dto.FechaFin,
            dto.FechaInicio,
            dto.Id.ToString(),
            dto.Lugar,
            dto.Nombre,
            dto.Pais,
            dto.Playa,
            (float)dto.PrizeAmountUsd,
            dto.Stars,
            ToGeneratedEventStatusPublic(dto.StatusPublic),
            dto.SurfScoresCode ?? string.Empty,
            dto.UpdatedAtUtc);
    }

    public static Generated.Response6 ToContract(IReadOnlyCollection<CategoryDto> categories)
    {
        return new Generated.Response6(categories.Select(ToContract).ToList());
    }

    public static Generated.CategoryResponse ToContract(CategoryDto dto)
    {
        return new Generated.CategoryResponse(
            dto.AgeRestriction,
            dto.CreatedAtUtc,
            dto.Descripcion ?? string.Empty,
            ToGeneratedCategoryGender(dto.Gender),
            dto.Id.ToString(),
            dto.MaxAge,
            dto.MinAge,
            dto.Nombre,
            ToGeneratedCategoryStatus(dto.Status),
            dto.SuccessorCategory is null ? null : new Generated.SuccessorCategory(dto.SuccessorCategory.Id.ToString(), dto.SuccessorCategory.Nombre),
            dto.SuccessorCategoryId?.ToString());
    }

    public static Generated.Response ToContract(EventCategoryListDto dto)
    {
        return new Generated.Response(dto.Data.Select(ToContract).ToList(), dto.UseCircuitTariffs);
    }

    public static Generated.Response2 ToUpdatedEventCategoriesContract(EventCategoryListDto dto)
    {
        return new Generated.Response2(dto.Data.Select(ToContract).ToList());
    }

    public static Generated.EventCategoryResponse ToContract(EventCategoryDto dto)
    {
        return new Generated.EventCategoryResponse(
            dto.Capacidad,
            dto.CategoryId.ToString(),
            dto.CategoryName,
            dto.CustomTariffCop.HasValue ? (float)dto.CustomTariffCop.Value : null,
            dto.CustomTariffUsd.HasValue ? (float)dto.CustomTariffUsd.Value : null,
            (float)dto.EffectiveTariffCop,
            (float)dto.EffectiveTariffUsd,
            dto.EnrolledCount);
    }

    public static CreateCircuitCommand ToCommand(Generated.CircuitRequest request)
    {
        return new CreateCircuitCommand(
            request.Nombre,
            request.Temporada,
            NormalizeOptional(request.Descripcion),
            ToDomainCircuitRegion(request.Region),
            ToDomainCircuitModalidad(request.Modalidad),
            ToDomainCircuitStatus(request.Estado),
            NormalizeOptional(request.SurfScoresCode));
    }

    public static UpdateCircuitCommand ToCommand(Guid circuitId, Generated.CircuitRequest request)
    {
        return new UpdateCircuitCommand(
            circuitId,
            request.Nombre,
            request.Temporada,
            NormalizeOptional(request.Descripcion),
            ToDomainCircuitRegion(request.Region),
            ToDomainCircuitModalidad(request.Modalidad),
            ToDomainCircuitStatus(request.Estado),
            NormalizeOptional(request.SurfScoresCode));
    }

    public static CreateEventCommand ToCommand(Generated.EventRequest request)
    {
        return new CreateEventCommand(
            ParseGuid(request.CircuitId, "circuitId"),
            request.Nombre,
            request.FechaInicio,
            request.FechaFin,
            request.Pais,
            request.Ciudad,
            request.Playa,
            request.Stars,
            request.CapacidadMaxima,
            (decimal)request.PrizeAmountUsd,
            NormalizeOptional(request.SurfScoresCode),
            ToDomainEventAccessType(request.AccessType),
            ToDomainEventStatusAdmin(request.Estado));
    }

    public static CreateCategoryCommand ToCommand(Generated.CategoryRequest request)
    {
        return new CreateCategoryCommand(
            request.Nombre,
            NormalizeOptional(request.Descripcion),
            ToDomainCategoryGender(request.Gender),
            request.AgeRestriction,
            request.MinAge,
            request.MaxAge,
            ParseNullableGuid(request.SuccessorCategoryId, "successorCategoryId"),
            ToDomainCategoryStatus(request.Status));
    }

    public static UpdateCategoryCommand ToCommand(Guid categoryId, Generated.CategoryRequest request)
    {
        return new UpdateCategoryCommand(
            categoryId,
            request.Nombre,
            NormalizeOptional(request.Descripcion),
            ToDomainCategoryGender(request.Gender),
            request.AgeRestriction,
            request.MinAge,
            request.MaxAge,
            ParseNullableGuid(request.SuccessorCategoryId, "successorCategoryId"),
            ToDomainCategoryStatus(request.Status));
    }

    public static UpdateEventCommand ToCommand(Guid eventId, Generated.EventRequest request)
    {
        return new UpdateEventCommand(
            eventId,
            ParseGuid(request.CircuitId, "circuitId"),
            request.Nombre,
            request.FechaInicio,
            request.FechaFin,
            request.Pais,
            request.Ciudad,
            request.Playa,
            request.Stars,
            request.CapacidadMaxima,
            (decimal)request.PrizeAmountUsd,
            NormalizeOptional(request.SurfScoresCode),
            ToDomainEventAccessType(request.AccessType),
            ToDomainEventStatusAdmin(request.Estado));
    }

    public static UpdateEventCategoriesCommand ToCommand(Guid eventId, Generated.Body body)
    {
        return new UpdateEventCategoriesCommand(
            eventId,
            body.UseCircuitTariffs,
            body.Categories.Select(x => new EventCategoryUpsertItem(
                ParseGuid(x.CategoryId, "categoryId"),
                x.CustomTariffUsd.HasValue ? (decimal)x.CustomTariffUsd.Value : null,
                x.CustomTariffCop.HasValue ? (decimal)x.CustomTariffCop.Value : null,
                x.Capacidad))
            .ToList());
    }

    public static CircuitStatus? ToDomainCircuitStatus(Generated.CircuitStatus? value)
    {
        return value.HasValue ? ToDomainCircuitStatus(value.Value) : null;
    }

    public static CircuitStatus? ParseCircuitStatus(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : NormalizeEnumText(value) switch
            {
                "activo" => CircuitStatus.Activo,
                "borrador" => CircuitStatus.Borrador,
                "archivado" => CircuitStatus.Archivado,
                "proximo" => CircuitStatus.Proximo,
                _ => throw new ValidationException("Estado de circuito invalido.", [new ValidationError("status", "Estado de circuito invalido.")])
            };
    }

    public static CircuitStatus ToDomainCircuitStatus(Generated.CircuitStatus value)
    {
        return value switch
        {
            Generated.CircuitStatus.Activo => CircuitStatus.Activo,
            Generated.CircuitStatus.Borrador => CircuitStatus.Borrador,
            Generated.CircuitStatus.Archivado => CircuitStatus.Archivado,
            Generated.CircuitStatus.Próximo => CircuitStatus.Proximo,
            _ => throw new ValidationException("Estado de circuito invalido.", [new ValidationError("estado", "Estado de circuito invalido.")])
        };
    }

    public static CircuitModalidad? ToDomainCircuitModalidad(Generated.CircuitModalidad? value)
    {
        return value.HasValue ? ToDomainCircuitModalidad(value.Value) : null;
    }

    public static CircuitModalidad? ParseCircuitModalidad(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : NormalizeEnumText(value) switch
            {
                "shortboard" => CircuitModalidad.Shortboard,
                "longboard" => CircuitModalidad.Longboard,
                "mixed" => CircuitModalidad.Mixed,
                _ => throw new ValidationException("Modalidad invalida.", [new ValidationError("modalidad", "Modalidad invalida.")])
            };
    }

    public static CircuitModalidad ToDomainCircuitModalidad(Generated.CircuitModalidad value)
    {
        return value switch
        {
            Generated.CircuitModalidad.Shortboard => CircuitModalidad.Shortboard,
            Generated.CircuitModalidad.Longboard => CircuitModalidad.Longboard,
            Generated.CircuitModalidad.Mixed => CircuitModalidad.Mixed,
            _ => throw new ValidationException("Modalidad invalida.", [new ValidationError("modalidad", "Modalidad invalida.")])
        };
    }

    public static CircuitRegion ToDomainCircuitRegion(Generated.CircuitRegion value)
    {
        return value switch
        {
            Generated.CircuitRegion.Latinoamérica => CircuitRegion.Latinoamerica,
            Generated.CircuitRegion.América_del_Sur => CircuitRegion.AmericaDelSur,
            Generated.CircuitRegion.América_Central => CircuitRegion.AmericaCentral,
            Generated.CircuitRegion.América_del_Norte => CircuitRegion.AmericaDelNorte,
            _ => throw new ValidationException("Region invalida.", [new ValidationError("region", "Region invalida.")])
        };
    }

    public static EventStatusPublic? ToDomainEventStatusPublic(Generated.EventStatusPublic? value)
    {
        return value.HasValue ? ToDomainEventStatusPublic(value.Value) : null;
    }

    public static CategoryStatus? ParseCategoryStatus(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : NormalizeEnumText(value) switch
            {
                "activo" => CategoryStatus.Activo,
                "inactivo" => CategoryStatus.Inactivo,
                _ => throw new ValidationException("Estado de categoria invalido.", [new ValidationError("status", "Estado de categoria invalido.")])
            };
    }

    public static EventStatusPublic? ParseEventStatusPublic(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : NormalizeEnumText(value) switch
            {
                "inscripcionesabiertas" => EventStatusPublic.InscripcionesAbiertas,
                "proximamente" => EventStatusPublic.Proximamente,
                "completado" => EventStatusPublic.Completado,
                "cerrado" => EventStatusPublic.Cerrado,
                _ => throw new ValidationException("Estado publico invalido.", [new ValidationError("status", "Estado publico invalido.")])
            };
    }

    public static EventStatusPublic ToDomainEventStatusPublic(Generated.EventStatusPublic value)
    {
        return value switch
        {
            Generated.EventStatusPublic.Inscripciones_Abiertas => EventStatusPublic.InscripcionesAbiertas,
            Generated.EventStatusPublic.Próximamente => EventStatusPublic.Proximamente,
            Generated.EventStatusPublic.Completado => EventStatusPublic.Completado,
            Generated.EventStatusPublic.Cerrado => EventStatusPublic.Cerrado,
            _ => throw new ValidationException("Estado publico invalido.", [new ValidationError("status", "Estado publico invalido.")])
        };
    }

    public static EventStatusAdmin ToDomainEventStatusAdmin(Generated.EventStatusAdmin value)
    {
        return value switch
        {
            Generated.EventStatusAdmin.Activo => EventStatusAdmin.Activo,
            Generated.EventStatusAdmin.Próximamente => EventStatusAdmin.Proximo,
            Generated.EventStatusAdmin.Completado => EventStatusAdmin.Completado,
            Generated.EventStatusAdmin.Cancelado => EventStatusAdmin.Cancelado,
            Generated.EventStatusAdmin.Borrador => EventStatusAdmin.Borrador,
            _ => throw new ValidationException("Estado de evento invalido.", [new ValidationError("estado", "Estado de evento invalido.")])
        };
    }

    public static EventAccessType ToDomainEventAccessType(Generated.EventAccessType value)
    {
        return value switch
        {
            Generated.EventAccessType.Abierto => EventAccessType.Abierto,
            Generated.EventAccessType.Restringido => EventAccessType.Restringido,
            Generated.EventAccessType.Solo_invitación => EventAccessType.SoloInvitacion,
            _ => throw new ValidationException("Tipo de acceso invalido.", [new ValidationError("accessType", "Tipo de acceso invalido.")])
        };
    }

    public static CategoryStatus ToDomainCategoryStatus(Generated.CategoryStatus value)
    {
        return value switch
        {
            Generated.CategoryStatus.Activo => CategoryStatus.Activo,
            Generated.CategoryStatus.Inactivo => CategoryStatus.Inactivo,
            _ => throw new ValidationException("Estado de categoria invalido.", [new ValidationError("status", "Estado de categoria invalido.")])
        };
    }

    public static CategoryGender ToDomainCategoryGender(Generated.CategoryGender value)
    {
        return value switch
        {
            Generated.CategoryGender.Masculino => CategoryGender.Masculino,
            Generated.CategoryGender.Femenino => CategoryGender.Femenino,
            Generated.CategoryGender.Ambos => CategoryGender.Ambos,
            _ => throw new ValidationException("Genero de categoria invalido.", [new ValidationError("gender", "Genero de categoria invalido.")])
        };
    }

    private static Generated.CircuitStatus ToGeneratedCircuitStatus(CircuitStatus value)
    {
        return value switch
        {
            CircuitStatus.Activo => Generated.CircuitStatus.Activo,
            CircuitStatus.Borrador => Generated.CircuitStatus.Borrador,
            CircuitStatus.Archivado => Generated.CircuitStatus.Archivado,
            CircuitStatus.Proximo => Generated.CircuitStatus.Próximo,
            _ => Generated.CircuitStatus.Borrador
        };
    }

    private static Generated.CircuitModalidad ToGeneratedCircuitModalidad(CircuitModalidad value)
    {
        return value switch
        {
            CircuitModalidad.Shortboard => Generated.CircuitModalidad.Shortboard,
            CircuitModalidad.Longboard => Generated.CircuitModalidad.Longboard,
            CircuitModalidad.Mixed => Generated.CircuitModalidad.Mixed,
            _ => Generated.CircuitModalidad.Shortboard
        };
    }

    private static Generated.CircuitRegion ToGeneratedCircuitRegion(CircuitRegion value)
    {
        return value switch
        {
            CircuitRegion.Latinoamerica => Generated.CircuitRegion.Latinoamérica,
            CircuitRegion.AmericaDelSur => Generated.CircuitRegion.América_del_Sur,
            CircuitRegion.AmericaCentral => Generated.CircuitRegion.América_Central,
            CircuitRegion.AmericaDelNorte => Generated.CircuitRegion.América_del_Norte,
            _ => Generated.CircuitRegion.Latinoamérica
        };
    }

    private static Generated.EventStatusAdmin ToGeneratedEventStatusAdmin(EventStatusAdmin value)
    {
        return value switch
        {
            EventStatusAdmin.Activo => Generated.EventStatusAdmin.Activo,
            EventStatusAdmin.Proximo => Generated.EventStatusAdmin.Próximamente,
            EventStatusAdmin.Completado => Generated.EventStatusAdmin.Completado,
            EventStatusAdmin.Cancelado => Generated.EventStatusAdmin.Cancelado,
            EventStatusAdmin.Borrador => Generated.EventStatusAdmin.Borrador,
            _ => Generated.EventStatusAdmin.Borrador
        };
    }

    private static Generated.EventStatusPublic ToGeneratedEventStatusPublic(EventStatusPublic value)
    {
        return value switch
        {
            EventStatusPublic.InscripcionesAbiertas => Generated.EventStatusPublic.Inscripciones_Abiertas,
            EventStatusPublic.Proximamente => Generated.EventStatusPublic.Próximamente,
            EventStatusPublic.Completado => Generated.EventStatusPublic.Completado,
            EventStatusPublic.Cerrado => Generated.EventStatusPublic.Cerrado,
            _ => Generated.EventStatusPublic.Cerrado
        };
    }

    private static Generated.EventAccessType ToGeneratedEventAccessType(EventAccessType value)
    {
        return value switch
        {
            EventAccessType.Abierto => Generated.EventAccessType.Abierto,
            EventAccessType.Restringido => Generated.EventAccessType.Restringido,
            EventAccessType.SoloInvitacion => Generated.EventAccessType.Solo_invitación,
            _ => Generated.EventAccessType.Abierto
        };
    }

    private static Generated.CategoryStatus ToGeneratedCategoryStatus(CategoryStatus value)
    {
        return value switch
        {
            CategoryStatus.Activo => Generated.CategoryStatus.Activo,
            CategoryStatus.Inactivo => Generated.CategoryStatus.Inactivo,
            _ => Generated.CategoryStatus.Activo
        };
    }

    private static Generated.CategoryGender ToGeneratedCategoryGender(CategoryGender value)
    {
        return value switch
        {
            CategoryGender.Masculino => Generated.CategoryGender.Masculino,
            CategoryGender.Femenino => Generated.CategoryGender.Femenino,
            CategoryGender.Ambos => Generated.CategoryGender.Ambos,
            _ => Generated.CategoryGender.Ambos
        };
    }

    public static Guid ParseGuid(string value, string field)
    {
        if (Guid.TryParse(value, out var guid))
        {
            return guid;
        }

        throw new ValidationException(
            "La solicitud contiene errores de validacion.",
            [new ValidationError(field, $"El valor '{value}' no es un GUID valido.")]);
    }

    public static Guid? ParseNullableGuid(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return ParseGuid(value, field);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string NormalizeEnumText(string value)
    {
        return new string(
            value
                .Trim()
                .ToLowerInvariant()
                .Normalize(System.Text.NormalizationForm.FormD)
                .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                .Where(c => c != ' ' && c != '_' && c != '-')
                .ToArray());
    }
}
