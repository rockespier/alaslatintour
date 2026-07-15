using AlasApp.Domain.Common;
using AlasApp.Domain.Enums;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Domain.Entities;

public sealed class Inscription : AuditableEntity
{
    private Inscription()
    {
    }

    private Inscription(
        Guid id,
        Guid competitorId,
        Guid eventId,
        Guid categoryId,
        string? shirtNumber,
        PaymentMethod paymentMethod,
        decimal montoUsd,
        bool reglamentoAceptado)
    {
        Id = id;
        CompetitorId = competitorId;
        EventId = eventId;
        CategoryId = categoryId;
        ShirtNumber = NormalizeOptional(shirtNumber);
        PaymentMethod = paymentMethod;
        MontoUsd = montoUsd;
        ReglamentoAceptado = reglamentoAceptado;
        EstadoAdmin = InscriptionStatusAdmin.Pendiente;
        EstadoCompetidor = InscriptionStatusCompetitor.Pendiente;
        InscripcionAt = DateTimeOffset.UtcNow;
    }

    public Guid CompetitorId { get; private set; }

    public Competitor? Competitor { get; private set; }

    public Guid EventId { get; private set; }

    public Event? Event { get; private set; }

    public Guid CategoryId { get; private set; }

    public Category? Category { get; private set; }

    public string? ShirtNumber { get; private set; }

    public PaymentMethod PaymentMethod { get; private set; }

    public decimal MontoUsd { get; private set; }

    public InscriptionStatusAdmin EstadoAdmin { get; private set; }

    public InscriptionStatusCompetitor EstadoCompetidor { get; private set; }

    public string? Resultado { get; private set; }

    public string? TransaccionId { get; private set; }

    public string? Notes { get; private set; }

    public bool ReglamentoAceptado { get; private set; }

    public DateTimeOffset InscripcionAt { get; private set; }

    public static Inscription Create(
        Guid competitorId,
        Guid eventId,
        Guid categoryId,
        string? shirtNumber,
        PaymentMethod paymentMethod,
        decimal montoUsd,
        bool reglamentoAceptado,
        DateTimeOffset inscripcionAt)
    {
        Validate(competitorId, eventId, categoryId, shirtNumber, montoUsd, reglamentoAceptado);

        var inscription = new Inscription(
            Guid.NewGuid(),
            competitorId,
            eventId,
            categoryId,
            shirtNumber,
            paymentMethod,
            montoUsd,
            reglamentoAceptado);

        inscription.InscripcionAt = inscripcionAt;
        return inscription;
    }

    public void Update(string? shirtNumber, InscriptionStatusAdmin? estadoAdmin, string? notes)
    {
        if (!string.IsNullOrWhiteSpace(shirtNumber) && shirtNumber.Trim().Length > 20)
        {
            throw new DomainRuleException("El numero de camiseta no puede exceder 20 caracteres.");
        }

        if (notes is not null && notes.Length > 2000)
        {
            throw new DomainRuleException("Las notas no pueden exceder 2000 caracteres.");
        }

        ShirtNumber = NormalizeOptional(shirtNumber);
        Notes = NormalizeNullable(notes);

        if (estadoAdmin.HasValue)
        {
            EstadoAdmin = estadoAdmin.Value;
            EstadoCompetidor = estadoAdmin.Value == InscriptionStatusAdmin.Pagado
                ? InscriptionStatusCompetitor.Confirmado
                : InscriptionStatusCompetitor.Pendiente;
        }
    }

    public void ApplyPayment(PaymentMethod paymentMethod, string transactionId, InscriptionStatusAdmin estadoAdmin)
    {
        if (string.IsNullOrWhiteSpace(transactionId))
        {
            throw new DomainRuleException("La transaccion del pago es obligatoria.");
        }

        if (transactionId.Trim().Length > 100)
        {
            throw new DomainRuleException("La transaccion del pago no puede exceder 100 caracteres.");
        }

        PaymentMethod = paymentMethod;
        TransaccionId = transactionId.Trim();
        EstadoAdmin = estadoAdmin;
        EstadoCompetidor = estadoAdmin == InscriptionStatusAdmin.Pagado
            ? InscriptionStatusCompetitor.Confirmado
            : InscriptionStatusCompetitor.Pendiente;
    }

    public void ApplyResult(string result, DateTimeOffset timestamp)
    {
        if (string.IsNullOrWhiteSpace(result))
        {
            throw new DomainRuleException("El resultado de la inscripcion es obligatorio.");
        }

        if (result.Trim().Length > 250)
        {
            throw new DomainRuleException("El resultado de la inscripcion no puede exceder 250 caracteres.");
        }

        Resultado = result.Trim();
        EstadoCompetidor = InscriptionStatusCompetitor.Completado;
        SetUpdated(timestamp);
    }

    public bool CanBeDeleted()
    {
        return EstadoAdmin == InscriptionStatusAdmin.Pendiente
            && EstadoCompetidor == InscriptionStatusCompetitor.Pendiente
            && string.IsNullOrWhiteSpace(TransaccionId)
            && string.IsNullOrWhiteSpace(Resultado);
    }

    private static void Validate(
        Guid competitorId,
        Guid eventId,
        Guid categoryId,
        string? shirtNumber,
        decimal montoUsd,
        bool reglamentoAceptado)
    {
        if (competitorId == Guid.Empty || eventId == Guid.Empty || categoryId == Guid.Empty)
        {
            throw new DomainRuleException("Competidor, evento y categoria son obligatorios.");
        }

        if (!reglamentoAceptado)
        {
            throw new DomainRuleException("El competidor debe aceptar el reglamento ALAS.");
        }

        if (montoUsd < 0)
        {
            throw new DomainRuleException("El monto de la inscripcion no puede ser negativo.");
        }

        if (!string.IsNullOrWhiteSpace(shirtNumber) && shirtNumber.Trim().Length > 20)
        {
            throw new DomainRuleException("El numero de camiseta no puede exceder 20 caracteres.");
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
