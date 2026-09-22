using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.BulkImports.Models;

namespace AlasApp.Application.Payments.Commands.ImportMembershipPayments;

public sealed record ImportMembershipPaymentsCommand(Guid EventId, byte[] FileContent) : IRequest<BulkImportResultDto>;
