using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.BulkImports.Models;

namespace AlasApp.Application.Circuits.Commands.ImportCircuits;

public sealed record ImportCircuitsCommand(byte[] FileContent) : IRequest<BulkImportResultDto>;
