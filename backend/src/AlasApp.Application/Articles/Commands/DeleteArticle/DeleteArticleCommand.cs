using AlasApp.Application.Abstractions.Messaging;

namespace AlasApp.Application.Articles.Commands.DeleteArticle;

public sealed record DeleteArticleCommand(string Slug) : IRequest<bool>;
