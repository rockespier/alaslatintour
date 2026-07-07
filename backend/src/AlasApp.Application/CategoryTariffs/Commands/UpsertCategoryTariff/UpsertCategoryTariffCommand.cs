using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.CategoryTariffs.Models;

namespace AlasApp.Application.CategoryTariffs.Commands.UpsertCategoryTariff;

public sealed record UpsertCategoryTariffCommand(
    Guid CategoryId,
    int StarLevel,
    decimal Usd,
    decimal Cop,
    bool Active) : IRequest<CategoryTariffDto>;
