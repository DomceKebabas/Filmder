using System.Security.Claims;
using Filmder.Data;
using Filmder.DTOs;
using Filmder.Models;
using Filmder.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Filmder.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("DefaultBucket")]
public class UserController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _dbContext;
    private readonly SupabaseService _storage;

    public UserController(UserManager<AppUser> userManager, AppDbContext dbContext, SupabaseService storage)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _storage = storage;
    }

    
    [HttpGet("profile")]
    public async Task<ActionResult<UserProfileDto>> ReturnLoggedInUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        return new UserProfileDto
        {
            Id = user.Id,
            Username = user.UserName,
            Email = user.Email,
            ProfilePictureUrl = user.ProfilePictureUrl
        };
    }
    
[HttpGet("stats")]
public async Task<ActionResult<UserStatsDto>> GetLoggedInUserStatsAsync()
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId == null) return BadRequest();

    // Get ratings directly
    var ratings = await _dbContext.Ratings
        .Where(r => r.UserId == userId)
        .Include(r => r.Movie)
        .ToListAsync();

    // Get liked swipes
    var likedSwipes = await _dbContext.SwipeHistories
        .Where(sh => sh.UserId == userId && sh.IsLike)
        .Include(sh => sh.Movie)
        .ToListAsync();

    // Get UserMovies
    var userMovies = await _dbContext.UserMovies
        .Where(um => um.UserId == userId)
        .Include(um => um.Movie)
        .Include(um => um.Rating)
        .ToListAsync();

    // Combine all unique movies
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

    // Get genres from all sources
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
    
    
    [HttpPost("watch")]
    public async Task<IActionResult> AddMovieToUser(AddMovieRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();

        var movie = await _dbContext.Movies.FindAsync(request.MovieId);
        if (movie == null) return NotFound();

        var existing = await _dbContext.UserMovies
            .FirstOrDefaultAsync(um => um.UserId == userId && um.MovieId == request.MovieId);

        if (existing != null) return NoContent();

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

            _dbContext.Ratings.Add(rating);
            await _dbContext.SaveChangesAsync();
        }

        var userMovie = new UserMovie
        {
            UserId = userId,
            MovieId = request.MovieId,
            WatchedAt = DateTime.UtcNow,
            RatingId = rating?.Id
        };

        _dbContext.UserMovies.Add(userMovie);
        await _dbContext.SaveChangesAsync();

        return Ok();
    }
    
    [HttpPost("upload-profile-picture")]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        var allowedTypes = new[]
        {
            "image/jpeg",
            "image/png",
            "image/gif",
            "image/webp",
            "image/svg+xml"
        };

        if (!allowedTypes.Contains(file.ContentType.ToLower()))
            return BadRequest("Invalid file type");

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest("File size must be under 5MB");

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

        if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
        {
            await _storage.DeleteProfilePictureByUrlAsync(
                user.ProfilePictureUrl
            );
        }

        var url = await _storage.UploadProfilePictureAsync(userId, file);
        if (url == null)
            return StatusCode(500, "Upload failed");

        user.ProfilePictureUrl = url;
        await _userManager.UpdateAsync(user);

        return Ok(new
        {
            message = "Profile picture uploaded successfully",
            profilePictureUrl = url
        });
    }

    [HttpDelete("profile-picture")]
    public async Task<IActionResult> DeleteProfilePicture()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

        if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
        {
            await _storage.DeleteProfilePictureByUrlAsync(
                user.ProfilePictureUrl
            );

            user.ProfilePictureUrl = null;
            await _userManager.UpdateAsync(user);
        }

        return Ok(new
        {
            message = "Profile picture removed successfully"
        });
    }
}

