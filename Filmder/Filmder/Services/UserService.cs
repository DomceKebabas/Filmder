using Filmder.DTOs;
using Filmder.Models;
using Filmder.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace Filmder.Services;

public class UserService : IUserService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IUserRepository _userRepository;
    private readonly IMovieRepository _movieRepository;
    private readonly ISwipeRepository _swipeRepository;
    private readonly SupabaseService _storage;

    private static readonly string[] AllowedFileTypes =
    {
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp",
        "image/svg+xml"
    };

    public UserService(
        UserManager<AppUser> userManager,
        IUserRepository userRepository,
        IMovieRepository movieRepository,
        ISwipeRepository swipeRepository,
        SupabaseService storage)
    {
        _userManager = userManager;
        _userRepository = userRepository;
        _movieRepository = movieRepository;
        _swipeRepository = swipeRepository;
        _storage = storage;
    }

    public async Task<UserProfileDto?> GetUserProfileAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return null;

        return new UserProfileDto
        {
            Id = user.Id,
            Username = user.UserName!,
            Email = user.Email!,
            ProfilePictureUrl = user.ProfilePictureUrl
        };
    }

    public async Task<UserStatsDto> GetUserStatsAsync(string userId)
    {
        var ratings = await _userRepository.GetRatingsWithMovieAsync(userId);
        var likedSwipes = await _swipeRepository.GetLikedSwipesWithMovieAsync(userId);
        var userMovies = await _userRepository.GetUserMoviesWithDetailsAsync(userId);

        var allMovieIds = ratings.Select(r => r.MovieId)
            .Union(likedSwipes.Select(s => s.MovieId))
            .Union(userMovies.Select(um => um.MovieId))
            .Distinct()
            .ToList();

        int totalMoviesWatched = allMovieIds.Count;
        int totalRatings = ratings.Count;

        double? averageRating = null;
        if (totalRatings > 0)
        {
            averageRating = ratings.Average(r => r.Score);
        }

        var allMovies = ratings.Select(r => r.Movie)
            .Union(likedSwipes.Select(s => s.Movie))
            .Union(userMovies.Select(um => um.Movie))
            .Where(m => m != null)
            .DistinctBy(m => m.Id)
            .ToList();

        var topGenres = allMovies
            .GroupBy(m => m.Genre)
            .OrderByDescending(g => g.Count())
            .Take(3)
            .Select(g => g.Key.ToString())
            .ToList();

        var favoriteMovies = ratings
            .OrderByDescending(r => r.Score)
            .Take(5)
            .Select(r => new FavoriteMovieDto
            {
                MovieId = r.MovieId,
                Title = r.Movie.Name,
                Score = r.Score,
                PosterUrl = r.Movie.PosterUrl
            })
            .ToList();

        return new UserStatsDto
        {
            TotalMoviesWatched = totalMoviesWatched,
            TotalRatings = totalRatings,
            AverageRating = averageRating,
            TopGenres = topGenres,
            FavoriteMovies = favoriteMovies
        };
    }

    public async Task<(bool success, string? message)> AddMovieToUserAsync(string userId, AddMovieRequest request)
    {
        var movie = await _movieRepository.GetByIdAsync(request.MovieId);
        if (movie == null)
        {
            return (false, "Movie not found");
        }

        var existing = await _userRepository.GetUserMovieAsync(userId, request.MovieId);
        if (existing != null)
        {
            return (true, null);
        }

        Rating? rating = null;

        if (request.RatingScore.HasValue)
        {
            rating = new Rating
            {
                UserId = userId,
                MovieId = request.MovieId,
                Score = request.RatingScore.Value,
                Comment = request.Comment
            };

            await _userRepository.AddRatingAsync(rating);
        }

        var userMovie = new UserMovie
        {
            UserId = userId,
            MovieId = request.MovieId,
            WatchedAt = DateTime.UtcNow,
            RatingId = rating?.Id
        };

        await _userRepository.AddUserMovieAsync(userMovie);

        return (true, "Movie added successfully");
    }

    public async Task<(bool success, string? message, string? url)> UploadProfilePictureAsync(string userId, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return (false, "No file uploaded", null);
        }

        if (!AllowedFileTypes.Contains(file.ContentType.ToLower()))
        {
            return (false, "Invalid file type", null);
        }

        if (file.Length > 5 * 1024 * 1024)
        {
            return (false, "File size must be under 5MB", null);
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return (false, "User not found", null);
        }

        if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
        {
            await _storage.DeleteProfilePictureByUrlAsync(user.ProfilePictureUrl);
        }

        var url = await _storage.UploadProfilePictureAsync(userId, file);
        if (url == null)
        {
            return (false, "Upload failed", null);
        }

        user.ProfilePictureUrl = url;
        await _userManager.UpdateAsync(user);

        return (true, "Profile picture uploaded successfully", url);
    }

    public async Task<(bool success, string? message)> DeleteProfilePictureAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return (false, "User not found");
        }

        if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
        {
            await _storage.DeleteProfilePictureByUrlAsync(user.ProfilePictureUrl);
            user.ProfilePictureUrl = null;
            await _userManager.UpdateAsync(user);
        }

        return (true, "Profile picture removed successfully");
    }
}