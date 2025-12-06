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
public class PersonalizedPlaylistController : ControllerBase
{
    private readonly IAIService _aiService;
    private readonly AppDbContext _context;

    public PersonalizedPlaylistController(IAIService aiService, AppDbContext context)
    {
        _aiService = aiService;
        _context = context;
    }

    [HttpGet("generate")]
    public async Task<ActionResult<PersonalizedPlaylistDto>> GeneratePlaylist([FromQuery] int count = 10)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        if (count < 5 || count > 30)
        {
            return BadRequest(new { message = "Count must be between 5 and 30" });
        }

        try
        {
            // Get all user ratings (ratings exist independently of UserMovies)
            var userRatings = await _context.Ratings
                .Where(r => r.UserId == userId)
                .ToDictionaryAsync(r => r.MovieId);

            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            
            // Get recent user activity
            var recentUserMovies = await _context.UserMovies
                .Where(um => um.UserId == userId && um.WatchedAt >= thirtyDaysAgo)
                .Include(um => um.Movie)
                .OrderByDescending(um => um.WatchedAt)
                .Take(20)
                .ToListAsync();

            var recentActivity = recentUserMovies.Select(um => new UserMovieTasteDto
            {
                MovieName = um.Movie.Name,
                Genre = um.Movie.Genre.ToString(),
                ReleaseYear = um.Movie.ReleaseYear,
                Director = um.Movie.Director,
                UserRating = userRatings.TryGetValue(um.MovieId, out var rating) ? rating.Score : null,
                UserComment = userRatings.GetValueOrDefault(um.MovieId)?.Comment,
                WatchedAt = um.WatchedAt
            }).ToList();

            // If not enough recent activity, get all UserMovies with their ratings
            if (recentActivity.Count < 5)
            {
                var userMoviesWithRatings = await _context.UserMovies
                    .Where(um => um.UserId == userId)
                    .Include(um => um.Movie)
                    .OrderByDescending(um => um.WatchedAt)
                    .Take(20)
                    .ToListAsync();

                recentActivity = userMoviesWithRatings
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
                    .OrderByDescending(m => m.UserRating ?? 0)
                    .ThenByDescending(m => m.WatchedAt)
                    .ToList();
            }
            
            if (!recentActivity.Any() && userRatings.Any())
            {
                var ratedMovieIds = userRatings.Keys.ToList();
                var ratedMovies = await _context.Movies
                    .Where(m => ratedMovieIds.Contains(m.Id))
                    .ToListAsync();

                recentActivity = ratedMovies
                    .Select(m => new UserMovieTasteDto
                    {
                        MovieName = m.Name,
                        Genre = m.Genre.ToString(),
                        ReleaseYear = m.ReleaseYear,
                        Director = m.Director,
                        UserRating = userRatings[m.Id].Score,
                        UserComment = userRatings[m.Id].Comment,
                        WatchedAt = userRatings[m.Id].CreatedAt
                    })
                    .OrderByDescending(m => m.UserRating)
                    .Take(20)
                    .ToList();
            }

            if (!recentActivity.Any())
            {
                return NotFound(new { message = "Not enough viewing history. Watch and rate some movies to get personalized recommendations!" });
            }

            // Generate AI playlist
            var playlist = await _aiService.GeneratePersonalizedPlaylist(recentActivity, count);

            // Try to match recommended movies to actual database movies
            var watchedMovieIds = await _context.UserMovies
                .Where(um => um.UserId == userId)
                .Select(um => um.MovieId)
                .ToHashSetAsync();

            foreach (var playlistMovie in playlist.Movies)
            {
                var dbMovie = await _context.Movies
                    .Where(m => !watchedMovieIds.Contains(m.Id))
                    .Where(m => m.Name.Contains(playlistMovie.MovieName) || 
                               playlistMovie.MovieName.Contains(m.Name))
                    .Where(m => Math.Abs(m.ReleaseYear - playlistMovie.ReleaseYear) <= 1)
                    .FirstOrDefaultAsync();

                if (dbMovie != null)
                {
                    playlistMovie.MovieId = dbMovie.Id;
                    playlistMovie.MovieName = dbMovie.Name;
                    playlistMovie.Genre = dbMovie.Genre.ToString();
                    playlistMovie.ReleaseYear = dbMovie.ReleaseYear;
                    playlistMovie.Rating = dbMovie.Rating;
                    playlistMovie.PosterUrl = dbMovie.PosterUrl ?? "";
                }
            }

            return Ok(playlist);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to generate playlist", details = ex.Message });
        }
    }

    [HttpGet("quick-picks")]
    public async Task<ActionResult> GetQuickPicks()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        try
        {
            var highRatedMovieIds = await _context.Ratings
                .Where(r => r.UserId == userId && r.Score >= 7)
                .Select(r => r.MovieId)
                .ToListAsync();

            var favoriteGenres = await _context.Movies
                .Where(m => highRatedMovieIds.Contains(m.Id))
                .GroupBy(m => m.Genre)
                .OrderByDescending(g => g.Count())
                .Take(3)
                .Select(g => g.Key)
                .ToListAsync();

            if (!favoriteGenres.Any())
            {
                return NotFound(new { message = "Not enough data for quick picks. Rate some movies first!" });
            }

            var watchedIds = await _context.UserMovies
                .Where(um => um.UserId == userId)
                .Select(um => um.MovieId)
                .ToHashSetAsync();

            var quickPicks = await _context.Movies
                .Where(m => !watchedIds.Contains(m.Id))
                .Where(m => favoriteGenres.Contains(m.Genre))
                .Where(m => m.Rating >= 7.5)
                .ToListAsync();

            var randomPicks = quickPicks
                .OrderBy(m => Guid.NewGuid())
                .Take(5)
                .Select(m => new
                {
                    m.Id,
                    m.Name,
                    Genre = m.Genre.ToString(),
                    m.ReleaseYear,
                    m.Rating,
                    m.PosterUrl,
                    m.Duration,
                    m.Director
                })
                .ToList();

            return Ok(new
            {
                title = "Quick Picks For You",
                description = $"Based on your love for {string.Join(", ", favoriteGenres.Select(g => g.ToString()))} movies",
                movies = randomPicks
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to get quick picks", details = ex.Message });
        }
    }
}