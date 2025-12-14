using Filmder.Interfaces;
using Filmder.Models;
using Microsoft.AspNetCore.Identity;

namespace Filmder.Services;

public class AccountRepository(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager
) : IAccountRepository
{
    public async Task<AppUser?> FindByUsernameAsync(string username)
        => await userManager.FindByNameAsync(username);

    public async Task<AppUser?> FindByEmailAsync(string email)
        => await userManager.FindByEmailAsync(email);

    public async Task<AppUser?> FindByIdAsync(string userId)
        => await userManager.FindByIdAsync(userId);

    public async Task<IdentityResult> CreateUserAsync(AppUser user, string password)
        => await userManager.CreateAsync(user, password);

    public async Task<string> GenerateEmailConfirmationTokenAsync(AppUser user)
        => await userManager.GenerateEmailConfirmationTokenAsync(user);

    public async Task<IdentityResult> ConfirmEmailAsync(AppUser user, string token)
        => await userManager.ConfirmEmailAsync(user, token);

    public async Task<SignInResult> CheckPasswordAsync(AppUser user, string password)
        => await signInManager.CheckPasswordSignInAsync(user, password, false);

    public async Task<string> GeneratePasswordResetTokenAsync(AppUser user)
        => await userManager.GeneratePasswordResetTokenAsync(user);

    public async Task<IdentityResult> ResetPasswordAsync(AppUser user, string token, string newPassword)
        => await userManager.ResetPasswordAsync(user, token, newPassword);
}