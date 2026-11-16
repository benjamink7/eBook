using eBook.API.Configuration;
using eBook.Model.Auth;
using eBook.Model.Common;
using eBook.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace eBook.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        return Ok(await _authService.RegisterAsync(request));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        return Ok(await _authService.LoginAsync(request));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync();
        return NoContent();
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult<AuthResponse>> ChangePassword(ChangePasswordRequest request)
    {
        return Ok(await _authService.ChangePasswordAsync(request));
    }

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [HttpPost("forgot-password")]
    public async Task<ActionResult<MessageResponse>> ForgotPassword(ForgotPasswordRequest request)
    {
        await _authService.ForgotPasswordAsync(request);

        return Ok(new MessageResponse(
            "Ako nalog s unesenom e-mail adresom postoji, poslali smo vam kod za reset lozinke."));
    }

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [HttpPost("reset-password")]
    public async Task<ActionResult<MessageResponse>> ResetPassword(ResetPasswordRequest request)
    {
        await _authService.ResetPasswordAsync(request);

        return Ok(new MessageResponse("Lozinka je uspješno promijenjena. Sada se možete prijaviti."));
    }
}
