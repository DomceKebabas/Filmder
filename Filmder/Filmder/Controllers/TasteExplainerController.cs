using System.Security.Claims;
using Filmder.Data;
using Filmder.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Filmder.Controllers;
[EnableRateLimiting("ExpensiveDaily")]
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasteExplainerController : ControllerBase
{
    private readonly IAIService _aiService;
    private readonly AppDbContext _context;

    public TasteExplainerController(IAIService aiService, AppDbContext context)
    {
        _aiService = aiService;
        _context = context;
    }

    [HttpGet("explain")]
    public async Task<ActionResult<TasteExplanationDto>> ExplainMyTaste()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        try
        {
            var userRatings = await _context.Ratings
                .Where(r => r.UserId == userId)
                .Include(r => r.Movie)
                .ToDictionaryAsync(r => r.MovieId);

            // Get user's watched movies
            var userMovies = await _context.UserMovies
                .Where(um => um.UserId == userId)
                .Include(um => um.Movie)
                .OrderByDescending(um => um.WatchedAt)
                .Take(50)
                .ToListAsync();

            // Build watched movies list, joining ratings from the Ratings table
            var watchedMovies = userMovies
                .Select(um => new UserMovieTasteDto
                {
                    MovieName = um.Movie.Name,
                    Genre = um.Movie.Genre.ToString(),
                    ReleaseYear = um.Movie.ReleaseYear,
                    Director = um.Movie.Director,
                    UserRating = userRatings.TryGetValue(um.MovieId, out var rating) ? rating.Score : null,
                    UserComment = userRatings.GetValueOrDefault(um.MovieId)?.Comment,
                    WatchedAt = um.WatchedAt
                })
                .ToList();
            
            var userMovieIds = userMovies.Select(um => um.MovieId).ToHashSet();
            var ratingsWithoutUserMovie = userRatings
                .Where(kvp => !userMovieIds.Contains(kvp.Key))
                .Select(kvp => new UserMovieTasteDto
                {
                    MovieName = kvp.Value.Movie.Name,
                    Genre = kvp.Value.Movie.Genre.ToString(),
                    ReleaseYear = kvp.Value.Movie.ReleaseYear,
                    Director = kvp.Value.Movie.Director,
                    UserRating = kvp.Value.Score,
                    UserComment = kvp.Value.Comment,
                    WatchedAt = kvp.Value.CreatedAt
                })
                .ToList();

            watchedMovies.AddRange(ratingsWithoutUserMovie);

            if (!watchedMovies.Any())
            {
                return NotFound(new { message = "No watched movies found. Start watching and rating movies to get your taste analysis!" });
            }

            // Filter to only movies with ratings for better analysis
            var ratedMovies = watchedMovies.Where(m => m.UserRating.HasValue).ToList();
            
            if (!ratedMovies.Any())
            {
                return Ok(new TasteExplanationDto
                {
                    OverallTasteProfile = "You've watched some movies, but haven't rated them yet. Rate your watched movies to get a detailed taste analysis!",
                    WatchingPersonality = "The Silent Watcher - Enjoys movies but keeps opinions private",
                    Insights = new List<TasteInsightDto>
                    {
                        new TasteInsightDto
                        {
                            Category = "Viewing Habits",
                            Explanation = $"You've watched {watchedMovies.Count} movies. Start rating them to unlock detailed insights!",
                            ExampleMovies = watchedMovies.Take(3).Select(m => m.MovieName).ToList()
                        }
                    }
                });
            }

            // Use rated movies for AI analysis (better insights)
            var explanation = await _aiService.ExplainUserTaste(ratedMovies);
            
            return Ok(explanation);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to analyze your taste", details = ex.Message });
        }
    }

    [HttpGet("summary")]
    public async Task<ActionResult> GetTasteSummary()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();
        
        var userRatings = await _context.Ratings
            .Where(r => r.UserId == userId)
            .Include(r => r.Movie)
            .ToListAsync();

        var userMovies = await _context.UserMovies
            .Where(um => um.UserId == userId)
            .Include(um => um.Movie)
            .ToListAsync();
        
        var ratingsByMovieId = userRatings.ToDictionary(r => r.MovieId);

        var summary = new
        {
            totalWatched = userMovies.Count,
            totalRated = userRatings.Count,
            averageRating = userRatings.Any() ? Math.Round(userRatings.Average(r => r.Score), 1) : 0,
            favoriteGenre = userRatings.Any() 
                ? userRatings
                    .GroupBy(r => r.Movie.Genre)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()?.Key.ToString() ?? "None yet"
                : userMovies.Any()
                    ? userMovies
                        .GroupBy(um => um.Movie.Genre)
                        .OrderByDescending(g => g.Count())
                        .FirstOrDefault()?.Key.ToString() ?? "None yet"
                    : "None yet",
            recentlyWatched = userMovies
                .OrderByDescending(um => um.WatchedAt)
                .Take(5)
                .Select(um => new
                {
                    um.Movie.Name,
                    um.Movie.Genre,
                    Rating = ratingsByMovieId.TryGetValue(um.MovieId, out var r) ? r.Score : (int?)null,
                    um.WatchedAt
                })
                .ToList()
        };

        return Ok(summary);
    }
}