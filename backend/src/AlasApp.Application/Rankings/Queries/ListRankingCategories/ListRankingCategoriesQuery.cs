using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Rankings.Models;

namespace AlasApp.Application.Rankings.Queries.ListRankingCategories;

public sealed record ListRankingCategoriesQuery : IRequest<IReadOnlyCollection<RankingCategoryAvailabilityDto>>;
