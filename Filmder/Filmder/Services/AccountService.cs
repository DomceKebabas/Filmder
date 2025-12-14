using Filmder.DTOs;
using Filmder.Exceptions;
using Filmder.Interfaces;
using Filmder.Models;
using Microsoft.AspNetCore.Mvc;

namespace Filmder.Services;

public class AccountService(
    IAccountRepository accountRepository,
    ITokenService tokenService,
    IEmailSender emailSender
) : IAccountService
{
    public async Task<UserDto> RegisterAsync(
        RegisterDto registerDto,
        string scheme,
        IUrlHelper urlHelper)
    {
        var existingUser = await accountRepository.FindByUsernameAsync(registerDto.Username);
        if (existingUser != null)
            throw new Exception("Username is already taken");

        var user = new AppUser
        {
            Email = registerDto.Email,
            UserName = registerDto.Username
        };

        var result = await accountRepository.CreateUserAsync(user, registerDto.Password);
        if (!result.Succeeded)
            throw new Exception("User creation failed");

        var token = await accountRepository.GenerateEmailConfirmationTokenAsync(user);

        var confirmationUrl = urlHelper.Action(
            "ConfirmEmail",
            "Account",
            new { userId = user.Id, token },
            scheme);

        await emailSender.SendEmailAsync(
            user.Email!,
            "Confirm your Filmder account",
            $"Click here to confirm your email: {confirmationUrl}"
        );

        return new UserDto(user.Id, user.Email, tokenService.CreateToken(user));
    }

    public async Task ConfirmEmailAsync(string userId, string token)
    {
        var user = await accountRepository.FindByIdAsync(userId);
        if (user == null)
            throw new Exception("Invalid user.");

        var result = await accountRepository.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
            throw new Exception("Email confirmation failed.");
    }

    public async Task<UserDto> LoginAsync(LoginDto loginDto)
    {
        var user = await accountRepository.FindByEmailAsync(loginDto.Email);
        if (user == null)
            throw new LoginFailedException("User with this email does not exist.");

        var result = await accountRepository.CheckPasswordAsync(user, loginDto.Password);
        if (!result.Succeeded)
            throw new LoginFailedException("Incorrect password.");

        return new UserDto(user.Id, user.Email!, tokenService.CreateToken(user));
    }

    public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        var user = await accountRepository.FindByEmailAsync(dto.Email);
        if (user == null)
            return;

        var token = await accountRepository.GeneratePasswordResetTokenAsync(user);

        var resetUrl =
            $"http://localhost:5173/reset-password?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(token)}";

        await emailSender.SendEmailAsync(
            user.Email!,
            "Reset your Filmder password",
            $"Click to reset your password: {resetUrl}"
        );
    }

    public async Task ResetPasswordAsync(ResetPasswordDto dto)
    {
        var user = await accountRepository.FindByEmailAsync(dto.Email);
        if (user == null)
            throw new Exception("Invalid request.");

        var result = await accountRepository.ResetPasswordAsync(
            user,
            dto.Token,
            dto.NewPassword
        );

        if (!result.Succeeded)
            throw new Exception("Password reset failed.");
    }
}
