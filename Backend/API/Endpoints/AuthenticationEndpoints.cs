using Application.Authentication.Commands;
using Application.Authentication.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Endpoints;

public static class AuthenticationEndpoints
{
    public static void MapAuthenticationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/auth").WithTags("Authentication");

        group.MapPost("register", async ([FromBody] RegisterUserCommand command, [FromServices] AuthenticationService authService, CancellationToken cancellationToken) =>
        {
            var result = await authService.RegisterAsync(command, cancellationToken);
            return result.IsFailure ? Results.BadRequest(new { Error = result.Error }) : Results.Ok(new { UserId = result.Value });
        });

        group.MapPost("login", async ([FromBody] LoginCommand command, [FromServices] AuthenticationService authService, CancellationToken cancellationToken) =>
        {
            var result = await authService.LoginAsync(command, cancellationToken);
            return result.IsFailure ? Results.BadRequest(new { Error = result.Error }) : Results.Ok(result.Value);
        });

        group.MapPost("forgot-password", async ([FromBody] RequestPasswordResetCommand command, [FromServices] ForgotPasswordService forgotPasswordService, CancellationToken cancellationToken) =>
        {
            var result = await forgotPasswordService.RequestPasswordResetAsync(command, cancellationToken);
            return result.IsFailure ? Results.BadRequest(new { Error = result.Error }) : Results.Ok();
        });

        group.MapPost("reset-password", async ([FromBody] ResetPasswordCommand command, [FromServices] ForgotPasswordService forgotPasswordService, CancellationToken cancellationToken) =>
        {
            var result = await forgotPasswordService.ResetPasswordAsync(command, cancellationToken);
            return result.IsFailure ? Results.BadRequest(new { Error = result.Error }) : Results.Ok();
        });
    }
}
