using AlasApp.Application.AdminSettings.Models;
using AlasApp.Application.Common;

namespace AlasApp.Application.AdminSettings;

public static class AdminSettingsValidator
{
    public static void Validate(AdminSettingsDto settings)
    {
        var errors = new List<ValidationError>();

        Required(settings.General.OrganizationName, "general.organizationName", errors);
        Required(settings.General.ShortName, "general.shortName", errors);
        Required(settings.General.ContactEmail, "general.contactEmail", errors);
        Required(settings.Notifications.AdminEmail, "notifications.adminEmail", errors);
        Required(settings.Notifications.SenderEmail, "notifications.senderEmail", errors);

        if (settings.General.Season.CurrentYear < 2000)
        {
            errors.Add(new ValidationError("general.season.currentYear", "La temporada actual no es valida."));
        }

        if (!DateOnly.TryParse(settings.General.Season.StartDate, out var seasonStart))
        {
            errors.Add(new ValidationError("general.season.startDate", "La fecha de inicio de temporada no es valida."));
        }

        if (!DateOnly.TryParse(settings.General.Season.EndDate, out var seasonEnd))
        {
            errors.Add(new ValidationError("general.season.endDate", "La fecha de fin de temporada no es valida."));
        }

        if (DateOnly.TryParse(settings.General.Season.StartDate, out seasonStart) &&
            DateOnly.TryParse(settings.General.Season.EndDate, out seasonEnd) &&
            seasonStart > seasonEnd)
        {
            errors.Add(new ValidationError("general.season.startDate", "La fecha de inicio no puede ser posterior a la fecha de fin."));
        }

        if (settings.Ranking.BestResultsCount is < 1 or > 10)
        {
            errors.Add(new ValidationError("ranking.bestResultsCount", "Los mejores resultados a contar deben estar entre 1 y 10."));
        }

        if (settings.Ranking.PointsMatrix.Count != 8)
        {
            errors.Add(new ValidationError("ranking.pointsMatrix", "La matriz de ranking debe incluir 8 filas de posiciones."));
        }

        foreach (var row in settings.Ranking.PointsMatrix)
        {
            if (string.IsNullOrWhiteSpace(row.Position))
            {
                errors.Add(new ValidationError("ranking.pointsMatrix.position", "Cada fila debe definir el puesto."));
            }

            if (row.Star1 < 0 || row.Star2 < 0 || row.Star3 < 0 || row.Star4 < 0 || row.Star5 < 0)
            {
                errors.Add(new ValidationError($"ranking.pointsMatrix.{row.Position}", "Los puntos de ranking no pueden ser negativos."));
            }
        }

        ValidatePrizeDistribution(settings.Ranking.PrizeDistribution, errors);

        if (settings.Integrations.SurfScores.CacheMinutes < 1)
        {
            errors.Add(new ValidationError("integrations.surfScores.cacheMinutes", "El cache de SurfScores debe ser de al menos 1 minuto."));
        }

        if (settings.Notifications.TokenValidityHours != AdminSettingsDefaults.TokenValidityHours)
        {
            errors.Add(new ValidationError("notifications.tokenValidityHours", "La validez del token debe mantenerse fija en 24 horas."));
        }

        if (settings.Live.YouTube.Width <= 0 || settings.Live.YouTube.Height <= 0)
        {
            errors.Add(new ValidationError("live.youtube", "Las dimensiones del embed de YouTube deben ser positivas."));
        }

        if (settings.Live.SurfScores.Width <= 0 || settings.Live.SurfScores.Height <= 0)
        {
            errors.Add(new ValidationError("live.surfScores", "Las dimensiones del iframe de SurfScores deben ser positivas."));
        }

        if (settings.Live.SurfScores.RefreshMinutes < AdminSettingsDefaults.MinimumSurfScoresRefreshMinutes)
        {
            errors.Add(new ValidationError("live.surfScores.refreshMinutes", "El refresco de SurfScores en vivo no puede ser menor a 5 minutos."));
        }

        if (!settings.Live.SurfScores.LocalDisplaysOnly)
        {
            errors.Add(new ValidationError("live.surfScores.localDisplaysOnly", "SurfScores en vivo solo puede habilitarse para pantallas locales."));
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("La configuracion enviada no es valida.", errors);
        }
    }

    private static void Required(string value, string field, ICollection<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new ValidationError(field, "El campo es obligatorio."));
        }
    }

    private static void ValidatePrizeDistribution(
        IReadOnlyCollection<PrizeDistributionSettingsDto> rows,
        ICollection<ValidationError> errors)
    {
        if (rows.Count == 0)
        {
            errors.Add(new ValidationError("ranking.prizeDistribution", "La distribucion de premios debe incluir al menos una fila."));
            return;
        }

        decimal star1 = 0;
        decimal star2 = 0;
        decimal star3 = 0;
        decimal star4 = 0;
        decimal star5 = 0;
        var seenPlaces = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.PlaceLabel))
            {
                errors.Add(new ValidationError("ranking.prizeDistribution.placeLabel", "Cada fila de premios debe definir el puesto."));
            }
            else if (!seenPlaces.Add(row.PlaceLabel.Trim()))
            {
                errors.Add(new ValidationError("ranking.prizeDistribution.placeLabel", $"Puesto duplicado: {row.PlaceLabel}."));
            }

            if (row.Star1Percent < 0 || row.Star2Percent < 0 || row.Star3Percent < 0 || row.Star4Percent < 0 || row.Star5Percent < 0)
            {
                errors.Add(new ValidationError($"ranking.prizeDistribution.{row.PlaceLabel}", "Los porcentajes de premios no pueden ser negativos."));
            }

            star1 += row.Star1Percent;
            star2 += row.Star2Percent;
            star3 += row.Star3Percent;
            star4 += row.Star4Percent;
            star5 += row.Star5Percent;
        }

        if (star1 > 100 || star2 > 100 || star3 > 100 || star4 > 100 || star5 > 100)
        {
            errors.Add(new ValidationError("ranking.prizeDistribution", "La suma de porcentajes por nivel de estrellas no puede exceder 100%."));
        }
    }
}
