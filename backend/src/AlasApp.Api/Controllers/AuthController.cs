using AlasApp.Api.Models;
using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Auth.Commands.ConfirmPasswordReset;
using AlasApp.Application.Auth.Commands.LogoutUser;
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
    [ProducesResponseType(typeof(Generated.RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Generated.RegisterResponse>> Register([FromBody] Generated.RegisterRequest body, CancellationToken cancellationToken)
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
}
