using AlasApp.Domain.Common;
using AlasApp.Domain.Enums;
using AlasApp.Domain.Exceptions;
using System.Net.Mail;

namespace AlasApp.Domain.Entities;

public sealed class Membership : AuditableEntity
{
    private Membership()
    {
    }

    private Membership(
        Guid id,
        string clubFederacion,
        string pais,
        MembershipPlan plan,
        DateTimeOffset inicioVigencia,
        DateTimeOffset vencimiento,
        string emailContacto)
    {
        Id = id;
        ClubFederacion = clubFederacion;
        Pais = pais;
        Plan = plan;
        InicioVigencia = NormalizeDate(inicioVigencia);
        Vencimiento = NormalizeDate(vencimiento);
        EmailContacto = emailContacto;
    }

    public string ClubFederacion { get; private set; } = string.Empty;

    public string Pais { get; private set; } = string.Empty;

    public MembershipPlan Plan { get; private set; }

    public DateTimeOffset InicioVigencia { get; private set; }

    public DateTimeOffset Vencimiento { get; private set; }

    public string EmailContacto { get; private set; } = string.Empty;

    public static Membership Create(
        string clubFederacion,
        string pais,
        MembershipPlan plan,
        DateTimeOffset inicioVigencia,
        DateTimeOffset vencimiento,
        string emailContacto)
    {
        Validate(clubFederacion, pais, inicioVigencia, vencimiento, emailContacto);

        return new Membership(
            Guid.NewGuid(),
            clubFederacion.Trim(),
            pais.Trim(),
            plan,
            inicioVigencia,
            vencimiento,
            NormalizeEmail(emailContacto));
    }

    public void Update(
        string clubFederacion,
        string pais,
        MembershipPlan plan,
        DateTimeOffset inicioVigencia,
        DateTimeOffset vencimiento,
        string emailContacto)
    {
        Validate(clubFederacion, pais, inicioVigencia, vencimiento, emailContacto);

        ClubFederacion = clubFederacion.Trim();
        Pais = pais.Trim();
        Plan = plan;
        InicioVigencia = NormalizeDate(inicioVigencia);
        Vencimiento = NormalizeDate(vencimiento);
        EmailContacto = NormalizeEmail(emailContacto);
    }

    public MembershipStatus GetStatus(DateTimeOffset nowUtc)
    {
        var today = NormalizeDate(nowUtc);
        return Vencimiento <= today.AddDays(30)
            ? MembershipStatus.VencePronto
            : MembershipStatus.Activo;
    }

    private static void Validate(
        string clubFederacion,
        string pais,
        DateTimeOffset inicioVigencia,
        DateTimeOffset vencimiento,
        string emailContacto)
    {
        if (string.IsNullOrWhiteSpace(clubFederacion))
        {
            throw new DomainRuleException("El club o federacion es obligatorio.");
        }

        if (clubFederacion.Trim().Length > 200)
        {
            throw new DomainRuleException("El club o federacion no puede exceder 200 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(pais))
        {
            throw new DomainRuleException("El pais es obligatorio.");
        }

        if (pais.Trim().Length > 100)
        {
            throw new DomainRuleException("El pais no puede exceder 100 caracteres.");
        }

        if (NormalizeDate(vencimiento) < NormalizeDate(inicioVigencia))
        {
            throw new DomainRuleException("La fecha de vencimiento no puede ser anterior al inicio de vigencia.");
        }

        _ = NormalizeEmail(emailContacto);
    }

    private static string NormalizeEmail(string emailContacto)
    {
        if (string.IsNullOrWhiteSpace(emailContacto))
        {
            throw new DomainRuleException("El email de contacto es obligatorio.");
        }

        var normalized = emailContacto.Trim();

        if (normalized.Length > 200)
        {
            throw new DomainRuleException("El email de contacto no puede exceder 200 caracteres.");
        }

        try
        {
            _ = new MailAddress(normalized);
            return normalized;
        }
        catch (FormatException)
        {
            throw new DomainRuleException("El email de contacto no es valido.");
        }
    }

    private static DateTimeOffset NormalizeDate(DateTimeOffset value)
    {
        return new DateTimeOffset(value.Year, value.Month, value.Day, 0, 0, 0, TimeSpan.Zero);
    }
}
