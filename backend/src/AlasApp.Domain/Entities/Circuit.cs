using AlasApp.Domain.Common;
using AlasApp.Domain.Enums;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Domain.Entities;

public sealed class Circuit : AuditableEntity
{
    private readonly List<Event> _events = [];

    private Circuit()
    {
    }

    private Circuit(
        Guid id,
        string nombre,
        int temporada,
        string? descripcion,
        CircuitRegion region,
        CircuitModalidad modalidad,
        CircuitStatus estado,
        string? surfScoresCode)
    {
        Id = id;
        Nombre = nombre;
        Temporada = temporada;
        Descripcion = descripcion;
        Region = region;
        Modalidad = modalidad;
        Estado = estado;
        SurfScoresCode = surfScoresCode;
    }

    public string Nombre { get; private set; } = string.Empty;

    public int Temporada { get; private set; }

    public string? Descripcion { get; private set; }

    public CircuitRegion Region { get; private set; }

    public CircuitModalidad Modalidad { get; private set; }

    public CircuitStatus Estado { get; private set; }

    public string? SurfScoresCode { get; private set; }

    public DateTimeOffset? LastSyncAt { get; private set; }

    public IReadOnlyCollection<Event> Events => _events;

    public static Circuit Create(
        string nombre,
        int temporada,
        string? descripcion,
        CircuitRegion region,
        CircuitModalidad modalidad,
        CircuitStatus estado,
        string? surfScoresCode)
    {
        Validate(nombre, temporada, descripcion, surfScoresCode);

        return new Circuit(
            Guid.NewGuid(),
            nombre.Trim(),
            temporada,
            NormalizeOptional(descripcion),
            region,
            modalidad,
            estado,
            NormalizeOptional(surfScoresCode));
    }

    public void Update(
        string nombre,
        int temporada,
        string? descripcion,
        CircuitRegion region,
        CircuitModalidad modalidad,
        CircuitStatus estado,
        string? surfScoresCode)
    {
        Validate(nombre, temporada, descripcion, surfScoresCode);

        Nombre = nombre.Trim();
        Temporada = temporada;
        Descripcion = NormalizeOptional(descripcion);
        Region = region;
        Modalidad = modalidad;
        Estado = estado;
        SurfScoresCode = NormalizeOptional(surfScoresCode);
    }

    public void MarkSynced(DateTimeOffset syncedAtUtc)
    {
        LastSyncAt = syncedAtUtc;
    }

    public void EnsureCanBeDeleted()
    {
        if (_events.Count > 0)
        {
            throw new DomainRuleException("No se puede eliminar un circuito con eventos asociados.");
        }
    }

    private static void Validate(string nombre, int temporada, string? descripcion, string? surfScoresCode)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new DomainRuleException("El nombre del circuito es obligatorio.");
        }

        if (temporada is < 2020 or > 2030)
        {
            throw new DomainRuleException("La temporada del circuito debe estar entre 2020 y 2030.");
        }

        if (descripcion is not null && descripcion.Length > 2000)
        {
            throw new DomainRuleException("La descripcion del circuito no puede exceder 2000 caracteres.");
        }

        if (surfScoresCode is not null && surfScoresCode.Length > 100)
        {
            throw new DomainRuleException("El codigo de SurfScores del circuito no puede exceder 100 caracteres.");
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
