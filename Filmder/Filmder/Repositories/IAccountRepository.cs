using Filmder.Models;
using Microsoft.AspNetCore.Identity;

namespace Filmder.Interfaces;

public interface IAccountRepository
{
    Task<AppUser?> FindByUsernameAsync(string username);
    Task<AppUser?> FindByEmailAsync(string email);
    Task<AppUser?> FindByIdAsync(string userId);

    Task<IdentityResult> CreateUserAsync(AppUser user, string password);
    Task<string> GenerateEmailConfirmationTokenAsync(AppUser user);
    Task<IdentityResult> ConfirmEmailAsync(AppUser user, string token);

    Task<SignInResult> CheckPasswordAsync(AppUser user, string password);

    Task<string> GeneratePasswordResetTokenAsync(AppUser user);
    Task<IdentityResult> ResetPasswordAsync(AppUser user, string token, string newPassword);
}