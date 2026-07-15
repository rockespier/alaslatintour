using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.SurfScoresImport.Models;

namespace AlasApp.Application.SurfScoresImport.Commands.ImportSurfScoresEvents;

public sealed record ImportSurfScoresEventsCommand(Guid CircuitId) : IRequest<SurfScoresImportResultDto>;
