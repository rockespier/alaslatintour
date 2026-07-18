using AlasApp.Api.Models;
using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Auth.Commands.ConfirmPasswordReset;
using AlasApp.Application.Auth.Commands.LogoutUser;
using AlasApp.Application.Auth.Commands.RegisterUser;
using AlasApp.Application.IdentityDocuments;
using AlasApp.Domain.Enums;
using AlasApp.Application.Auth.Commands.RequestPasswordReset;
using Generated = AlasApp.AlasApi.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AlasApp.Api.Controllers;

[ApiController]
[Route("v1/auth")]
public sealed class AuthController(IRequestDispatcher dispatcher) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Generated.LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Generated.LoginResponse>> Login([FromBody] Generated.LoginRequest body, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(ApiContractMapper.ToCommand(body), cancellationToken);
        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(Generated.RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Generated.RegisterResponse>> Register([FromForm] RegisterFormRequest body, CancellationToken cancellationToken)
    {
        var command = new RegisterUserCommand(
            body.Email,
            body.Password,
            body.Nombre,
            body.Apellido,
            ParseUserType(body.Tipo),
            body.Pais ?? string.Empty,
            ParsePreferredLanguage(body.IdiomaPreferido),
            body.Newsletter,
            body.Terminos,
            body.Reglamento,
            body.FechaNacimiento,
            ParseOptional<CompetitorGender>(body.Genero),
            body.Telefono ?? string.Empty,
            body.Club ?? string.Empty,
            ParseOptional<CompetitorPostura>(body.Postura),
            ParseOptional<CompetitorShirtSize>(body.TallaCamiseta),
            body.Federacion ?? string.Empty,
            body.Patrocinadores ?? string.Empty,
            body.IdentityDocument is null
                ? null
                : new IdentityDocumentUpload(
                    body.IdentityDocument.FileName,
                    body.IdentityDocument.ContentType,
                    body.IdentityDocument.OpenReadStream(),
                    body.IdentityDocument.Length));

        var result = await dispatcher.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiContractMapper.ToContract(result));
    }


    [HttpPost("register")]
    [AllowAnonymous]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(Generated.RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Generated.RegisterResponse>> RegisterJson([FromBody] Generated.RegisterRequest body, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(ApiContractMapper.ToCommand(body), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiContractMapper.ToContract(result));
    }

    [HttpPost("password-reset/request")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Generated.MessageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<Generated.MessageResponse>> RequestPasswordReset([FromBody] Generated.PasswordResetRequest body, CancellationToken cancellationToken)
    {
        await dispatcher.Send(new RequestPasswordResetCommand(body.Email), cancellationToken);
        return Ok(new Generated.MessageResponse("Si el correo existe, recibirás un enlace de recuperación."));
    }

    [HttpPost("password-reset/confirm")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Generated.MessageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<Generated.MessageResponse>> ConfirmPasswordReset([FromBody] Generated.PasswordResetConfirmRequest body, CancellationToken cancellationToken)
    {
        await dispatcher.Send(new ConfirmPasswordResetCommand(body.Token, body.NewPassword), cancellationToken);
        return Ok(new Generated.MessageResponse("Contraseña actualizada correctamente."));
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(Generated.MessageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<Generated.MessageResponse>> Logout(CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Name);
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            throw new UnauthorizedAccessException("No se pudo identificar el usuario autenticado.");
        }

        await dispatcher.Send(new LogoutUserCommand(userId), cancellationToken);
        return Ok(new Generated.MessageResponse("Sesión cerrada."));
    }

    private static UserType ParseUserType(string value)
    {
        return string.Equals(value, "competidor", StringComparison.OrdinalIgnoreCase)
            ? UserType.Competidor
            : UserType.Espectador;
    }

    private static PreferredLanguage ParsePreferredLanguage(string? value)
    {
        return ParseOptional<PreferredLanguage>(value) ?? PreferredLanguage.Espanol;
    }

    private static TEnum? ParseOptional<TEnum>(string? value) where TEnum : struct
    {
        return Enum.TryParse<TEnum>(value, true, out var parsed) ? parsed : null;
    }
}


public sealed class RegisterFormRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public string Apellido { get; init; } = string.Empty;
    public string Tipo { get; init; } = string.Empty;
    public string? Pais { get; init; }
    public string? IdiomaPreferido { get; init; }
    public bool Newsletter { get; init; }
    public bool Terminos { get; init; }
    public bool Reglamento { get; init; }
    public DateTimeOffset? FechaNacimiento { get; init; }
    public string? Genero { get; init; }
    public string? Telefono { get; init; }
    public string? Club { get; init; }
    public string? Postura { get; init; }
    public string? TallaCamiseta { get; init; }
    public string? Federacion { get; init; }
    public string? Patrocinadores { get; init; }
    public IFormFile? IdentityDocument { get; init; }
}
