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
            // Get user's watched movies with ratings
            var watchedMovies = await _context.UserMovies
                .Where(um => um.UserId == userId)
                .Include(um => um.Movie)
                .Include(um => um.Rating)
                .OrderByDescending(um => um.WatchedAt)
                .Take(50) // Analyze last 50 movies for better insights
                .Select(um => new UserMovieTasteDto
                {
                    MovieName = um.Movie.Name,
                    Genre = um.Movie.Genre.ToString(),
                    ReleaseYear = um.Movie.ReleaseYear,
                    Director = um.Movie.Director,
                    UserRating = um.Rating != null ? um.Rating.Score : null,
                    UserComment = um.Rating != null ? um.Rating.Comment : null,
                    WatchedAt = um.WatchedAt
                })
                .ToListAsync();

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

        var stats = await _context.UserMovies
            .Where(um => um.UserId == userId)
            .Include(um => um.Movie)
            .Include(um => um.Rating)
            .ToListAsync();

        var ratedMovies = stats.Where(um => um.Rating != null).ToList();

        var summary = new
        {
            totalWatched = stats.Count,
            totalRated = ratedMovies.Count,
            averageRating = ratedMovies.Any() ? Math.Round(ratedMovies.Average(um => um.Rating!.Score), 1) : 0,
            favoriteGenre = stats
                .GroupBy(um => um.Movie.Genre)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key.ToString() ?? "None yet",
            recentlyWatched = stats
                .OrderByDescending(um => um.WatchedAt)
                .Take(5)
                .Select(um => new
                {
                    um.Movie.Name,
                    um.Movie.Genre,
                    Rating = um.Rating?.Score,
                    um.WatchedAt
                })
                .ToList()
        };

        return Ok(summary);
    }
}