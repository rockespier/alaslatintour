namespace AlasApp.Application.EventCategories.Models;

public sealed record EventCategoryListDto(bool UseCircuitTariffs, IReadOnlyCollection<EventCategoryDto> Data);
