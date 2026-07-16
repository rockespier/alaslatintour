using AlasApp.Domain.Common;
using AlasApp.Domain.Enums;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Domain.Entities;

public sealed class Category : AuditableEntity
{
    public const int DefaultBestResultsCount = 5;

    private readonly List<CategoryTariff> _tariffs = [];
    private readonly List<EventCategory> _eventCategories = [];

    private Category()
    {
    }

    private Category(
        Guid id,
        string nombre,
        string? descripcion,
        CategoryGender gender,
        bool ageRestriction,
        int? minAge,
        int? maxAge,
        Guid? successorCategoryId,
        CategoryStatus status,
        decimal membresiaAnualUsd,
        decimal membresiaPorEventoUsd,
        int bestResultsCount,
        string? surfScoresCode)
    {
        Id = id;
        Nombre = nombre;
        Descripcion = descripcion;
        Gender = gender;
        AgeRestriction = ageRestriction;
        MinAge = minAge;
        MaxAge = maxAge;
        SuccessorCategoryId = successorCategoryId;
        Status = status;
        MembresiaAnualUsd = membresiaAnualUsd;
        MembresiaPorEventoUsd = membresiaPorEventoUsd;
        BestResultsCount = bestResultsCount;
        SurfScoresCode = surfScoresCode;
    }

    public string Nombre { get; private set; } = string.Empty;

    public string? Descripcion { get; private set; }

    public CategoryGender Gender { get; private set; }

    public bool AgeRestriction { get; private set; }

    public int? MinAge { get; private set; }

    public int? MaxAge { get; private set; }

    public Guid? SuccessorCategoryId { get; private set; }

    public Category? SuccessorCategory { get; private set; }

    public CategoryStatus Status { get; private set; }

    public decimal MembresiaAnualUsd { get; private set; }

    public decimal MembresiaPorEventoUsd { get; private set; }

    public int BestResultsCount { get; private set; }

    public string? SurfScoresCode { get; private set; }

    public IReadOnlyCollection<CategoryTariff> Tariffs => _tariffs;

    public IReadOnlyCollection<EventCategory> EventCategories => _eventCategories;

    public static Category Create(
        string nombre,
        string? descripcion,
        CategoryGender gender,
        bool ageRestriction,
        int? minAge,
        int? maxAge,
        Guid? successorCategoryId,
        CategoryStatus status,
        decimal membresiaAnualUsd,
        decimal membresiaPorEventoUsd,
        int bestResultsCount,
        string? surfScoresCode)
    {
        Validate(
            nombre,
            descripcion,
            ageRestriction,
            minAge,
            maxAge,
            successorCategoryId,
            null,
            membresiaAnualUsd,
            membresiaPorEventoUsd,
            bestResultsCount,
            surfScoresCode);

        return new Category(
            Guid.NewGuid(),
            nombre.Trim(),
            NormalizeOptional(descripcion),
            gender,
            ageRestriction,
            minAge,
            maxAge,
            successorCategoryId,
            status,
            membresiaAnualUsd,
            membresiaPorEventoUsd,
            bestResultsCount,
            NormalizeOptional(surfScoresCode));
    }

    public void Update(
        string nombre,
        string? descripcion,
        CategoryGender gender,
        bool ageRestriction,
        int? minAge,
        int? maxAge,
        Guid? successorCategoryId,
        CategoryStatus status,
        decimal membresiaAnualUsd,
        decimal membresiaPorEventoUsd,
        int bestResultsCount,
        string? surfScoresCode)
    {
        Validate(
            nombre,
            descripcion,
            ageRestriction,
            minAge,
            maxAge,
            successorCategoryId,
            Id,
            membresiaAnualUsd,
            membresiaPorEventoUsd,
            bestResultsCount,
            surfScoresCode);

        Nombre = nombre.Trim();
        Descripcion = NormalizeOptional(descripcion);
        Gender = gender;
        AgeRestriction = ageRestriction;
        MinAge = minAge;
        MaxAge = maxAge;
        SuccessorCategoryId = successorCategoryId;
        Status = status;
        MembresiaAnualUsd = membresiaAnualUsd;
        MembresiaPorEventoUsd = membresiaPorEventoUsd;
        BestResultsCount = bestResultsCount;
        SurfScoresCode = NormalizeOptional(surfScoresCode);
    }

    public void EnsureCanBeDeleted()
    {
        if (_eventCategories.Count > 0)
        {
            throw new DomainRuleException("No se puede eliminar una categoria asociada a eventos.");
        }
    }

    public CategoryTariff SetTariff(int starLevel, decimal usd, decimal cop, bool active)
    {
        var tariff = _tariffs.FirstOrDefault(x => x.StarLevel == starLevel);

        if (tariff is null)
        {
            tariff = CategoryTariff.Create(Id, starLevel, usd, cop, active);
            _tariffs.Add(tariff);
            return tariff;
        }

        tariff.Update(usd, cop, active);
        return tariff;
    }

    private static void Validate(
        string nombre,
        string? descripcion,
        bool ageRestriction,
        int? minAge,
        int? maxAge,
        Guid? successorCategoryId,
        Guid? categoryId,
        decimal membresiaAnualUsd,
        decimal membresiaPorEventoUsd,
        int bestResultsCount,
        string? surfScoresCode)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new DomainRuleException("El nombre de la categoria es obligatorio.");
        }

        if (descripcion is not null && descripcion.Length > 2000)
        {
            throw new DomainRuleException("La descripcion de la categoria no puede exceder 2000 caracteres.");
        }

        if (surfScoresCode is not null && surfScoresCode.Length > 100)
        {
            throw new DomainRuleException("El codigo de SurfScores de la categoria no puede exceder 100 caracteres.");
        }

        if (!ageRestriction && (minAge.HasValue || maxAge.HasValue))
        {
            throw new DomainRuleException("Los limites de edad solo se permiten cuando la restriccion por edad esta activa.");
        }

        if (ageRestriction && (!minAge.HasValue || !maxAge.HasValue))
        {
            throw new DomainRuleException("La categoria con restriccion de edad debe definir edad minima y maxima.");
        }

        if (minAge.HasValue && minAge.Value < 0)
        {
            throw new DomainRuleException("La edad minima no puede ser negativa.");
        }

        if (maxAge.HasValue && maxAge.Value < 0)
        {
            throw new DomainRuleException("La edad maxima no puede ser negativa.");
        }

        if (minAge.HasValue && maxAge.HasValue && minAge.Value > maxAge.Value)
        {
            throw new DomainRuleException("La edad minima no puede ser mayor que la edad maxima.");
        }

        if (successorCategoryId.HasValue && categoryId.HasValue && successorCategoryId.Value == categoryId.Value)
        {
            throw new DomainRuleException("La categoria sucesora no puede ser la misma categoria.");
        }

        if (membresiaAnualUsd < 0)
        {
            throw new DomainRuleException("La membresia anual no puede ser negativa.");
        }

        if (membresiaPorEventoUsd < 0)
        {
            throw new DomainRuleException("La membresia por evento no puede ser negativa.");
        }

        if (bestResultsCount is < 1 or > 10)
        {
            throw new DomainRuleException("La cantidad de mejores resultados debe estar entre 1 y 10.");
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
