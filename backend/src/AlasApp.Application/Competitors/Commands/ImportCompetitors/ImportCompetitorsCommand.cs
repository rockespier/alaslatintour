using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.BulkImports.Models;

namespace AlasApp.Application.Competitors.Commands.ImportCompetitors;

public sealed record ImportCompetitorsCommand(byte[] FileContent) : IRequest<BulkImportResultDto>;
