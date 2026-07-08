using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Articles.Models;
using AlasApp.Application.Common;

namespace AlasApp.Application.Articles.Commands.CreateArticle;

public sealed class CreateArticleCommandHandler(IWordPressService wordPressService)
    : IRequestHandler<CreateArticleCommand, ArticleDetailDto>
{
    public Task<ArticleDetailDto> Handle(CreateArticleCommand request, CancellationToken cancellationToken)
    {
        Validate(request.Titulo, request.Resumen, request.ContentHtml);

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

        return wordPressService.CreateAsync(dto, cancellationToken);
    }

    private static void Validate(string titulo, string resumen, string contentHtml)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(titulo))
        {
            errors.Add(new ValidationError("titulo", "El título es obligatorio."));
        }

        if (string.IsNullOrWhiteSpace(resumen))
        {
            errors.Add(new ValidationError("resumen", "El resumen es obligatorio."));
        }

        if (string.IsNullOrWhiteSpace(contentHtml))
        {
            errors.Add(new ValidationError("content", "El contenido es obligatorio."));
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("La solicitud contiene errores de validacion.", errors);
        }
    }
}
