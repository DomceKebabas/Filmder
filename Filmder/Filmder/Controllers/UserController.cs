using System.Security.Claims;
using Filmder.Data;
using Filmder.DTOs;
using Filmder.Models;
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

    public UserController(UserManager<AppUser> userManager, AppDbContext dbContext)
    {
        _userManager = userManager;
        _dbContext = dbContext;
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

        var watchedMovies = await _dbContext.UserMovies
            .Where(um => um.UserId == userId)
            .Include(um => um.Movie)
            .Include(um => um.Rating)
            .ToListAsync();

        if (!watchedMovies.Any())
            return new UserStatsDto();

        int totalMoviesWatched = watchedMovies.Count;
        int totalRatings = watchedMovies.Count(um => um.Rating != null);

        double? averageRating = null;
        if (totalRatings > 0)
        {
            var ratingScores = watchedMovies
                .Where(um => um.Rating != null)
                .Select(um => um.Rating!.Score);

            averageRating = ratingScores.Average();
        }

        var topGenres = watchedMovies
            .Where(um => um.Movie.Genre != null)
            .GroupBy(um => um.Movie.Genre)
            .OrderByDescending(g => g.Count())
            .Take(3)
            .Select(g => g.Key.ToString())  
            .ToList();


        var favoriteMovies = watchedMovies
            .Where(um => um.Rating != null)
            .OrderByDescending(um => um.Rating!.Score)
            .Take(5)
            .Select(um => new FavoriteMovieDto
            {
                MovieId = um.MovieId,
                Title = um.Movie.Name,
                Score = um.Rating.Score,
                PosterUrl = um.Movie.PosterUrl
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
    
}