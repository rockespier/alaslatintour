using AlasApp.Application.Articles.Models;
using AlasApp.Application.Common;
using AlasApp.Application.Uploads.Models;

namespace AlasApp.Application.Abstractions.Services;

public interface IWordPressService
{
    Task<PagedResult<ArticleSummaryDto>> ListArticlesAsync(ArticleListFilter filter, CancellationToken cancellationToken);

    Task<ArticleDetailDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken);

    Task<ArticleDetailDto> CreateAsync(ArticleUpsertDto article, CancellationToken cancellationToken);

    Task<ArticleDetailDto> UpdateAsync(string slug, ArticleUpsertDto article, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(string slug, CancellationToken cancellationToken);

    Task<UploadedMediaDto> UploadMediaAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken);
}
