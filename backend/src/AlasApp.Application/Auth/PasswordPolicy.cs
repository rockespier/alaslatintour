namespace AlasApp.Application.Auth;

internal static class PasswordPolicy
{
    public const string Message = "La contraseña debe tener al menos 8 caracteres, 1 mayúscula y 1 dígito.";

    public static bool IsValid(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            return false;
        }

        return password.Any(char.IsUpper) && password.Any(char.IsDigit);
    }
}
