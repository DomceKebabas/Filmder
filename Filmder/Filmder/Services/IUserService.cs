using Filmder.DTOs;
using Microsoft.AspNetCore.Http;

namespace Filmder.Services;

public interface IUserService
{
    Task<UserProfileDto?> GetUserProfileAsync(string userId);
    Task<UserStatsDto> GetUserStatsAsync(string userId);
    Task<(bool success, string? message)> AddMovieToUserAsync(string userId, AddMovieRequest request);
    Task<(bool success, string? message, string? url)> UploadProfilePictureAsync(string userId, IFormFile file);
    Task<(bool success, string? message)> DeleteProfilePictureAsync(string userId);
}