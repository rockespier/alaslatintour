using AlasApp.Domain.Enums;

namespace AlasApp.Application.Categories.Models;

public sealed record CategoryListFilter(CategoryStatus? Status);
