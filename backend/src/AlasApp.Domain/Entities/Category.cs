using AlasApp.Domain.Common;
using AlasApp.Domain.Enums;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Domain.Entities;

public sealed class Category : AuditableEntity
{
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
        CategoryStatus status)
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
        CategoryStatus status)
    {
        Validate(nombre, descripcion, ageRestriction, minAge, maxAge, successorCategoryId, null);

        return new Category(
            Guid.NewGuid(),
            nombre.Trim(),
            NormalizeOptional(descripcion),
            gender,
            ageRestriction,
            minAge,
            maxAge,
            successorCategoryId,
            status);
    }

    public void Update(
        string nombre,
        string? descripcion,
        CategoryGender gender,
        bool ageRestriction,
        int? minAge,
        int? maxAge,
        Guid? successorCategoryId,
        CategoryStatus status)
    {
        Validate(nombre, descripcion, ageRestriction, minAge, maxAge, successorCategoryId, Id);

        Nombre = nombre.Trim();
        Descripcion = NormalizeOptional(descripcion);
        Gender = gender;
        AgeRestriction = ageRestriction;
        MinAge = minAge;
        MaxAge = maxAge;
        SuccessorCategoryId = successorCategoryId;
        Status = status;
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
        Guid? categoryId)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new DomainRuleException("El nombre de la categoria es obligatorio.");
        }

        if (descripcion is not null && descripcion.Length > 2000)
        {
            throw new DomainRuleException("La descripcion de la categoria no puede exceder 2000 caracteres.");
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
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
