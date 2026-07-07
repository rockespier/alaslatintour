using AlasApp.Application.Circuits.Commands.CreateCircuit;
using AlasApp.Application.Circuits.Commands.UpdateCircuit;
using AlasApp.Application.Circuits.Models;
using AlasApp.Application.Common;
using AlasApp.Application.Categories.Commands.CreateCategory;
using AlasApp.Application.Categories.Commands.UpdateCategory;
using AlasApp.Application.Categories.Models;
using AlasApp.Application.CategoryTariffs.Commands.UpsertCategoryTariff;
using AlasApp.Application.CategoryTariffs.Models;
using AlasApp.Application.Auth.Commands.LoginUser;
using AlasApp.Application.Auth.Commands.RegisterUser;
using AlasApp.Application.Auth.Models;
using AlasApp.Application.Competitors.Commands.CreateCompetitor;
using AlasApp.Application.Competitors.Commands.UpdateCompetitor;
using AlasApp.Application.Competitors.Models;
using AlasApp.Application.EventCategories.Commands.UpdateEventCategories;
using AlasApp.Application.EventCategories.Models;
using AlasApp.Application.Events.Commands.CreateEvent;
using AlasApp.Application.Events.Commands.UpdateEvent;
using AlasApp.Application.Events.Models;
using AlasApp.Application.Inscriptions.Commands.CreateInscription;
using AlasApp.Application.Inscriptions.Commands.UpdateInscription;
using AlasApp.Application.Inscriptions.Models;
using AlasApp.Application.Payments.Commands.CreatePayment;
using AlasApp.Application.Payments.Commands.RequestBeachToken;
using AlasApp.Application.Payments.Commands.RedeemBeachToken;
using AlasApp.Application.Payments.Commands.RejectBeachToken;
using AlasApp.Application.Payments.Commands.UpdatePayment;
using AlasApp.Application.Payments.Models;
using AlasApp.Application.Rankings.Models;
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

    public static Generated.LoginResponse ToContract(LoginResultDto dto)
    {
        return new Generated.LoginResponse(dto.AccessToken, dto.ExpiresIn, ToContract(dto.User));
    }

    public static Generated.AuthenticatedUser ToContract(AuthenticatedUserDto dto)
    {
        return new Generated.AuthenticatedUser(
            dto.AdminRole.HasValue ? ToGeneratedAdminRole(dto.AdminRole.Value) : null,
            dto.Email,
            dto.FullName,
            dto.Id.ToString(),
            ToGeneratedUserType(dto.Tipo));
    }

    public static Generated.RegisterResponse ToContract(RegisterResultDto dto)
    {
        return new Generated.RegisterResponse(
            dto.Email,
            dto.Id.ToString(),
            dto.LicenseStatus.HasValue ? ToGeneratedLicenseStatus(dto.LicenseStatus.Value) : null,
            dto.Message,
            ToGeneratedUserType(dto.Tipo));
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

    public static Generated.Response7 ToContract(IReadOnlyCollection<CategoryTariffDto> tariffs)
    {
        return new Generated.Response7(tariffs.Select(ToContract).ToList());
    }

    public static Generated.TariffResponse ToContract(CategoryTariffDto dto)
    {
        return new Generated.TariffResponse(dto.Active, (float)dto.Cop, dto.StarLevel, (float)dto.Usd);
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

    public static Generated.CompetitorListResponse ToContract(PagedResult<CompetitorDto> result)
    {
        return new Generated.CompetitorListResponse(
            result.Items.Select(ToContract).ToList(),
            new Generated.PaginationMeta(result.CurrentPage, result.ItemsPerPage, result.TotalItems, result.TotalPages));
    }

    public static Generated.CompetitorResponse ToContract(CompetitorDto dto)
    {
        return new Generated.CompetitorResponse(
            dto.Apellido,
            dto.Club,
            dto.CreatedAtUtc,
            dto.Email,
            dto.FechaNacimiento,
            dto.Federacion,
            ToGeneratedCompetitorGender(dto.Genero),
            dto.Id.ToString(),
            ToContract(dto.License),
            dto.Nombre,
            dto.NumeroCamiseta,
            dto.Pais,
            dto.Patrocinadores,
            ToGeneratedCompetitorPostura(dto.Postura),
            dto.SurfScoresCode,
            ToGeneratedCompetitorShirtSize(dto.TallaCamiseta),
            dto.Telefono);
    }

    public static Generated.LicenseInfo ToContract(CompetitorLicenseDto dto)
    {
        return new Generated.LicenseInfo(
            dto.EnabledCategories.ToList(),
            dto.ExpirationDate,
            dto.Number,
            dto.NumberLong,
            ToGeneratedLicenseStatus(dto.Status));
    }

    public static Generated.NotificationPreferencesResponse ToContract(NotificationPreferencesDto dto)
    {
        return new Generated.NotificationPreferencesResponse(
            dto.Email,
            dto.Inscripciones,
            dto.Push,
            dto.Resultados,
            dto.Tokens);
    }

    public static Generated.InscriptionListResponse ToContract(PagedResult<CompetitorInscriptionDto> result)
    {
        return new Generated.InscriptionListResponse(
            result.Items.Select(ToContract).ToList(),
            new Generated.PaginationMeta(result.CurrentPage, result.ItemsPerPage, result.TotalItems, result.TotalPages));
    }

    public static Generated.AdminInscriptionListResponse ToContract(PagedResult<AdminInscriptionRowDto> result)
    {
        return new Generated.AdminInscriptionListResponse(
            result.Items.Select(ToContract).ToList(),
            new Generated.PaginationMeta(result.CurrentPage, result.ItemsPerPage, result.TotalItems, result.TotalPages));
    }

    public static Generated.AdminInscriptionRow ToContract(AdminInscriptionRowDto dto)
    {
        return new Generated.AdminInscriptionRow(
            dto.Categoria,
            dto.Country,
            ToGeneratedInscriptionStatusAdmin(dto.EstadoAdmin),
            dto.Federacion,
            dto.FullName,
            dto.Id.ToString(),
            dto.InscripcionDate,
            dto.LicenciaNumber,
            (float)dto.MontoUsd,
            dto.Notas,
            ToGeneratedPaymentMethod(dto.PaymentMethod),
            dto.Ranking2025,
            dto.Ranking2026,
            dto.SequentialNumber,
            dto.TransaccionId);
    }

    public static Generated.InscriptionResponse ToContract(InscriptionDto dto)
    {
        return new Generated.InscriptionResponse(
            new Generated.Category(dto.Category.Id.ToString(), dto.Category.Nombre),
            new Generated.Circuit(dto.Circuit.Id.ToString(), dto.Circuit.Nombre),
            new Generated.Competitor(dto.Competitor.Country, dto.Competitor.FullName, dto.Competitor.Id.ToString()),
            ToGeneratedInscriptionStatusAdmin(dto.EstadoAdmin),
            ToGeneratedInscriptionStatusCompetitor(dto.EstadoCompetidor),
            new Generated.Event(dto.Event.Id.ToString(), dto.Event.Lugar, dto.Event.Nombre),
            dto.Id.ToString(),
            dto.InscripcionAt,
            (float)dto.MontoUsd,
            ToGeneratedPaymentMethod(dto.PaymentMethod),
            dto.Resultado,
            dto.ShirtNumber,
            dto.TransaccionId);
    }

    public static Generated.InscriptionResponse ToContract(CompetitorInscriptionDto dto)
    {
        return new Generated.InscriptionResponse(
            new Generated.Category(dto.CategoryId, dto.CategoryNombre),
            new Generated.Circuit(dto.CircuitId, dto.CircuitNombre),
            new Generated.Competitor(string.Empty, string.Empty, dto.CompetitorId),
            ToGeneratedInscriptionStatusAdmin(dto.EstadoAdmin),
            ToGeneratedInscriptionStatusCompetitor(dto.EstadoCompetidor),
            new Generated.Event(dto.EventId, dto.EventLugar, dto.EventNombre),
            dto.Id.ToString(),
            dto.InscripcionAt,
            (float)dto.MontoUsd,
            ToGeneratedPaymentMethod(dto.PaymentMethod),
            dto.Resultado,
            dto.ShirtNumber,
            dto.TransaccionId);
    }

    public static Generated.PointsHistoryResponse ToContract(CompetitorPointsHistoryDto dto)
    {
        return new Generated.PointsHistoryResponse(
            dto.Attribution,
            dto.CategoryId,
            dto.CompetitorId.ToString(),
            dto.Data.Select(ToContract).ToList(),
            ToContract(dto.Stats),
            dto.Temporada);
    }

    public static Generated.PointsHistoryEntry ToContract(CompetitorPointsHistoryEntryDto dto)
    {
        return new Generated.PointsHistoryEntry(
            dto.Categoria,
            dto.Cuenta,
            dto.EventoId,
            dto.EventoNombre,
            dto.FechaFin,
            dto.FechaInicio,
            dto.Puesto,
            dto.Puntos,
            dto.Stars,
            dto.Ubicacion);
    }

    public static Generated.Stats ToContract(CompetitorPointsHistoryStatsDto dto)
    {
        return new Generated.Stats(
            dto.EventosDisputados,
            dto.MejorResultado,
            dto.MejorResultadoEvento,
            dto.Posicion,
            dto.PuntosAcumulados,
            dto.TotalEventos);
    }

    public static Generated.Response8 ToContract(IReadOnlyCollection<CompetitorCalendarEventDto> data)
    {
        return new Generated.Response8(data.Select(ToContract).ToList());
    }

    public static Generated.CalendarEventResponse ToContract(CompetitorCalendarEventDto dto)
    {
        return new Generated.CalendarEventResponse(
            dto.Categoria,
            dto.EventId,
            dto.FechaFin,
            dto.FechaInicio,
            ParseInscriptionStatusCompetitor(dto.InscriptionStatus),
            dto.Lugar,
            dto.Nombre,
            dto.Stars);
    }

    public static Generated.PaymentListResponse ToContract(PagedResult<PaymentDto> result)
    {
        return new Generated.PaymentListResponse(
            result.Items.Select(ToContract).ToList(),
            new Generated.PaginationMeta(result.CurrentPage, result.ItemsPerPage, result.TotalItems, result.TotalPages));
    }

    public static Generated.PaymentResponse ToContract(PaymentDto dto)
    {
        return new Generated.PaymentResponse(
            dto.Categoria,
            dto.Competidor,
            dto.CreatedAt,
            ToGeneratedPaymentStatusAdmin(dto.Estado),
            dto.Evento,
            dto.Fecha,
            dto.Id.ToString(),
            ToGeneratedPaymentMethod(dto.Metodo),
            (float)dto.MontoUsd,
            dto.TransaccionId);
    }

    public static Generated.PaymentKpiResponse ToContract(PaymentKpiDto dto)
    {
        return new Generated.PaymentKpiResponse(
            new Generated.MembresiasActivas((double)dto.MembresiasActivas.AmountUsd, dto.MembresiasActivas.Count),
            new Generated.PagoPaypalConfirmados((double)dto.PagoPaypalConfirmados.AmountUsd, dto.PagoPaypalConfirmados.Count),
            new Generated.PagosPlayaValidados((double)dto.PagosPlayaValidados.AmountUsd, dto.PagosPlayaValidados.Count, dto.PagosPlayaValidados.PendingCount),
            dto.TendenciaPercent,
            (float)dto.TotalRecaudadoMes);
    }

    public static Generated.BeachTokenPendingResponse ToContract(BeachTokenPendingDto dto)
    {
        return new Generated.BeachTokenPendingResponse(
            dto.Message,
            dto.RequestId.ToString(),
            Generated.BeachTokenPendingResponseStatus.Pending);
    }

    public static Generated.BeachTokenRedeemResponse ToContract(BeachTokenRedeemResultDto dto)
    {
        return new Generated.BeachTokenRedeemResponse(
            dto.Categoria,
            dto.Evento,
            Generated.BeachTokenRedeemResponseFinancialStatus.Pendiente,
            (double)dto.MontoUsd,
            dto.Reference,
            Generated.BeachTokenRedeemResponseStatus.Success);
    }

    public static Generated.BeachTokenAdminResponse ToContract(BeachTokenAdminDto dto)
    {
        return new Generated.BeachTokenAdminResponse(
            (double)dto.AmountUsd,
            dto.Category,
            dto.CompetitorEmail,
            dto.CompetitorName,
            dto.Event,
            dto.ExpiracionAt,
            dto.GeneradoAt,
            dto.Id.ToString(),
            ToGeneratedTokenHistoryStatus(dto.Status),
            dto.TokenCode,
            dto.UsadoEn);
    }

    public static Generated.BeachTokenAdminListResponse ToContract(BeachTokenAdminListDto dto)
    {
        return new Generated.BeachTokenAdminListResponse(
            new Generated.DailyStats(dto.DailyStats.ApprovedToday, dto.DailyStats.PendingCount, dto.DailyStats.RejectedToday),
            dto.History.Select(ToContract).ToList(),
            new Generated.PaginationMeta(
                dto.Pagination.CurrentPage,
                dto.Pagination.ItemsPerPage,
                dto.Pagination.TotalItems,
                dto.Pagination.TotalPages),
            dto.PendingRequests.Select(ToContract).ToList());
    }

    public static Generated.RankingResponse ToContract(RankingDto dto)
    {
        return new Generated.RankingResponse(
            "Results by SurfScores.com",
            dto.CachedAtUtc,
            dto.CategoryId.ToString(),
            dto.CategoryName,
            dto.Entries.Select(ToContract).ToList(),
            new Generated.PaginationMeta(
                dto.Pagination.CurrentPage,
                dto.Pagination.ItemsPerPage,
                dto.Pagination.TotalItems,
                dto.Pagination.TotalPages),
            dto.Year);
    }

    public static Generated.RankingEntry ToContract(RankingEntryDto dto)
    {
        return new Generated.RankingEntry(dto.Country, dto.Events, dto.Name, dto.Points, dto.Pos, dto.Variation);
    }

    public static Generated.Response9 ToContract(IReadOnlyCollection<RankingCategoryAvailabilityDto> categories)
    {
        return new Generated.Response9(
            categories.Select(x => new Generated.Data(x.AvailableYears.ToList(), x.CategoryId.ToString(), x.CategoryName)).ToList());
    }

    public static Generated.Response10 ToContract(SurfScoresSyncResultDto dto)
    {
        return new Generated.Response10(dto.CircuitCode, dto.RecordsUpdated, dto.SyncedAtUtc);
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

    public static RegisterUserCommand ToCommand(Generated.RegisterRequest request)
    {
        return new RegisterUserCommand(
            request.Email,
            request.Password,
            request.Nombre,
            request.Apellido,
            ToDomainUserType(request.Tipo),
            request.Pais,
            ToDomainPreferredLanguage(request.IdiomaPreferido),
            request.Newsletter,
            request.Terminos,
            request.Reglamento,
            request.FechaNacimiento == default ? null : request.FechaNacimiento,
            ToNullableDomainCompetitorGender(request.Genero),
            request.Telefono,
            request.Club,
            ToNullableDomainCompetitorPostura(request.Postura),
            ToNullableDomainCompetitorShirtSize(request.TallaCamiseta),
            request.Federacion,
            request.Patrocinadores);
    }

    public static LoginUserCommand ToCommand(Generated.LoginRequest request)
    {
        return new LoginUserCommand(request.Email, request.Password, request.RememberMe);
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

    public static CreateCompetitorCommand ToCommand(Generated.CompetitorRequest request)
    {
        return new CreateCompetitorCommand(
            request.Nombre,
            request.Apellido,
            request.Email,
            request.FechaNacimiento,
            ToDomainCompetitorGender(request.Genero),
            request.Pais,
            request.Telefono,
            request.Club,
            ToDomainCompetitorPostura(request.Postura),
            ToDomainCompetitorShirtSize(request.TallaCamiseta),
            request.NumeroCamiseta,
            request.Patrocinadores,
            request.Federacion);
    }

    public static UpdateCompetitorCommand ToCommand(Guid competitorId, Generated.CompetitorRequest request)
    {
        return new UpdateCompetitorCommand(
            competitorId,
            request.Nombre,
            request.Apellido,
            request.Email,
            request.FechaNacimiento,
            ToDomainCompetitorGender(request.Genero),
            request.Pais,
            request.Telefono,
            request.Club,
            ToDomainCompetitorPostura(request.Postura),
            ToDomainCompetitorShirtSize(request.TallaCamiseta),
            request.NumeroCamiseta,
            request.Patrocinadores,
            request.Federacion);
    }

    public static CreateInscriptionCommand ToCommand(Generated.InscriptionRequest request)
    {
        return new CreateInscriptionCommand(
            ParseGuid(request.CompetitorId, "competitorId"),
            ParseGuid(request.EventId, "eventId"),
            ParseGuid(request.CategoryId, "categoryId"),
            NormalizeOptional(request.ShirtNumber),
            ToDomainPaymentMethod(request.PaymentMethod),
            request.Reglamento);
    }

    public static UpdateInscriptionCommand ToCommand(Guid inscriptionId, Generated.InscriptionUpdateRequest request)
    {
        return new UpdateInscriptionCommand(
            inscriptionId,
            NormalizeOptional(request.ShirtNumber),
            ToDomainInscriptionStatusAdmin(request.EstadoAdmin),
            NormalizeOptional(request.Notes));
    }

    public static CreatePaymentCommand ToCommand(Generated.PaymentRequest request)
    {
        return new CreatePaymentCommand(
            ParseGuid(request.InscriptionId, "inscriptionId"),
            ToDomainPaymentMethod(request.Method),
            (decimal)request.AmountUsd,
            request.TransactionId);
    }

    public static UpdatePaymentCommand ToCommand(Guid paymentId, Generated.PaymentUpdateRequest request)
    {
        return new UpdatePaymentCommand(
            paymentId,
            ToDomainPaymentStatusAdmin(request.Status),
            NormalizeOptional(request.Notes));
    }

    public static RequestBeachTokenCommand ToBeachTokenRequestCommand(Generated.BeachTokenRequest request)
    {
        return new RequestBeachTokenCommand(ParseGuid(request.InscriptionId, "inscriptionId"));
    }

    public static RedeemBeachTokenCommand ToBeachTokenRedeemCommand(Generated.BeachTokenRedeemRequest request)
    {
        return new RedeemBeachTokenCommand(
            ParseGuid(request.InscriptionId, "inscriptionId"),
            request.TokenCode);
    }

    public static RejectBeachTokenCommand ToRejectBeachTokenCommand(Guid tokenId, Generated.Body4 request)
    {
        return new RejectBeachTokenCommand(tokenId, request.Reason);
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

    public static LicenseStatus? ParseLicenseStatus(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : NormalizeEnumText(value) switch
            {
                "activa" => LicenseStatus.Activa,
                "pendientedevalidacion" => LicenseStatus.PendienteDeValidacion,
                _ => throw new ValidationException("Estado de licencia invalido.", [new ValidationError("licenseStatus", "Estado de licencia invalido.")])
            };
    }

    public static LicenseStatus ToDomainLicenseStatus(Generated.LicenseStatus value)
    {
        return value switch
        {
            Generated.LicenseStatus.Activa => LicenseStatus.Activa,
            Generated.LicenseStatus.Pendiente_de_validación => LicenseStatus.PendienteDeValidacion,
            _ => throw new ValidationException("Estado de licencia invalido.", [new ValidationError("status", "Estado de licencia invalido.")])
        };
    }

    public static InscriptionStatusAdmin? ParseInscriptionStatusAdmin(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : NormalizeEnumText(value) switch
            {
                "pagado" => InscriptionStatusAdmin.Pagado,
                "pendiente" => InscriptionStatusAdmin.Pendiente,
                _ => throw new ValidationException("Estado de inscripcion invalido.", [new ValidationError("status", "Estado de inscripcion invalido.")])
            };
    }

    public static PaymentStatusAdmin? ParsePaymentStatusAdmin(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : NormalizeEnumText(value) switch
            {
                "confirmado" => PaymentStatusAdmin.Confirmado,
                "pendiente" => PaymentStatusAdmin.Pendiente,
                _ => throw new ValidationException("Estado de pago invalido.", [new ValidationError("status", "Estado de pago invalido.")])
            };
    }

    public static PaymentMethod? ParsePaymentMethod(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : NormalizeEnumText(value) switch
            {
                "paypal" => PaymentMethod.Paypal,
                "beach" => PaymentMethod.Beach,
                _ => throw new ValidationException("Metodo de pago invalido.", [new ValidationError("method", "Metodo de pago invalido.")])
            };
    }

    public static TokenHistoryStatus? ParseTokenHistoryStatus(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : NormalizeEnumText(value) switch
            {
                "usado" => TokenHistoryStatus.Usado,
                "expirado" => TokenHistoryStatus.Expirado,
                "rechazado" => TokenHistoryStatus.Rechazado,
                "pendiente" => TokenHistoryStatus.Pendiente,
                _ => throw new ValidationException("Estado de token invalido.", [new ValidationError("status", "Estado de token invalido.")])
            };
    }

    public static Guid? ParseOptionalGuid(string? value, string field)
    {
        return string.IsNullOrWhiteSpace(value) ? null : ParseGuid(value, field);
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

    public static CompetitorGender ToDomainCompetitorGender(Generated.CompetitorRequestGenero value)
    {
        return value switch
        {
            Generated.CompetitorRequestGenero.Masculino => CompetitorGender.Masculino,
            Generated.CompetitorRequestGenero.Femenino => CompetitorGender.Femenino,
            Generated.CompetitorRequestGenero.Prefiero_no_indicar => CompetitorGender.PrefieroNoIndicar,
            _ => throw new ValidationException("Genero de competidor invalido.", [new ValidationError("genero", "Genero de competidor invalido.")])
        };
    }

    public static CompetitorPostura ToDomainCompetitorPostura(Generated.Postura value)
    {
        return value switch
        {
            Generated.Postura.Regular => CompetitorPostura.Regular,
            Generated.Postura.Goofy => CompetitorPostura.Goofy,
            _ => throw new ValidationException("Postura invalida.", [new ValidationError("postura", "Postura invalida.")])
        };
    }

    public static CompetitorShirtSize ToDomainCompetitorShirtSize(Generated.ShirtSize value)
    {
        return value switch
        {
            Generated.ShirtSize.XS => CompetitorShirtSize.XS,
            Generated.ShirtSize.S => CompetitorShirtSize.S,
            Generated.ShirtSize.M => CompetitorShirtSize.M,
            Generated.ShirtSize.L => CompetitorShirtSize.L,
            Generated.ShirtSize.XL => CompetitorShirtSize.XL,
            Generated.ShirtSize.XXL => CompetitorShirtSize.XXL,
            _ => throw new ValidationException("Talla de camiseta invalida.", [new ValidationError("tallaCamiseta", "Talla de camiseta invalida.")])
        };
    }

    public static PaymentMethod ToDomainPaymentMethod(Generated.PaymentMethodEnum value)
    {
        return value switch
        {
            Generated.PaymentMethodEnum.Paypal => PaymentMethod.Paypal,
            Generated.PaymentMethodEnum.Beach => PaymentMethod.Beach,
            _ => throw new ValidationException("Metodo de pago invalido.", [new ValidationError("paymentMethod", "Metodo de pago invalido.")])
        };
    }

    public static InscriptionStatusAdmin ToDomainInscriptionStatusAdmin(Generated.InscriptionStatusAdmin value)
    {
        return value switch
        {
            Generated.InscriptionStatusAdmin.Pagado => InscriptionStatusAdmin.Pagado,
            Generated.InscriptionStatusAdmin.Pendiente => InscriptionStatusAdmin.Pendiente,
            _ => throw new ValidationException("Estado admin de la inscripcion invalido.", [new ValidationError("estadoAdmin", "Estado admin de la inscripcion invalido.")])
        };
    }

    public static PaymentStatusAdmin ToDomainPaymentStatusAdmin(Generated.PaymentStatusAdmin value)
    {
        return value switch
        {
            Generated.PaymentStatusAdmin.Confirmado => PaymentStatusAdmin.Confirmado,
            Generated.PaymentStatusAdmin.Pendiente => PaymentStatusAdmin.Pendiente,
            _ => throw new ValidationException("Estado del pago invalido.", [new ValidationError("status", "Estado del pago invalido.")])
        };
    }

    public static UserType ToDomainUserType(Generated.UserType value)
    {
        return value switch
        {
            Generated.UserType.Espectador => UserType.Espectador,
            Generated.UserType.Competidor => UserType.Competidor,
            _ => throw new ValidationException("Tipo de usuario inválido.", [new ValidationError("tipo", "Tipo de usuario inválido.")])
        };
    }

    public static PreferredLanguage ToDomainPreferredLanguage(Generated.PreferredLanguage value)
    {
        return value switch
        {
            Generated.PreferredLanguage.Español => PreferredLanguage.Espanol,
            Generated.PreferredLanguage.Português => PreferredLanguage.Portugues,
            Generated.PreferredLanguage.English => PreferredLanguage.English,
            _ => throw new ValidationException("Idioma preferido inválido.", [new ValidationError("idiomaPreferido", "Idioma preferido inválido.")])
        };
    }

    private static CompetitorGender? ToNullableDomainCompetitorGender(Generated.CategoryGender value)
    {
        return value switch
        {
            Generated.CategoryGender.Masculino => CompetitorGender.Masculino,
            Generated.CategoryGender.Femenino => CompetitorGender.Femenino,
            Generated.CategoryGender.Ambos => null,
            _ => null
        };
    }

    private static CompetitorPostura? ToNullableDomainCompetitorPostura(Generated.Postura value)
    {
        return value switch
        {
            Generated.Postura.Regular => CompetitorPostura.Regular,
            Generated.Postura.Goofy => CompetitorPostura.Goofy,
            _ => null
        };
    }

    private static CompetitorShirtSize? ToNullableDomainCompetitorShirtSize(Generated.ShirtSize value)
    {
        return value switch
        {
            Generated.ShirtSize.XS => CompetitorShirtSize.XS,
            Generated.ShirtSize.S => CompetitorShirtSize.S,
            Generated.ShirtSize.M => CompetitorShirtSize.M,
            Generated.ShirtSize.L => CompetitorShirtSize.L,
            Generated.ShirtSize.XL => CompetitorShirtSize.XL,
            Generated.ShirtSize.XXL => CompetitorShirtSize.XXL,
            _ => null
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

    private static Generated.CompetitorRequestGenero ToGeneratedCompetitorGender(CompetitorGender value)
    {
        return value switch
        {
            CompetitorGender.Masculino => Generated.CompetitorRequestGenero.Masculino,
            CompetitorGender.Femenino => Generated.CompetitorRequestGenero.Femenino,
            CompetitorGender.PrefieroNoIndicar => Generated.CompetitorRequestGenero.Prefiero_no_indicar,
            _ => Generated.CompetitorRequestGenero.Prefiero_no_indicar
        };
    }

    private static Generated.Postura ToGeneratedCompetitorPostura(CompetitorPostura value)
    {
        return value switch
        {
            CompetitorPostura.Regular => Generated.Postura.Regular,
            CompetitorPostura.Goofy => Generated.Postura.Goofy,
            _ => Generated.Postura.Regular
        };
    }

    private static Generated.ShirtSize ToGeneratedCompetitorShirtSize(CompetitorShirtSize value)
    {
        return value switch
        {
            CompetitorShirtSize.XS => Generated.ShirtSize.XS,
            CompetitorShirtSize.S => Generated.ShirtSize.S,
            CompetitorShirtSize.M => Generated.ShirtSize.M,
            CompetitorShirtSize.L => Generated.ShirtSize.L,
            CompetitorShirtSize.XL => Generated.ShirtSize.XL,
            CompetitorShirtSize.XXL => Generated.ShirtSize.XXL,
            _ => Generated.ShirtSize.M
        };
    }

    private static Generated.LicenseStatus ToGeneratedLicenseStatus(LicenseStatus value)
    {
        return value switch
        {
            LicenseStatus.Activa => Generated.LicenseStatus.Activa,
            LicenseStatus.PendienteDeValidacion => Generated.LicenseStatus.Pendiente_de_validación,
            _ => Generated.LicenseStatus.Pendiente_de_validación
        };
    }

    private static Generated.PaymentMethodEnum ToGeneratedPaymentMethod(PaymentMethod value)
    {
        return value switch
        {
            PaymentMethod.Paypal => Generated.PaymentMethodEnum.Paypal,
            PaymentMethod.Beach => Generated.PaymentMethodEnum.Beach,
            _ => Generated.PaymentMethodEnum.Paypal
        };
    }

    private static Generated.PaymentStatusAdmin ToGeneratedPaymentStatusAdmin(PaymentStatusAdmin value)
    {
        return value switch
        {
            PaymentStatusAdmin.Confirmado => Generated.PaymentStatusAdmin.Confirmado,
            PaymentStatusAdmin.Pendiente => Generated.PaymentStatusAdmin.Pendiente,
            _ => Generated.PaymentStatusAdmin.Pendiente
        };
    }

    private static Generated.TokenHistoryStatus ToGeneratedTokenHistoryStatus(TokenHistoryStatus value)
    {
        return value switch
        {
            TokenHistoryStatus.Usado => Generated.TokenHistoryStatus.Usado,
            TokenHistoryStatus.Expirado => Generated.TokenHistoryStatus.Expirado,
            TokenHistoryStatus.Rechazado => Generated.TokenHistoryStatus.Rechazado,
            TokenHistoryStatus.Pendiente => Generated.TokenHistoryStatus.Pendiente,
            _ => Generated.TokenHistoryStatus.Pendiente
        };
    }

    private static Generated.InscriptionStatusAdmin ToGeneratedInscriptionStatusAdmin(InscriptionStatusAdmin value)
    {
        return value switch
        {
            InscriptionStatusAdmin.Pagado => Generated.InscriptionStatusAdmin.Pagado,
            InscriptionStatusAdmin.Pendiente => Generated.InscriptionStatusAdmin.Pendiente,
            _ => Generated.InscriptionStatusAdmin.Pendiente
        };
    }

    private static Generated.InscriptionStatusCompetitor ToGeneratedInscriptionStatusCompetitor(InscriptionStatusCompetitor value)
    {
        return value switch
        {
            InscriptionStatusCompetitor.Confirmado => Generated.InscriptionStatusCompetitor.Confirmado,
            InscriptionStatusCompetitor.Pendiente => Generated.InscriptionStatusCompetitor.Pendiente,
            InscriptionStatusCompetitor.Completado => Generated.InscriptionStatusCompetitor.Completado,
            _ => Generated.InscriptionStatusCompetitor.Pendiente
        };
    }

    private static Generated.UserType ToGeneratedUserType(UserType value)
    {
        return value switch
        {
            UserType.Espectador => Generated.UserType.Espectador,
            UserType.Competidor => Generated.UserType.Competidor,
            _ => Generated.UserType.Espectador
        };
    }

    private static Generated.AdminRole ToGeneratedAdminRole(AdminRole value)
    {
        return value switch
        {
            AdminRole.SuperAdmin => Generated.AdminRole.Super_Admin,
            AdminRole.Admin => Generated.AdminRole.Admin,
            AdminRole.Arbitro => Generated.AdminRole.Árbitro,
            AdminRole.Revisor => Generated.AdminRole.Revisor,
            _ => Generated.AdminRole.Admin
        };
    }

    private static Generated.InscriptionStatusCompetitor ParseInscriptionStatusCompetitor(string value)
    {
        return NormalizeEnumText(value) switch
        {
            "confirmado" => Generated.InscriptionStatusCompetitor.Confirmado,
            "pendiente" => Generated.InscriptionStatusCompetitor.Pendiente,
            "completado" => Generated.InscriptionStatusCompetitor.Completado,
            _ => Generated.InscriptionStatusCompetitor.Pendiente
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
