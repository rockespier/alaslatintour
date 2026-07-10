namespace AlasApp.Infrastructure.Authentication;

public sealed class BootstrapAdminOptions
{
    public const string SectionName = "BootstrapAdmin";

    public bool Enabled { get; init; }

    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string Nombre { get; init; } = string.Empty;

    public string Apellido { get; init; } = string.Empty;
}
