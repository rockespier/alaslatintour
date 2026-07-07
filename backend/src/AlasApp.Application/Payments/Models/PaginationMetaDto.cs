namespace AlasApp.Application.Payments.Models;

public sealed record PaginationMetaDto(
    int CurrentPage,
    int TotalPages,
    int TotalItems,
    int ItemsPerPage);
