using System.Security.Claims;
using Filmder.Data;
using Filmder.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Filmder.Controllers;

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
            // Get recent user activity (last 30 days or last 20 movies)
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            
            var recentActivity = await _context.UserMovies
                .Where(um => um.UserId == userId && um.WatchedAt >= thirtyDaysAgo)
                .Include(um => um.Movie)
                .Include(um => um.Rating)
                .OrderByDescending(um => um.WatchedAt)
                .Take(20)
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

            // If not enough recent activity, get their highest-rated movies
            if (recentActivity.Count < 5)
            {
                recentActivity = await _context.UserMovies
                    .Where(um => um.UserId == userId && um.Rating != null)
                    .Include(um => um.Movie)
                    .Include(um => um.Rating)
                    .OrderByDescending(um => um.Rating!.Score)
                    .ThenByDescending(um => um.WatchedAt)
                    .Take(20)
                    .Select(um => new UserMovieTasteDto
                    {
                        MovieName = um.Movie.Name,
                        Genre = um.Movie.Genre.ToString(),
                        ReleaseYear = um.Movie.ReleaseYear,
                        Director = um.Movie.Director,
                        UserRating = um.Rating!.Score,
                        UserComment = um.Rating.Comment,
                        WatchedAt = um.WatchedAt
                    })
                    .ToListAsync();
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
                // Try to find movie in database by name and year
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
            var favoriteGenres = await _context.UserMovies
                .Where(um => um.UserId == userId && um.Rating != null && um.Rating.Score >= 7)
                .Include(um => um.Movie)
                .GroupBy(um => um.Movie.Genre)
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

            // FIX: Get movies first, then randomize in memory
            var quickPicks = await _context.Movies
                .Where(m => !watchedIds.Contains(m.Id))
                .Where(m => favoriteGenres.Contains(m.Genre))
                .Where(m => m.Rating >= 7.5)
                .ToListAsync();  // Load to memory first

            // Now randomize in memory
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