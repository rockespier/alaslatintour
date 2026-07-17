using AlasApp.Api.Models;
using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.AdminUsers.Commands.DeleteAdminUser;
using AlasApp.Application.Auth.Commands.ChangeUserPassword;
using AlasApp.Application.AdminUsers.Queries.GetAdminUserById;
using AlasApp.Application.AdminUsers.Queries.ListAdminUsers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Generated = AlasApp.AlasApi.Api.Controllers;
using AlasApp.Api.Authorization;

namespace AlasApp.Api.Controllers;

[ApiController]
[Route("v1/admin/users")]
[Authorize]
public sealed class AdminUsersController(IRequestDispatcher dispatcher) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AdminPolicies.UsersRead)]
    [ProducesResponseType(typeof(Generated.AdminUserListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<Generated.AdminUserListResponse>> List(CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new ListAdminUsersQuery(), cancellationToken);
        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpGet("{userId}")]
    [Authorize(Policy = AdminPolicies.UsersRead)]
    [ProducesResponseType(typeof(Generated.AdminUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Generated.AdminUserResponse>> GetById(string userId, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetAdminUserByIdQuery(ResolveRequestedUserId(userId, User)), cancellationToken);
        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpPost]
    [Authorize(Policy = AdminPolicies.UsersWrite)]
    [ProducesResponseType(typeof(Generated.AdminUserResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<Generated.AdminUserResponse>> Create([FromBody] Generated.AdminUserRequest body, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(ApiContractMapper.ToCommand(body), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { userId = result.Id.ToString() }, ApiContractMapper.ToContract(result));
    }

    [HttpPut("{userId}")]
    [Authorize(Policy = AdminPolicies.UsersWrite)]
    [ProducesResponseType(typeof(Generated.AdminUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Generated.AdminUserResponse>> Update(string userId, [FromBody] Generated.AdminUserUpdateRequest body, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(ApiContractMapper.ToCommand(ResolveRequestedUserId(userId, User), body), cancellationToken);
        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpDelete("{userId}")]
    [Authorize(Policy = AdminPolicies.UsersWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(string userId, CancellationToken cancellationToken)
    {
        var currentUserId = TryParseCurrentUserId(User);
        await dispatcher.Send(new DeleteAdminUserCommand(ResolveRequestedUserId(userId, User), currentUserId), cancellationToken);
        return NoContent();
    }

    [HttpPost("{userId}/password")]
    [Authorize(Policy = AdminPolicies.UsersWrite)]
    [ProducesResponseType(typeof(Generated.MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Generated.MessageResponse>> ChangePassword(
        string userId,
        [FromBody] PasswordChangeRequest body,
        CancellationToken cancellationToken)
    {
        await dispatcher.Send(
            new ChangeUserPasswordCommand(ResolveRequestedUserId(userId, User), body.NewPassword),
            cancellationToken);

        return Ok(new Generated.MessageResponse("Contraseña actualizada correctamente."));
    }

    [HttpPost("me/password")]
    [ProducesResponseType(typeof(Generated.MessageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<Generated.MessageResponse>> ChangeOwnPassword(
        [FromBody] PasswordChangeRequest body,
        CancellationToken cancellationToken)
    {
        var currentUserId = TryParseCurrentUserId(User)
            ?? throw new UnauthorizedAccessException("No se pudo identificar el usuario autenticado.");

        await dispatcher.Send(
            new ChangeUserPasswordCommand(currentUserId, body.NewPassword),
            cancellationToken);

        return Ok(new Generated.MessageResponse("Contraseña actualizada correctamente."));
    }

    private static Guid ResolveRequestedUserId(string userId, ClaimsPrincipal principal)
    {
        if (string.Equals(userId, "me", StringComparison.OrdinalIgnoreCase))
        {
            return TryParseCurrentUserId(principal)
                ?? throw new UnauthorizedAccessException("No se pudo identificar el usuario autenticado.");
        }

        return ApiContractMapper.ParseGuid(userId, "userId");
    }

    private static Guid? TryParseCurrentUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
