using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.BulkImports.Models;

namespace AlasApp.Application.Events.Commands.ImportEvents;

public sealed record ImportEventsCommand(byte[] FileContent) : IRequest<BulkImportResultDto>;
