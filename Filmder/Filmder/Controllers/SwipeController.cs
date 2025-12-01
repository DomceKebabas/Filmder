using Filmder.Data;
using Filmder.DTOs;
using Filmder.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;

namespace Filmder.Controllers;
[EnableRateLimiting("DefaultBucket")]
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SwipeController : ControllerBase
{
    private readonly AppDbContext _context;

    public SwipeController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("random")]
    public async Task<ActionResult<Movie>> GetRandomMovie(
        [FromQuery] string? genre = null,
        [FromQuery] int? minYear = null,
        [FromQuery] int? maxDuration = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var swipedMovieIds = await _context.SwipeHistories
            .Where(sh => sh.UserId == userId)
            .Select(sh => sh.MovieId)
            .ToListAsync();

        var query = _context.Movies.AsQueryable();

        if (swipedMovieIds.Any())
        {
            query = query.Where(m => !swipedMovieIds.Contains(m.Id));
        }

        if (!string.IsNullOrEmpty(genre))
        {
            if (Enum.TryParse<MovieGenre>(genre, true, out var parsedGenre))
            {
                query = query.Where(m => m.Genre == parsedGenre);
            }
        }

        if (minYear.HasValue)
        {
            query = query.Where(m => m.ReleaseYear >= minYear.Value);
        }

        if (maxDuration.HasValue)
        {
            query = query.Where(m => m.Duration <= maxDuration.Value);
        }

        var totalMovies = await query.CountAsync();
        
        if (totalMovies == 0)
        {
            return NotFound(new { message = "No more movies to swipe. Try adjusting filters or you've seen them all!" });
        }

        var random = new Random();
        var randomSkip = random.Next(0, totalMovies);
        
        var movie = await query
            .Skip(randomSkip)
            .FirstOrDefaultAsync();

        if (movie == null)
        {
            return NotFound(new { message = "No movies found" });
        }

        return Ok(movie);
    }

    [HttpPost("swipe")]
    public async Task<ActionResult> RecordSwipe([FromBody] SwipeDto swipeDto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var movieExists = await _context.Movies.AnyAsync(m => m.Id == swipeDto.MovieId);
        if (!movieExists)
        {
            return NotFound(new { message = "Movie not found" });
        }

        var existingSwipe = await _context.SwipeHistories
            .FirstOrDefaultAsync(sh => sh.UserId == userId && sh.MovieId == swipeDto.MovieId);

        if (existingSwipe != null)
        {
            return BadRequest(new { message = "You've already swiped on this movie" });
        }

        var swipeHistory = new SwipeHistory
        {
            UserId = userId,
            MovieId = swipeDto.MovieId,
            IsLike = swipeDto.IsLike,
            SwipedAt = DateTime.UtcNow
        };

        _context.SwipeHistories.Add(swipeHistory);
        await _context.SaveChangesAsync();

        return Ok(new { message = swipeDto.IsLike ? "Movie liked!" : "Movie passed" });
    }

    [HttpGet("history")]
    public async Task<ActionResult<List<SwipeHistoryDto>>> GetSwipeHistory(
        [FromQuery] bool? onlyLikes = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        IQueryable<SwipeHistory> query = _context.SwipeHistories
            .Where(sh => sh.UserId == userId);

        if (onlyLikes.HasValue)
        {
            query = query.Where(sh => sh.IsLike == onlyLikes.Value);
        }

        var history = await query
            .Include(sh => sh.Movie)
            .OrderByDescending(sh => sh.SwipedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(sh => new SwipeHistoryDto
            {
                Id = sh.Id,
                MovieId = sh.MovieId,
                MovieName = sh.Movie.Name,
                PosterUrl = sh.Movie.PosterUrl ?? "",
                IsLike = sh.IsLike,
                SwipedAt = sh.SwipedAt
            })
            .ToListAsync();

        return Ok(history);
    }

    [HttpGet("liked")]
    public async Task<ActionResult<List<Movie>>> GetLikedMovies(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var likedMovies = await _context.SwipeHistories
            .Where(sh => sh.UserId == userId && sh.IsLike)
            .Include(sh => sh.Movie)
            .OrderByDescending(sh => sh.SwipedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(sh => sh.Movie)
            .ToListAsync();

        return Ok(likedMovies);
    }

    [HttpDelete("history/{swipeId}")]
    public async Task<ActionResult> DeleteSwipe(int swipeId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var swipe = await _context.SwipeHistories
            .FirstOrDefaultAsync(sh => sh.Id == swipeId && sh.UserId == userId);

        if (swipe == null)
        {
            return NotFound(new { message = "Swipe not found" });
        }

        _context.SwipeHistories.Remove(swipe);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Swipe removed" });
    }

    [HttpGet("stats")]
    public async Task<ActionResult> GetSwipeStats()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var totalSwipes = await _context.SwipeHistories
            .Where(sh => sh.UserId == userId)
            .CountAsync();

        var totalLikes = await _context.SwipeHistories
            .Where(sh => sh.UserId == userId && sh.IsLike)
            .CountAsync();

        var totalDislikes = totalSwipes - totalLikes;

        var favoriteGenre = await _context.SwipeHistories
            .Where(sh => sh.UserId == userId && sh.IsLike)
            .Include(sh => sh.Movie)
            .GroupBy(sh => sh.Movie.Genre)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key.ToString())
            .FirstOrDefaultAsync();

        return Ok(new
        {
            totalSwipes,
            totalLikes,
            totalDislikes,
            likePercentage = totalSwipes > 0 ? Math.Round((double)totalLikes / totalSwipes * 100, 1) : 0,
            favoriteGenre = favoriteGenre ?? "None yet"
        });
    }
}