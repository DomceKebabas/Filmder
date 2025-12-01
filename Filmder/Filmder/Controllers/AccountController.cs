using Filmder.DTOs;
using Filmder.Exceptions;
using Filmder.Interfaces;
using Filmder.Models;
using Filmder.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Filmder.Controllers;
[EnableRateLimiting("SlidingLimiter")]
[ApiController]
public class AccountController(UserManager<AppUser> userManager, SignInManager<AppUser>signInManager,ITokenService tokenService,IEmailSender _emailSender) : ControllerBase
{
    [HttpPost]
    [Route("register")]
    public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
    {
        var existingUser = await userManager.FindByNameAsync(registerDto.Username);
        if (existingUser != null)
        {
            return BadRequest(new { message = "Username is already taken" });
        }

        var user = new AppUser
        {
            Email = registerDto.Email,
            UserName = registerDto.Username
        };
    
        var result = await userManager.CreateAsync(user, registerDto.Password);
   
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }
        
        
       
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);

        var confirmationUrl = Url.Action(
            "ConfirmEmail",
            "Account",
            new { userId = user.Id, token = token },
            Request.Scheme);

        await _emailSender.SendEmailAsync(user.Email!, "Confirm your Filmder account",
            $"Click here to confirm your email: {confirmationUrl}");

        

        return Ok(new UserDto(user.Id, user.Email, tokenService.CreateToken(user)));
    }
    
    [HttpGet]
    [Route("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(string userId, string token)
    {
        var user = await userManager.FindByIdAsync(userId);

        if (user == null)
            return BadRequest("Invalid user.");

        var result = await userManager.ConfirmEmailAsync(user, token);

        if (!result.Succeeded)
            return BadRequest("Email confirmation failed.");

        return Ok("Email confirmed successfully!");
    }
    
   

    [HttpPost]
    [Route("login")]
    public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
    {
        var user = await userManager.FindByEmailAsync(loginDto.Email);

        if (user == null)
            throw new LoginFailedException("User with this email does not exist.");
        
       // if (!await _userManager.IsEmailConfirmedAsync(user))
           // return BadRequest(new { message = "Please confirm your email before logging in." }); 

        var result = await signInManager.CheckPasswordSignInAsync(
            user, loginDto.Password, false
        );

        if (!result.Succeeded)
            throw new LoginFailedException("Incorrect password.");

        return Ok(new UserDto(user.Id, user.Email!, tokenService.CreateToken(user)));
    }
    
    
    [HttpPost]
    [Route("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        var user = await userManager.FindByEmailAsync(dto.Email);

        if (user == null)
            return Ok(new { message = "If this email exists, a reset link has been sent." });

        var token = await userManager.GeneratePasswordResetTokenAsync(user);

        var resetUrl = Url.Action(
            "ResetPassword",
            "Account",
            new { email = user.Email, token = token },
            Request.Scheme);

        await _emailSender.SendEmailAsync(user.Email!, "Reset your Filmder password",
            $"Click to reset your password: {resetUrl}");

        return Ok(new { message = "If this email exists, a reset link has been sent." });
    }

    [HttpPost]
    [Route("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        var user = await userManager.FindByEmailAsync(dto.Email);

        if (user == null)
            return BadRequest("Invalid request.");

        var result = await userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok("Password reset successful.");
    }
    
}