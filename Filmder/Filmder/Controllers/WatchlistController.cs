using System.Security.Claims;
using Filmder.Data;
using Filmder.DTOs;
using Filmder.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Filmder.Controllers;
[EnableRateLimiting("DefaultBucket")]
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WatchlistController : ControllerBase
{
    private readonly AppDbContext _context;

    public WatchlistController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("generate")]
    public async Task<ActionResult<List<WatchlistMovieDto>>> GenerateWatchlist([FromQuery] int count = 20)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var genreScores = new Dictionary<MovieGenre, double>();
        foreach (MovieGenre genre in Enum.GetValues(typeof(MovieGenre)))
        {
            genreScores[genre] = 0;
        }

        var ratings = await _context.Ratings
            .Include(r => r.Movie)
            .Where(r => r.UserId == userId && r.Score >= 7)
            .ToListAsync();

        foreach (var rating in ratings)
        {
            if (rating.Movie != null)
            {
                genreScores[rating.Movie.Genre] += (rating.Score - 6) * 2.0;
            }
        }

        var gameVotes = await _context.MovieScores
            .Include(ms => ms.Movie)
            .Include(ms => ms.Game)
            .Where(ms => ms.Game != null && ms.Game.Users.Any(u => u.Id == userId) && ms.MovieScoreValue > 50)
            .ToListAsync();

        foreach (var vote in gameVotes)
        {
            if (vote.Movie != null && vote.Game != null)
            {
                genreScores[vote.Movie.Genre] += (vote.MovieScoreValue / 100.0);
            }
        }

        var topGenres = genreScores
            .Where(kvp => kvp.Value > 0)
            .OrderByDescending(kvp => kvp.Value)
            .Take(3)
            .Select(kvp => kvp.Key)
            .ToList();

        var seenMovieIds = await _context.Ratings
            .Where(r => r.UserId == userId)
            .Select(r => r.MovieId)
            .Union(_context.SwipeHistories
                .Where(sh => sh.UserId == userId)
                .Select(sh => sh.MovieId))
            .ToHashSetAsync();

        List<Movie> movies;

        if (!topGenres.Any())
        {
            movies = await _context.Movies
                .Where(m => !seenMovieIds.Contains(m.Id))
                .OrderByDescending(m => m.Rating)
                .Take(count)
                .ToListAsync();
        }
        else
        {
            movies = await _context.Movies
                .Where(m => !seenMovieIds.Contains(m.Id) && topGenres.Contains(m.Genre))
                .OrderByDescending(m => m.Rating)
                .Take(count)
                .ToListAsync();
        }

        var watchlist = movies.Select(m => new WatchlistMovieDto
        {
            Id = m.Id,
            Name = m.Name,
            Genre = m.Genre.ToString(),
            Description = m.Description,
            ReleaseYear = m.ReleaseYear,
            Rating = m.Rating,
            PosterUrl = m.PosterUrl ?? string.Empty,
            TrailerUrl = m.TrailerUrl ?? string.Empty,
            Duration = m.Duration,
            Director = m.Director,
            Cast = m.Cast,
            RecommendationScore = CalculateScore(m, genreScores.GetValueOrDefault(m.Genre, 0))
        }).ToList();

        return Ok(watchlist);
    }

    [HttpGet("preferences")]
    public async Task<ActionResult<UserPreferencesDto>> GetUserPreferences()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var genreScores = new Dictionary<string, double>();

        var ratings = await _context.Ratings
            .Include(r => r.Movie)
            .Where(r => r.UserId == userId)
            .ToListAsync();

        foreach (var rating in ratings)
        {
            if (rating.Movie == null) continue;
            
            var genre = rating.Movie.Genre.ToString();
            if (!genreScores.ContainsKey(genre))
                genreScores[genre] = 0;
            
            if (rating.Score >= 7)
                genreScores[genre] += (rating.Score - 6) * 2.0;
        }

        var gameVotes = await _context.MovieScores
            .Include(ms => ms.Movie)
            .Include(ms => ms.Game)
            .Where(ms => ms.Game != null && ms.Game.Users.Any(u => u.Id == userId) && ms.MovieScoreValue > 50)
            .ToListAsync();

        foreach (var vote in gameVotes)
        {
            if (vote.Movie == null || vote.Game == null) continue;
            
            var genre = vote.Movie.Genre.ToString();
            if (!genreScores.ContainsKey(genre))
                genreScores[genre] = 0;
            genreScores[genre] += (vote.MovieScoreValue / 100.0);
        }

        var favoriteGenre = genreScores
            .OrderByDescending(kvp => kvp.Value)
            .FirstOrDefault();

        return Ok(new UserPreferencesDto
        {
            FavoriteGenre = favoriteGenre.Key ?? "None",
            GenreScores = genreScores
                .OrderByDescending(kvp => kvp.Value)
                .ToDictionary(kvp => kvp.Key, kvp => Math.Round(kvp.Value, 2)),
            TotalRatings = ratings.Count,
            TotalGameVotes = gameVotes.Count
        });
    }

    private double CalculateScore(Movie movie, double genreScore)
    {
        var normalizedGenreScore = Math.Min(genreScore / 10.0, 10.0);
        return Math.Round((movie.Rating * 0.6 + normalizedGenreScore * 0.4) * 10, 2);
    }
}