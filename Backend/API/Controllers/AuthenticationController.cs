using Application.Authentication.Commands;
using Application.Authentication.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthenticationController : ControllerBase
{
    private readonly AuthenticationService _authService;
    private readonly ForgotPasswordService _forgotPasswordService;

    public AuthenticationController(AuthenticationService authService, ForgotPasswordService forgotPasswordService)
    {
        _authService = authService;
        _forgotPasswordService = forgotPasswordService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(command, cancellationToken);
        return result.IsFailure ? BadRequest(new { Error = result.Error }) : Ok(new { UserId = result.Value });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(command, cancellationToken);
        return result.IsFailure ? BadRequest(new { Error = result.Error }) : Ok(result.Value);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] RequestPasswordResetCommand command, CancellationToken cancellationToken)
    {
        var result = await _forgotPasswordService.RequestPasswordResetAsync(command, cancellationToken);
        return result.IsFailure ? BadRequest(new { Error = result.Error }) : Ok();
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await _forgotPasswordService.ResetPasswordAsync(command, cancellationToken);
        return result.IsFailure ? BadRequest(new { Error = result.Error }) : Ok();
    }
}
