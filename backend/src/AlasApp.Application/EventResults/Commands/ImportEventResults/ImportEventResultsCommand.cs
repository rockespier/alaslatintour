using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.BulkImports.Models;

namespace AlasApp.Application.EventResults.Commands.ImportEventResults;

public sealed record ImportEventResultsCommand(Guid EventId, Guid CategoryId, byte[] FileContent) : IRequest<BulkImportResultDto>;
