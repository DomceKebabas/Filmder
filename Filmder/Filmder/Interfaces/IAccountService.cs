using Filmder.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Filmder.Interfaces;

public interface IAccountService
{
    Task<UserDto> RegisterAsync(RegisterDto registerDto, string scheme, IUrlHelper urlHelper);
    Task ConfirmEmailAsync(string userId, string token);
    Task<UserDto> LoginAsync(LoginDto loginDto);
    Task ForgotPasswordAsync(ForgotPasswordDto dto);
    Task ResetPasswordAsync(ResetPasswordDto dto);
}