namespace AlasApp.Application.Common;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int CurrentPage, int ItemsPerPage, int TotalItems)
{
    public int TotalPages => ItemsPerPage == 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)ItemsPerPage);
}
