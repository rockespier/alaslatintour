using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Auth.Commands.ConfirmPasswordReset;
using AlasApp.Application.Auth.Commands.LoginUser;
using AlasApp.Application.Auth.Commands.LogoutUser;
using AlasApp.Application.Auth.Commands.RegisterUser;
using AlasApp.Application.Auth.Commands.RequestPasswordReset;
using AlasApp.Application.Categories.Commands.CreateCategory;
using AlasApp.Application.Categories.Commands.DeleteCategory;
using AlasApp.Application.Categories.Commands.UpdateCategory;
using AlasApp.Application.Categories.Queries.GetCategoryById;
using AlasApp.Application.Categories.Queries.ListCategories;
using AlasApp.Application.CategoryTariffs.Commands.UpsertCategoryTariff;
using AlasApp.Application.CategoryTariffs.Queries.GetCategoryTariffs;
using AlasApp.Application.Competitors.Commands.CreateCompetitor;
using AlasApp.Application.Competitors.Commands.DeleteCompetitor;
using AlasApp.Application.Competitors.Commands.UpdateCompetitorLicense;
using AlasApp.Application.Competitors.Commands.UpdateCompetitorNotifications;
using AlasApp.Application.Competitors.Commands.UpdateCompetitor;
using AlasApp.Application.Competitors.Queries.GetCompetitorCalendar;
using AlasApp.Application.Competitors.Queries.GetCompetitorById;
using AlasApp.Application.Competitors.Queries.GetCompetitorInscriptions;
using AlasApp.Application.Competitors.Queries.GetCompetitorNotifications;
using AlasApp.Application.Competitors.Queries.GetCompetitorPointsHistory;
using AlasApp.Application.Competitors.Queries.ListCompetitors;
using AlasApp.Application.Circuits.Commands.CreateCircuit;
using AlasApp.Application.Circuits.Commands.DeleteCircuit;
using AlasApp.Application.Circuits.Commands.UpdateCircuit;
using AlasApp.Application.Circuits.Queries.GetCircuitById;
using AlasApp.Application.Circuits.Queries.ListCircuits;
using AlasApp.Application.EventCategories.Commands.UpdateEventCategories;
using AlasApp.Application.EventCategories.Queries.GetEventCategories;
using AlasApp.Application.Events.Commands.CreateEvent;
using AlasApp.Application.Events.Commands.DeleteEvent;
using AlasApp.Application.Events.Commands.UpdateEvent;
using AlasApp.Application.Events.Queries.GetEventById;
using AlasApp.Application.Events.Queries.ListEvents;
using AlasApp.Application.Inscriptions.Commands.CreateInscription;
using AlasApp.Application.Inscriptions.Commands.DeleteInscription;
using AlasApp.Application.Inscriptions.Commands.UpdateInscription;
using AlasApp.Application.Inscriptions.Queries.GetInscriptionById;
using AlasApp.Application.Inscriptions.Queries.ListInscriptions;
using AlasApp.Application.Payments.Commands.ApproveBeachToken;
using AlasApp.Application.Payments.Commands.CreatePayment;
using AlasApp.Application.Payments.Commands.RejectBeachToken;
using AlasApp.Application.Payments.Commands.RequestBeachToken;
using AlasApp.Application.Payments.Commands.RedeemBeachToken;
using AlasApp.Application.Payments.Commands.UpdatePayment;
using AlasApp.Application.Payments.Queries.GetPaymentById;
using AlasApp.Application.Payments.Queries.GetPaymentKpis;
using AlasApp.Application.Payments.Queries.ListBeachTokens;
using AlasApp.Application.Payments.Queries.ListPayments;
using AlasApp.Application.Rankings.Commands.SyncSurfScoresCircuit;
using AlasApp.Application.Rankings.Queries.GetRanking;
using AlasApp.Application.Rankings.Queries.ListRankingCategories;
using Microsoft.Extensions.DependencyInjection;

namespace AlasApp.Application.Common;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IClock, SystemClock>();
        services.AddScoped<IRequestDispatcher, RequestDispatcher>();
        services.AddScoped<IRequestHandler<RegisterUserCommand, Auth.Models.RegisterResultDto>, RegisterUserCommandHandler>();
        services.AddScoped<IRequestHandler<LoginUserCommand, Auth.Models.LoginResultDto>, LoginUserCommandHandler>();
        services.AddScoped<IRequestHandler<RequestPasswordResetCommand, bool>, RequestPasswordResetCommandHandler>();
        services.AddScoped<IRequestHandler<ConfirmPasswordResetCommand, bool>, ConfirmPasswordResetCommandHandler>();
        services.AddScoped<IRequestHandler<LogoutUserCommand, bool>, LogoutUserCommandHandler>();

        services.AddScoped<IRequestHandler<ListCircuitsQuery, PagedResult<Circuits.Models.CircuitDto>>, ListCircuitsQueryHandler>();
        services.AddScoped<IRequestHandler<GetCircuitByIdQuery, Circuits.Models.CircuitDto>, GetCircuitByIdQueryHandler>();
        services.AddScoped<IRequestHandler<CreateCircuitCommand, Circuits.Models.CircuitDto>, CreateCircuitCommandHandler>();
        services.AddScoped<IRequestHandler<UpdateCircuitCommand, Circuits.Models.CircuitDto>, UpdateCircuitCommandHandler>();
        services.AddScoped<IRequestHandler<DeleteCircuitCommand, bool>, DeleteCircuitCommandHandler>();

        services.AddScoped<IRequestHandler<ListEventsQuery, PagedResult<Events.Models.EventDto>>, ListEventsQueryHandler>();
        services.AddScoped<IRequestHandler<GetEventByIdQuery, Events.Models.EventDto>, GetEventByIdQueryHandler>();
        services.AddScoped<IRequestHandler<CreateEventCommand, Events.Models.EventDto>, CreateEventCommandHandler>();
        services.AddScoped<IRequestHandler<UpdateEventCommand, Events.Models.EventDto>, UpdateEventCommandHandler>();
        services.AddScoped<IRequestHandler<DeleteEventCommand, bool>, DeleteEventCommandHandler>();

        services.AddScoped<IRequestHandler<ListCategoriesQuery, IReadOnlyCollection<Categories.Models.CategoryDto>>, ListCategoriesQueryHandler>();
        services.AddScoped<IRequestHandler<GetCategoryByIdQuery, Categories.Models.CategoryDto>, GetCategoryByIdQueryHandler>();
        services.AddScoped<IRequestHandler<CreateCategoryCommand, Categories.Models.CategoryDto>, CreateCategoryCommandHandler>();
        services.AddScoped<IRequestHandler<UpdateCategoryCommand, Categories.Models.CategoryDto>, UpdateCategoryCommandHandler>();
        services.AddScoped<IRequestHandler<DeleteCategoryCommand, bool>, DeleteCategoryCommandHandler>();
        services.AddScoped<IRequestHandler<GetCategoryTariffsQuery, IReadOnlyCollection<CategoryTariffs.Models.CategoryTariffDto>>, GetCategoryTariffsQueryHandler>();
        services.AddScoped<IRequestHandler<UpsertCategoryTariffCommand, CategoryTariffs.Models.CategoryTariffDto>, UpsertCategoryTariffCommandHandler>();

        services.AddScoped<IRequestHandler<GetEventCategoriesQuery, EventCategories.Models.EventCategoryListDto>, GetEventCategoriesQueryHandler>();
        services.AddScoped<IRequestHandler<UpdateEventCategoriesCommand, EventCategories.Models.EventCategoryListDto>, UpdateEventCategoriesCommandHandler>();
        services.AddScoped<IRequestHandler<ListCompetitorsQuery, PagedResult<Competitors.Models.CompetitorDto>>, ListCompetitorsQueryHandler>();
        services.AddScoped<IRequestHandler<GetCompetitorByIdQuery, Competitors.Models.CompetitorDto>, GetCompetitorByIdQueryHandler>();
        services.AddScoped<IRequestHandler<CreateCompetitorCommand, Competitors.Models.CompetitorDto>, CreateCompetitorCommandHandler>();
        services.AddScoped<IRequestHandler<UpdateCompetitorCommand, Competitors.Models.CompetitorDto>, UpdateCompetitorCommandHandler>();
        services.AddScoped<IRequestHandler<DeleteCompetitorCommand, bool>, DeleteCompetitorCommandHandler>();
        services.AddScoped<IRequestHandler<UpdateCompetitorLicenseCommand, Competitors.Models.CompetitorDto>, UpdateCompetitorLicenseCommandHandler>();
        services.AddScoped<IRequestHandler<GetCompetitorNotificationsQuery, Competitors.Models.NotificationPreferencesDto>, GetCompetitorNotificationsQueryHandler>();
        services.AddScoped<IRequestHandler<UpdateCompetitorNotificationsCommand, Competitors.Models.NotificationPreferencesDto>, UpdateCompetitorNotificationsCommandHandler>();
        services.AddScoped<IRequestHandler<GetCompetitorInscriptionsQuery, PagedResult<Competitors.Models.CompetitorInscriptionDto>>, GetCompetitorInscriptionsQueryHandler>();
        services.AddScoped<IRequestHandler<GetCompetitorPointsHistoryQuery, Competitors.Models.CompetitorPointsHistoryDto>, GetCompetitorPointsHistoryQueryHandler>();
        services.AddScoped<IRequestHandler<GetCompetitorCalendarQuery, IReadOnlyCollection<Competitors.Models.CompetitorCalendarEventDto>>, GetCompetitorCalendarQueryHandler>();
        services.AddScoped<IRequestHandler<ListInscriptionsQuery, PagedResult<Inscriptions.Models.AdminInscriptionRowDto>>, ListInscriptionsQueryHandler>();
        services.AddScoped<IRequestHandler<GetInscriptionByIdQuery, Inscriptions.Models.InscriptionDto>, GetInscriptionByIdQueryHandler>();
        services.AddScoped<IRequestHandler<CreateInscriptionCommand, Inscriptions.Models.InscriptionDto>, CreateInscriptionCommandHandler>();
        services.AddScoped<IRequestHandler<UpdateInscriptionCommand, Inscriptions.Models.InscriptionDto>, UpdateInscriptionCommandHandler>();
        services.AddScoped<IRequestHandler<DeleteInscriptionCommand, bool>, DeleteInscriptionCommandHandler>();
        services.AddScoped<IRequestHandler<ListPaymentsQuery, PagedResult<Payments.Models.PaymentDto>>, ListPaymentsQueryHandler>();
        services.AddScoped<IRequestHandler<GetPaymentByIdQuery, Payments.Models.PaymentDto>, GetPaymentByIdQueryHandler>();
        services.AddScoped<IRequestHandler<CreatePaymentCommand, Payments.Models.PaymentDto>, CreatePaymentCommandHandler>();
        services.AddScoped<IRequestHandler<UpdatePaymentCommand, Payments.Models.PaymentDto>, UpdatePaymentCommandHandler>();
        services.AddScoped<IRequestHandler<GetPaymentKpisQuery, Payments.Models.PaymentKpiDto>, GetPaymentKpisQueryHandler>();
        services.AddScoped<IRequestHandler<RequestBeachTokenCommand, Payments.Models.BeachTokenPendingDto>, RequestBeachTokenCommandHandler>();
        services.AddScoped<IRequestHandler<ApproveBeachTokenCommand, Payments.Models.BeachTokenAdminDto>, ApproveBeachTokenCommandHandler>();
        services.AddScoped<IRequestHandler<RejectBeachTokenCommand, Payments.Models.BeachTokenAdminDto>, RejectBeachTokenCommandHandler>();
        services.AddScoped<IRequestHandler<RedeemBeachTokenCommand, Payments.Models.BeachTokenRedeemResultDto>, RedeemBeachTokenCommandHandler>();
        services.AddScoped<IRequestHandler<ListBeachTokensQuery, Payments.Models.BeachTokenAdminListDto>, ListBeachTokensQueryHandler>();
        services.AddScoped<IRequestHandler<GetRankingQuery, Rankings.Models.RankingDto>, GetRankingQueryHandler>();
        services.AddScoped<IRequestHandler<ListRankingCategoriesQuery, IReadOnlyCollection<Rankings.Models.RankingCategoryAvailabilityDto>>, ListRankingCategoriesQueryHandler>();
        services.AddScoped<IRequestHandler<SyncSurfScoresCircuitCommand, Rankings.Models.SurfScoresSyncResultDto>, SyncSurfScoresCircuitCommandHandler>();

        return services;
    }
}
