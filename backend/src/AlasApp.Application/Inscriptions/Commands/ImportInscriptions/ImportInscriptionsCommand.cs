using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.BulkImports.Models;

namespace AlasApp.Application.Inscriptions.Commands.ImportInscriptions;

public sealed record ImportInscriptionsCommand(
    Guid EventId,
    Guid CategoryId,
    byte[] FileContent) : IRequest<BulkImportResultDto>;
