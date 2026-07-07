using AlasApp.Domain.Common;
using AlasApp.Domain.Enums;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Domain.Entities;

public sealed class Payment : AuditableEntity
{
    private Payment()
    {
    }

    private Payment(
        Guid id,
        Guid inscriptionId,
        PaymentMethod method,
        decimal amountUsd,
        string transactionId,
        PaymentStatusAdmin status,
        DateTimeOffset fecha)
    {
        Id = id;
        InscriptionId = inscriptionId;
        Method = method;
        AmountUsd = amountUsd;
        TransactionId = transactionId;
        Status = status;
        Fecha = NormalizeDate(fecha);
    }

    public Guid InscriptionId { get; private set; }

    public Inscription? Inscription { get; private set; }

    public PaymentMethod Method { get; private set; }

    public decimal AmountUsd { get; private set; }

    public string TransactionId { get; private set; } = string.Empty;

    public PaymentStatusAdmin Status { get; private set; }

    public string? Notes { get; private set; }

    public DateTimeOffset Fecha { get; private set; }

    public static Payment Create(
        Guid inscriptionId,
        PaymentMethod method,
        decimal amountUsd,
        string transactionId,
        PaymentStatusAdmin status,
        DateTimeOffset fecha)
    {
        Validate(inscriptionId, amountUsd, transactionId);

        return new Payment(
            Guid.NewGuid(),
            inscriptionId,
            method,
            amountUsd,
            transactionId.Trim(),
            status,
            fecha);
    }

    public void Update(PaymentStatusAdmin status, string? notes)
    {
        if (notes is not null && notes.Trim().Length > 2000)
        {
            throw new DomainRuleException("Las notas del pago no pueden exceder 2000 caracteres.");
        }

        Status = status;
        Notes = NormalizeOptional(notes);
    }

    private static void Validate(Guid inscriptionId, decimal amountUsd, string transactionId)
    {
        if (inscriptionId == Guid.Empty)
        {
            throw new DomainRuleException("La inscripcion del pago es obligatoria.");
        }

        if (amountUsd < 0)
        {
            throw new DomainRuleException("El monto del pago no puede ser negativo.");
        }

        if (string.IsNullOrWhiteSpace(transactionId))
        {
            throw new DomainRuleException("La transaccion del pago es obligatoria.");
        }

        if (transactionId.Trim().Length > 100)
        {
            throw new DomainRuleException("La transaccion del pago no puede exceder 100 caracteres.");
        }
    }

    private static DateTimeOffset NormalizeDate(DateTimeOffset value)
    {
        return new DateTimeOffset(value.Year, value.Month, value.Day, 0, 0, 0, TimeSpan.Zero);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
