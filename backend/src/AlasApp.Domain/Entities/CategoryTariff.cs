using AlasApp.Domain.Exceptions;

namespace AlasApp.Domain.Entities;

public sealed class CategoryTariff
{
    private CategoryTariff()
    {
    }

    private CategoryTariff(Guid categoryId, int starLevel, decimal usd, decimal cop, bool active)
    {
        CategoryId = categoryId;
        StarLevel = starLevel;
        Usd = usd;
        Cop = cop;
        Active = active;
    }

    public Guid CategoryId { get; private set; }

    public Category? Category { get; private set; }

    public int StarLevel { get; private set; }

    public decimal Usd { get; private set; }

    public decimal Cop { get; private set; }

    public bool Active { get; private set; }

    public static CategoryTariff Create(Guid categoryId, int starLevel, decimal usd, decimal cop, bool active)
    {
        Validate(categoryId, starLevel, usd, cop);
        return new CategoryTariff(categoryId, starLevel, usd, cop, active);
    }

    public void Update(decimal usd, decimal cop, bool active)
    {
        Validate(CategoryId, StarLevel, usd, cop);
        Usd = usd;
        Cop = cop;
        Active = active;
    }

    private static void Validate(Guid categoryId, int starLevel, decimal usd, decimal cop)
    {
        if (categoryId == Guid.Empty)
        {
            throw new DomainRuleException("La categoria de la tarifa es obligatoria.");
        }

        if (starLevel is < 1 or > 7)
        {
            throw new DomainRuleException("El nivel de estrellas de la tarifa debe estar entre 1 y 7.");
        }

        if (usd < 0 || cop < 0)
        {
            throw new DomainRuleException("Las tarifas no pueden ser negativas.");
        }
    }
}
