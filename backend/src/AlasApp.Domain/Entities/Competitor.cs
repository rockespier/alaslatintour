using AlasApp.Domain.Common;
using AlasApp.Domain.Enums;
using AlasApp.Domain.Exceptions;
using System.Net.Mail;

namespace AlasApp.Domain.Entities;

public sealed class Competitor : AuditableEntity
{
    private readonly List<CompetitorLicenseCategory> _enabledLicenseCategories = [];
    private readonly List<CompetitorFine> _fines = [];

    private Competitor()
    {
    }

    private Competitor(
        Guid id,
        string nombre,
        string apellido,
        string email,
        DateTimeOffset fechaNacimiento,
        CompetitorGender genero,
        string pais,
        string telefono,
        string club,
        CompetitorPostura postura,
        CompetitorShirtSize tallaCamiseta,
        string numeroCamiseta,
        string patrocinadores,
        string federacion,
        string surfScoresCode)
    {
        Id = id;
        Nombre = nombre;
        Apellido = apellido;
        Email = email;
        FechaNacimiento = NormalizeDate(fechaNacimiento);
        Genero = genero;
        Pais = pais;
        Telefono = telefono;
        Club = club;
        Postura = postura;
        TallaCamiseta = tallaCamiseta;
        NumeroCamiseta = numeroCamiseta;
        Patrocinadores = patrocinadores;
        Federacion = federacion;
        SurfScoresCode = surfScoresCode;
        LicenseNumber = string.Empty;
        LicenseNumberLong = string.Empty;
        LicenseStatus = LicenseStatus.PendienteDeValidacion;
        LicenseExpirationDate = DateTimeOffset.UnixEpoch;
        NotificationEmail = true;
        NotificationPush = false;
        NotificationResultados = true;
        NotificationInscripciones = true;
    }

    public string Nombre { get; private set; } = string.Empty;

    public string Apellido { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public DateTimeOffset FechaNacimiento { get; private set; }

    public CompetitorGender Genero { get; private set; }

    public string Pais { get; private set; } = string.Empty;

    public string Telefono { get; private set; } = string.Empty;

    public string Club { get; private set; } = string.Empty;

    public CompetitorPostura Postura { get; private set; }

    public CompetitorShirtSize TallaCamiseta { get; private set; }

    public string NumeroCamiseta { get; private set; } = string.Empty;

    public string Patrocinadores { get; private set; } = string.Empty;

    public string Federacion { get; private set; } = string.Empty;

    public string SurfScoresCode { get; private set; } = string.Empty;

    public string LicenseNumber { get; private set; } = string.Empty;

    public string LicenseNumberLong { get; private set; } = string.Empty;

    public LicenseStatus LicenseStatus { get; private set; }

    public DateTimeOffset LicenseExpirationDate { get; private set; }

    public bool NotificationEmail { get; private set; }

    public bool NotificationPush { get; private set; }

    public bool NotificationResultados { get; private set; }

    public bool NotificationInscripciones { get; private set; }

    public IReadOnlyCollection<CompetitorLicenseCategory> EnabledLicenseCategories => _enabledLicenseCategories;

    public IReadOnlyCollection<CompetitorFine> Fines => _fines;

    public static Competitor Create(
        string nombre,
        string apellido,
        string email,
        DateTimeOffset fechaNacimiento,
        CompetitorGender genero,
        string pais,
        string telefono,
        string club,
        CompetitorPostura postura,
        CompetitorShirtSize tallaCamiseta,
        string numeroCamiseta,
        string patrocinadores,
        string federacion,
        string? surfScoresCode = null)
    {
        Validate(
            nombre,
            apellido,
            email,
            fechaNacimiento,
            pais,
            telefono,
            club,
            numeroCamiseta,
            patrocinadores,
            federacion,
            surfScoresCode ?? string.Empty);

        return new Competitor(
            Guid.NewGuid(),
            nombre.Trim(),
            apellido.Trim(),
            email.Trim(),
            fechaNacimiento,
            genero,
            pais.Trim(),
            NormalizeOptional(telefono),
            NormalizeOptional(club),
            postura,
            tallaCamiseta,
            NormalizeOptional(numeroCamiseta),
            NormalizeOptional(patrocinadores),
            NormalizeOptional(federacion),
            NormalizeOptional(surfScoresCode));
    }

    public void Update(
        string nombre,
        string apellido,
        string email,
        DateTimeOffset fechaNacimiento,
        CompetitorGender genero,
        string pais,
        string telefono,
        string club,
        CompetitorPostura postura,
        CompetitorShirtSize tallaCamiseta,
        string numeroCamiseta,
        string patrocinadores,
        string federacion,
        string? surfScoresCode = null)
    {
        Validate(
            nombre,
            apellido,
            email,
            fechaNacimiento,
            pais,
            telefono,
            club,
            numeroCamiseta,
            patrocinadores,
            federacion,
            surfScoresCode ?? string.Empty);

        Nombre = nombre.Trim();
        Apellido = apellido.Trim();
        Email = email.Trim();
        FechaNacimiento = NormalizeDate(fechaNacimiento);
        Genero = genero;
        Pais = pais.Trim();
        Telefono = NormalizeOptional(telefono);
        Club = NormalizeOptional(club);
        Postura = postura;
        TallaCamiseta = tallaCamiseta;
        NumeroCamiseta = NormalizeOptional(numeroCamiseta);
        Patrocinadores = NormalizeOptional(patrocinadores);
        Federacion = NormalizeOptional(federacion);
        SurfScoresCode = NormalizeOptional(surfScoresCode);
    }

    public void UpdateLicense(
        LicenseStatus status,
        string licenseNumber,
        string licenseNumberLong,
        DateTimeOffset expirationDate,
        IEnumerable<string> enabledCategories)
    {
        var normalizedNumber = NormalizeOptional(licenseNumber);
        var normalizedLong = NormalizeOptional(licenseNumberLong);
        var normalizedCategories = enabledCategories
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (status == LicenseStatus.Activa && string.IsNullOrWhiteSpace(normalizedNumber))
        {
            throw new DomainRuleException("Una licencia activa debe tener numero de licencia.");
        }

        LicenseStatus = status;
        LicenseNumber = normalizedNumber;
        LicenseNumberLong = normalizedLong;
        LicenseExpirationDate = NormalizeDate(expirationDate);

        _enabledLicenseCategories.Clear();
        _enabledLicenseCategories.AddRange(normalizedCategories.Select(x => CompetitorLicenseCategory.Create(Id, x)));
    }

    public void UpdateNotificationPreferences(bool email, bool push, bool resultados, bool inscripciones)
    {
        NotificationEmail = email;
        NotificationPush = push;
        NotificationResultados = resultados;
        NotificationInscripciones = inscripciones;
    }

    private static void Validate(
        string nombre,
        string apellido,
        string email,
        DateTimeOffset fechaNacimiento,
        string pais,
        string telefono,
        string club,
        string numeroCamiseta,
        string patrocinadores,
        string federacion,
        string surfScoresCode)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new DomainRuleException("El nombre del competidor es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(apellido))
        {
            throw new DomainRuleException("El apellido del competidor es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(pais))
        {
            throw new DomainRuleException("El pais del competidor es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainRuleException("El email del competidor es obligatorio.");
        }

        try
        {
            _ = new MailAddress(email.Trim());
        }
        catch (FormatException)
        {
            throw new DomainRuleException("El email del competidor no es valido.");
        }

        if (NormalizeDate(fechaNacimiento) > NormalizeDate(DateTimeOffset.UtcNow))
        {
            throw new DomainRuleException("La fecha de nacimiento no puede ser futura.");
        }

        ValidateLength(telefono, 50, "El telefono del competidor no puede exceder 50 caracteres.");
        ValidateLength(club, 200, "El club del competidor no puede exceder 200 caracteres.");
        ValidateLength(numeroCamiseta, 20, "El numero de camiseta no puede exceder 20 caracteres.");
        ValidateLength(patrocinadores, 1000, "Los patrocinadores no pueden exceder 1000 caracteres.");
        ValidateLength(federacion, 200, "La federacion no puede exceder 200 caracteres.");
        ValidateLength(surfScoresCode, 100, "El codigo de SurfScores no puede exceder 100 caracteres.");
    }

    private static void ValidateLength(string value, int maxLength, string message)
    {
        if (value.Trim().Length > maxLength)
        {
            throw new DomainRuleException(message);
        }
    }

    private static string NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static DateTimeOffset NormalizeDate(DateTimeOffset value)
    {
        return new DateTimeOffset(value.Year, value.Month, value.Day, 0, 0, 0, TimeSpan.Zero);
    }
}
