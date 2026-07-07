using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Common;
using AlasApp.Application.Competitors.Models;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Application.Competitors.Commands.UpdateCompetitorLicense;

public sealed class UpdateCompetitorLicenseCommandHandler(
    ICompetitorRepository competitorRepository,
    IUnitOfWork unitOfWork,
    IClock clock)
    : IRequestHandler<UpdateCompetitorLicenseCommand, CompetitorDto>
{
    public async Task<CompetitorDto> Handle(UpdateCompetitorLicenseCommand request, CancellationToken cancellationToken)
    {
        var competitor = await competitorRepository.GetEntityByIdAsync(request.CompetitorId, cancellationToken)
            ?? throw new NotFoundException("Competidor no encontrado.");

        try
        {
            competitor.UpdateLicense(
                request.Status,
                request.LicenseNumber,
                BuildLongLicenseNumber(request.LicenseNumber, request.ExpirationDate),
                request.ExpirationDate,
                request.EnabledCategories);

            competitor.SetUpdated(clock.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return await competitorRepository.GetByIdAsync(competitor.Id, cancellationToken)
                ?? throw new NotFoundException("Competidor no encontrado despues de actualizar la licencia.");
        }
        catch (DomainRuleException exception)
        {
            throw new ValidationException(exception.Message, [new ValidationError("body", exception.Message)]);
        }
    }

    private static string BuildLongLicenseNumber(string licenseNumber, DateTimeOffset expirationDate)
    {
        if (string.IsNullOrWhiteSpace(licenseNumber))
        {
            return string.Empty;
        }

        return $"{licenseNumber.Trim()}-{expirationDate.Year}";
    }
}
