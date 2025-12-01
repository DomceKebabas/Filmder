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
public class RatingController : ControllerBase
{
    private readonly AppDbContext _context;

    public RatingController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("rate")]
    public async Task<IActionResult> RateMovie([FromBody] RateMovieDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var movie = await _context.Movies.FindAsync(dto.MovieId);
        if (movie == null) return NotFound("Movie not found");

        var existingRating = await _context.Ratings
            .FirstOrDefaultAsync(r => r.UserId == userId && r.MovieId == dto.MovieId);

        if (existingRating != null)
        {
            existingRating.Score = dto.Score;
            existingRating.Comment = dto.Comment;
            existingRating.CreatedAt = DateTime.UtcNow;
        }
        else
        {
            var rating = new Rating
            {
                UserId = userId,
                MovieId = dto.MovieId,
                Score = dto.Score,
                Comment = dto.Comment
            };
            _context.Ratings.Add(rating);
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Rating saved successfully" });
    }

    [HttpGet("movie/{movieId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRatings(int movieId)
    {
        var ratings = await _context.Ratings
            .Where(r => r.MovieId == movieId)
            .Include(r => r.User)
            .Select(r => new
            {
                r.Id,
                r.Score,
                r.Comment,
                r.CreatedAt,
                UserEmail = r.User.Email
            })
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return Ok(ratings);
    }

    [HttpGet("movie/{movieId}/average")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAverageRating(int movieId)
    {
        var ratings = await _context.Ratings
            .Where(r => r.MovieId == movieId)
            .ToListAsync();

        if (!ratings.Any())
        {
            return Ok(new { averageScore = 0, totalRatings = 0 });
        }

        var averageScore = ratings.Average(r => r.Score);
        var totalRatings = ratings.Count;

        return Ok(new { averageScore = Math.Round(averageScore, 1), totalRatings });
    }
}