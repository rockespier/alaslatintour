using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.AdminSettings.Models;

namespace AlasApp.Application.AdminSettings.Commands.TestIntegration;

public sealed record TestIntegrationCommand(string Provider) : IRequest<IntegrationTestResultDto>;
