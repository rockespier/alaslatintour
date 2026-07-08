using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Articles.Models;
using AlasApp.Application.Common;

namespace AlasApp.Application.Articles.Commands.UpdateArticle;

public sealed class UpdateArticleCommandHandler(IWordPressService wordPressService)
    : IRequestHandler<UpdateArticleCommand, ArticleDetailDto>
{
    public Task<ArticleDetailDto> Handle(UpdateArticleCommand request, CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(request.Slug))
        {
            errors.Add(new ValidationError("slug", "El slug es obligatorio."));
        }

        if (string.IsNullOrWhiteSpace(request.Titulo))
        {
            errors.Add(new ValidationError("titulo", "El título es obligatorio."));
        }

        if (string.IsNullOrWhiteSpace(request.Resumen))
        {
            errors.Add(new ValidationError("resumen", "El resumen es obligatorio."));
        }

        if (string.IsNullOrWhiteSpace(request.ContentHtml))
        {
            errors.Add(new ValidationError("content", "El contenido es obligatorio."));
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("La solicitud contiene errores de validacion.", errors);
        }

        var dto = new ArticleUpsertDto(
            request.Titulo.Trim(),
            request.Resumen.Trim(),
            request.ContentHtml.Trim(),
            request.Categoria,
            request.Autor.Trim(),
            request.AutorTitulo.Trim(),
            request.ImagenUrl.Trim(),
            request.Tags.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            request.Featured,
            request.ShowRankingWidget,
            string.IsNullOrWhiteSpace(request.RelatedEventId) ? null : request.RelatedEventId.Trim());

        return wordPressService.UpdateAsync(request.Slug.Trim(), dto, cancellationToken);
    }
}
