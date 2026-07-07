namespace AlasApp.Domain.Entities;

public sealed class CompetitorLicenseCategory
{
    private CompetitorLicenseCategory()
    {
    }

    private CompetitorLicenseCategory(Guid competitorId, string categoryId)
    {
        CompetitorId = competitorId;
        CategoryId = categoryId;
    }

    public Guid CompetitorId { get; private set; }

    public Competitor? Competitor { get; private set; }

    public string CategoryId { get; private set; } = string.Empty;

    public static CompetitorLicenseCategory Create(Guid competitorId, string categoryId)
    {
        return new CompetitorLicenseCategory(competitorId, categoryId.Trim());
    }
}
