using AlasApp.Application.Pages.Models;

namespace AlasApp.Application.Abstractions.Services;

public interface IPageService
{
    /// <summary>
    /// Page by its Spanish slug (e.g. `quienes-somos`), in the requested language when WordPress
    /// (Polylang) has that translation. Null when the page doesn't exist or WordPress is unreachable.
    /// </summary>
    Task<PageContentDto?> GetBySlugAsync(string slug, string? lang, CancellationToken cancellationToken);
}
