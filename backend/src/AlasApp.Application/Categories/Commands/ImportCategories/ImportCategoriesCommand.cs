using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.BulkImports.Models;

namespace AlasApp.Application.Categories.Commands.ImportCategories;

public sealed record ImportCategoriesCommand(byte[] FileContent) : IRequest<BulkImportResultDto>;
