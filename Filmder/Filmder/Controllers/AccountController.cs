using Filmder.DTOs;
using Filmder.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Filmder.Controllers;

[EnableRateLimiting("SlidingLimiter")]
[ApiController]
public class AccountController(IAccountService accountService) : ControllerBase
{
    [EnableRateLimiting("ExpensiveDaily")]
    [HttpPost]
    [Route("register")]
    public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
    {
        var user = await accountService.RegisterAsync(
            registerDto,
            Request.Scheme,
            Url
        );

        return Ok(user);
    }

    [HttpGet]
    [Route("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(string userId, string token)
    {
        await accountService.ConfirmEmailAsync(userId, token);
        return Ok("Email confirmed successfully!");
    }

    [HttpPost]
    [Route("login")]
    public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
    {
        var user = await accountService.LoginAsync(loginDto);
        return Ok(user);
    }

    [HttpPost]
    [Route("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
    {
        await accountService.ForgotPasswordAsync(dto);
        return Ok(new { message = "If this email exists, a reset link has been sent." });
    }

    [HttpPost]
    [Route("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
    {
        await accountService.ResetPasswordAsync(dto);
        return Ok("Password reset successful.");
    }
}