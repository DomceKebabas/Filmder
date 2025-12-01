using System.Security.Claims;
using Filmder.Data;
using Filmder.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Filmder.Controllers;
[EnableRateLimiting("DefaultBucket")]
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GroupStatsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public GroupStatsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    [HttpGet("playedGamesCount")]
    public async Task<ActionResult<int>> TotalGamesPlayed([FromQuery] int groupId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();

        var groupMember = await _dbContext.GroupMembers
            .FirstOrDefaultAsync(gm => gm.UserId == userId && gm.GroupId == groupId);
        
        if (groupMember == null) return Unauthorized();

        var groupGamesCount = await _dbContext.Games
            .Where(gm => gm.GroupId == groupId && !gm.IsActive)
            .CountAsync();

        return Ok(groupGamesCount);
    }
    
    [HttpGet("highestVotedMovie")]
    public async Task<ActionResult<HighestRatedMovieDto>> HighestVotedMovie([FromQuery] int groupId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();

        var groupMember = await _dbContext.GroupMembers
            .FirstOrDefaultAsync(gm => gm.UserId == userId && gm.GroupId == groupId);
        
        if (groupMember == null) return Unauthorized();

        var movieAndScore = await _dbContext.MovieScores
            .Where(ms => ms.Game != null && !ms.Game.IsActive && ms.Game.GroupId == groupId)
            .Where(ms => ms.Movie != null)
            .OrderByDescending(ms => ms.MovieScoreValue)
            .Include(ms => ms.Movie)
            .Select(ms => new HighestRatedMovieDto
            {
                Id = ms.Movie!.Id,
                Name = ms.Movie!.Name,
                Genre = ms.Movie!.Genre.ToString(),
                Description = ms.Movie!.Description,
                ReleaseYear = ms.Movie!.ReleaseYear,
                Rating = ms.Movie!.Rating,
                PosterUrl = ms.Movie!.PosterUrl ?? string.Empty,
                TrailerUrl = ms.Movie!.TrailerUrl ?? string.Empty,
                Duration = ms.Movie!.Duration,
                Director = ms.Movie!.Director,
                Cast = ms.Movie!.Cast,
                CreatedAt = ms.Movie!.CreatedAt,
                Score = ms.MovieScoreValue
            })
            .FirstOrDefaultAsync();

        if (movieAndScore == null) return NotFound();
        
        return Ok(movieAndScore);
    }
    
    [HttpGet("highestVotedGenre")]
    public async Task<ActionResult<PopularGenreDto>> HighestVotedGenre([FromQuery] int groupId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();

        var groupMember = await _dbContext.GroupMembers
            .FirstOrDefaultAsync(gm => gm.UserId == userId && gm.GroupId == groupId);
        
        if (groupMember == null) return Unauthorized();

        var mostPopular = await _dbContext.MovieScores
            .Where(ms => ms.Game != null && !ms.Game.IsActive && ms.Game.GroupId == groupId)
            .Where(ms => ms.Movie != null)
            .Include(ms => ms.Movie)
            .GroupBy(ms => ms.Movie!.Genre)
            .Select(g => new PopularGenreDto
            {
                Genre = g.Key.ToString(),
                TotalScore = g.Sum(ms => ms.MovieScoreValue)
            })
            .OrderByDescending(g => g.TotalScore)
            .FirstOrDefaultAsync();

        return mostPopular == null ? NotFound() : Ok(mostPopular);
    }
    
    [HttpGet("averageMovieScore")]
    public async Task<ActionResult<double>> GetAverageMovieScore([FromQuery] int groupId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();

        var query = _dbContext.MovieScores
            .Where(ms => ms.Game != null && !ms.Game.IsActive && ms.Game.GroupId == groupId);

        if (!await query.AnyAsync())
            return Ok(0.0);

        double averageScore = await query.AverageAsync(ms => ms.MovieScoreValue);

        return Ok(Math.Round(averageScore, 2)); 
    }
    
    [HttpGet("averageMovieDuration")]
    public async Task<ActionResult<double>> GetAverageMovieDuration([FromQuery] int groupId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();

        var query = _dbContext.MovieScores
            .Where(ms => ms.Game != null && !ms.Game.IsActive && ms.Game.GroupId == groupId)
            .Where(ms => ms.Movie != null)
            .Include(ms => ms.Movie);

        if (!await query.AnyAsync())
            return Ok(0.0);

        double averageDuration = await query.AverageAsync(ms => ms.Movie!.Duration);

        return Ok(Math.Round(averageDuration, 2));
    }
}