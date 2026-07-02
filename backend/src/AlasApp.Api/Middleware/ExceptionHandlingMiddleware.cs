using AlasApp.Application.Common;
using Microsoft.Extensions.Hosting;

namespace AlasApp.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException exception)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "VALIDATION_ERROR",
                message = exception.Message,
                fields = exception.Errors.Select(x => new { field = x.Field, message = x.Message }),
                timestamp = DateTimeOffset.UtcNow
            });
        }
        catch (NotFoundException exception)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "NOT_FOUND",
                message = exception.Message,
                timestamp = DateTimeOffset.UtcNow
            });
        }
        catch (ConflictException exception)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "CONFLICT",
                message = exception.Message,
                timestamp = DateTimeOffset.UtcNow
            });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled exception while processing request.");

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "INTERNAL_SERVER_ERROR",
                message = "Ocurrio un error inesperado.",
                detail = environment.IsDevelopment() || environment.IsEnvironment("Testing") ? exception.ToString() : null,
                timestamp = DateTimeOffset.UtcNow
            });
        }
    }
}
