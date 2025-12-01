using Filmder.DTOs;
using Filmder.Exceptions;
using Filmder.Models;
using Filmder.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Filmder.Controllers;
[ApiController]
public class AccountController(UserManager<AppUser> userManager, SignInManager<AppUser>signInManager,ITokenService tokenService) : ControllerBase
{
    [EnableRateLimiting("ExpensiveDaily")]
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

        return Ok(new UserDto(user.Id, user.Email, tokenService.CreateToken(user)));
    }
    
    [EnableRateLimiting("SlidingLimiter")]
    [HttpPost]
    [Route("login")]
    public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
    {
        var user = await userManager.FindByEmailAsync(loginDto.Email);

        if (user == null)
            throw new LoginFailedException("User with this email does not exist.");

        var result = await signInManager.CheckPasswordSignInAsync(
            user, loginDto.Password, false
        );

        if (!result.Succeeded)
            throw new LoginFailedException("Incorrect password.");

        return Ok(new UserDto(user.Id, user.Email!, tokenService.CreateToken(user)));
    }
    
}