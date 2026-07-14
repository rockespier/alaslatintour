using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.AdminSettings.Models;
using AlasApp.Application.Common;

namespace AlasApp.Application.AdminSettings.Commands.TestIntegration;

public sealed class TestIntegrationCommandHandler(
    IAdminSettingsRepository repository,
    IClock clock)
    : IRequestHandler<TestIntegrationCommand, IntegrationTestResultDto>
{
    public async Task<IntegrationTestResultDto> Handle(TestIntegrationCommand request, CancellationToken cancellationToken)
    {
        var provider = request.Provider.Trim().ToLowerInvariant();
        var json = await repository.GetJsonAsync(AdminSettingsDefaults.SettingsKey, cancellationToken);
        var settings = AdminSettingsSerializer.DeserializeOrDefault(json);

        return provider switch
        {
            "surfscores" => Test(
                "surfscores",
                settings.Integrations.SurfScores.Endpoint,
                "SurfScores esta configurado para pruebas de conexion."),
            "wordpress" => Test(
                "wordpress",
                settings.Integrations.WordPress.Endpoint,
                "WordPress esta configurado para pruebas de conexion."),
            _ => throw new ValidationException(
                "Proveedor de integracion no soportado.",
                [new ValidationError("provider", "Los proveedores validos son surfscores o wordpress.")])
        };
    }

    private IntegrationTestResultDto Test(string provider, string endpoint, string successMessage)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return new IntegrationTestResultDto(
                provider,
                "missing_configuration",
                "Falta configurar el endpoint de la integracion.",
                clock.UtcNow);
        }

        return new IntegrationTestResultDto(provider, "connected", successMessage, clock.UtcNow);
    }
}
