using AlasApp.Domain.Common;
using AlasApp.Domain.Enums;
using AlasApp.Domain.Exceptions;
using System.Net.Mail;

namespace AlasApp.Domain.Entities;

public sealed class UserAccount : AuditableEntity
{
    private UserAccount()
    {
    }

    private UserAccount(
        Guid id,
        string email,
        string passwordHash,
        string nombre,
        string apellido,
        UserType tipo,
        string pais,
        PreferredLanguage idiomaPreferido,
        bool newsletter,
        bool acceptedTerms,
        bool acceptedReglamento,
        Guid? competitorId,
        AdminRole? adminRole)
    {
        Id = id;
        Email = email;
        PasswordHash = passwordHash;
        Nombre = nombre;
        Apellido = apellido;
        Tipo = tipo;
        Pais = pais;
        IdiomaPreferido = idiomaPreferido;
        Newsletter = newsletter;
        AcceptedTerms = acceptedTerms;
        AcceptedReglamento = acceptedReglamento;
        CompetitorId = competitorId;
        AdminRole = adminRole;
        IsActive = true;
        TokenVersion = 1;
        FailedLoginAttempts = 0;
        LastLoginAtUtc = null;
        LockedUntilUtc = null;
        PasswordChangedAtUtc = null;
    }

    public string Email { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public string Nombre { get; private set; } = string.Empty;

    public string Apellido { get; private set; } = string.Empty;

    public UserType Tipo { get; private set; }

    public string Pais { get; private set; } = string.Empty;

    public PreferredLanguage IdiomaPreferido { get; private set; }

    public bool Newsletter { get; private set; }

    public bool AcceptedTerms { get; private set; }

    public bool AcceptedReglamento { get; private set; }

    public Guid? CompetitorId { get; private set; }

    public AdminRole? AdminRole { get; private set; }

    public bool IsActive { get; private set; }

    public int TokenVersion { get; private set; }

    public int FailedLoginAttempts { get; private set; }

    public DateTimeOffset? LockedUntilUtc { get; private set; }

    public DateTimeOffset? LastLoginAtUtc { get; private set; }

    public DateTimeOffset? PasswordChangedAtUtc { get; private set; }

    public string FullName => $"{Nombre} {Apellido}".Trim();

    public static UserAccount Create(
        string email,
        string passwordHash,
        string nombre,
        string apellido,
        UserType tipo,
        string pais,
        PreferredLanguage idiomaPreferido,
        bool newsletter,
        bool acceptedTerms,
        bool acceptedReglamento,
        Guid? competitorId = null,
        AdminRole? adminRole = null)
    {
        Validate(email, passwordHash, nombre, apellido, tipo, pais, acceptedTerms, acceptedReglamento);

        return new UserAccount(
            Guid.NewGuid(),
            email.Trim().ToLowerInvariant(),
            passwordHash.Trim(),
            nombre.Trim(),
            apellido.Trim(),
            tipo,
            NormalizeOptional(pais),
            idiomaPreferido,
            newsletter,
            acceptedTerms,
            acceptedReglamento,
            competitorId,
            adminRole);
    }

    public void RecordLogin(DateTimeOffset timestamp)
    {
        EnsureActive();
        EnsureNotLocked(timestamp);
        LastLoginAtUtc = timestamp;
        FailedLoginAttempts = 0;
        LockedUntilUtc = null;
    }

    public bool RegisterFailedLoginAttempt(DateTimeOffset timestamp, int maxAttempts, TimeSpan lockDuration)
    {
        if (maxAttempts <= 0)
        {
            throw new DomainRuleException("La configuración de intentos fallidos no es válida.");
        }

        if (lockDuration <= TimeSpan.Zero)
        {
            throw new DomainRuleException("La duración del bloqueo debe ser positiva.");
        }

        if (LockedUntilUtc.HasValue && LockedUntilUtc.Value <= timestamp)
        {
            FailedLoginAttempts = 0;
            LockedUntilUtc = null;
        }

        FailedLoginAttempts++;

        if (FailedLoginAttempts >= maxAttempts)
        {
            FailedLoginAttempts = maxAttempts;
            LockedUntilUtc = timestamp.Add(lockDuration);
            return true;
        }

        return false;
    }

    public void ChangePassword(string passwordHash, DateTimeOffset timestamp)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainRuleException("La contraseña es obligatoria.");
        }

        PasswordHash = passwordHash.Trim();
        PasswordChangedAtUtc = timestamp;
        TokenVersion++;
    }

    public void InvalidateActiveTokens()
    {
        TokenVersion++;
    }

    public void Deactivate()
    {
        IsActive = false;
        TokenVersion++;
    }

    public void Activate()
    {
        IsActive = true;
        TokenVersion++;
    }

    public void AssignAdminRole(AdminRole role)
    {
        AdminRole = role;
    }

    public void EnsureActive()
    {
        if (!IsActive)
        {
            throw new DomainRuleException("La cuenta de usuario está inactiva.");
        }
    }

    public void EnsureNotLocked(DateTimeOffset timestamp)
    {
        if (!LockedUntilUtc.HasValue)
        {
            return;
        }

        if (LockedUntilUtc.Value <= timestamp)
        {
            FailedLoginAttempts = 0;
            LockedUntilUtc = null;
            return;
        }

        throw new DomainRuleException("La cuenta está bloqueada temporalmente por múltiples intentos fallidos.");
    }

    private static void Validate(
        string email,
        string passwordHash,
        string nombre,
        string apellido,
        UserType tipo,
        string pais,
        bool acceptedTerms,
        bool acceptedReglamento)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new DomainRuleException("El nombre es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(apellido))
        {
            throw new DomainRuleException("El apellido es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainRuleException("El email es obligatorio.");
        }

        try
        {
            _ = new MailAddress(email.Trim());
        }
        catch (FormatException)
        {
            throw new DomainRuleException("El email no es válido.");
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainRuleException("La contraseña es obligatoria.");
        }

        if (!acceptedTerms)
        {
            throw new DomainRuleException("Debes aceptar los términos para registrarte.");
        }

        if (tipo == UserType.Competidor && !acceptedReglamento)
        {
            throw new DomainRuleException("Debes aceptar el reglamento para registrarte como competidor.");
        }

        if (tipo == UserType.Competidor && string.IsNullOrWhiteSpace(pais))
        {
            throw new DomainRuleException("El país es obligatorio para competidores.");
        }
    }

    private static string NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
