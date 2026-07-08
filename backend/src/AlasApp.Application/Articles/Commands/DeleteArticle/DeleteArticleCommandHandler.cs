using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Common;

namespace AlasApp.Application.Articles.Commands.DeleteArticle;

public sealed class DeleteArticleCommandHandler(IWordPressService wordPressService)
    : IRequestHandler<DeleteArticleCommand, bool>
{
    public async Task<bool> Handle(DeleteArticleCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Slug))
        {
            throw new ValidationException("La solicitud contiene errores de validacion.", [new ValidationError("slug", "El slug es obligatorio.")]);
        }

        var deleted = await wordPressService.DeleteAsync(request.Slug.Trim(), cancellationToken);
        if (!deleted)
        {
            throw new NotFoundException("No se encontró el artículo solicitado.");
        }

        return true;
    }
}
