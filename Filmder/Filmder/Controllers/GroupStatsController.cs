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

        // Count voting games
        var votingGamesCount = await _dbContext.Games
            .Where(gm => gm.GroupId == groupId && !gm.IsActive)
            .CountAsync();

        // Count rating guessing games
        var ratingGamesCount = await _dbContext.RatingGuessingGames
            .Where(rg => rg.GroupId == groupId && !rg.IsActive)
            .CountAsync();

        return Ok(votingGamesCount + ratingGamesCount);
    }
    
    [HttpGet("ratingGamesCount")]
    public async Task<ActionResult<int>> RatingGamesPlayed([FromQuery] int groupId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();

        var groupMember = await _dbContext.GroupMembers
            .FirstOrDefaultAsync(gm => gm.UserId == userId && gm.GroupId == groupId);
        
        if (groupMember == null) return Unauthorized();

        var ratingGamesCount = await _dbContext.RatingGuessingGames
            .Where(rg => rg.GroupId == groupId && !rg.IsActive)
            .CountAsync();

        return Ok(ratingGamesCount);
    }
    
    [HttpGet("votingGamesCount")]
    public async Task<ActionResult<int>> VotingGamesPlayed([FromQuery] int groupId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();

        var groupMember = await _dbContext.GroupMembers
            .FirstOrDefaultAsync(gm => gm.UserId == userId && gm.GroupId == groupId);
        
        if (groupMember == null) return Unauthorized();

        var votingGamesCount = await _dbContext.Games
            .Where(gm => gm.GroupId == groupId && !gm.IsActive)
            .CountAsync();

        return Ok(votingGamesCount);
    }
    
    [HttpGet("bestRatingGuesser")]
    public async Task<ActionResult> GetBestRatingGuesser([FromQuery] int groupId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();

        var groupMember = await _dbContext.GroupMembers
            .FirstOrDefaultAsync(gm => gm.UserId == userId && gm.GroupId == groupId);
        
        if (groupMember == null) return Unauthorized();

        // Get all finished rating games for this group
        var finishedGames = await _dbContext.RatingGuessingGames
            .Where(rg => rg.GroupId == groupId && !rg.IsActive)
            .Include(rg => rg.Movies)
            .Include(rg => rg.Guesses)
                .ThenInclude(g => g.User)
            .ToListAsync();

        if (!finishedGames.Any())
            return NotFound("No finished rating games");

        // Calculate average difference for each player across all games
        var playerStats = finishedGames
            .SelectMany(game => game.Guesses.Select(guess => new
            {
                guess.UserId,
                guess.User,
                Difference = Math.Abs(
                    game.Movies.FirstOrDefault(m => m.Id == guess.MovieId)?.Rating ?? 0 
                    - guess.RatingGuessValue
                )
            }))
            .GroupBy(x => x.UserId)
            .Select(group => new
            {
                UserId = group.Key,
                Username = group.FirstOrDefault()?.User?.UserName 
                    ?? group.FirstOrDefault()?.User?.Email 
                    ?? "Unknown",
                AverageDifference = Math.Round(group.Average(x => x.Difference), 2),
                TotalGuesses = group.Count()
            })
            .OrderBy(x => x.AverageDifference)
            .FirstOrDefault();

        if (playerStats == null)
            return NotFound("No guesses found");

        return Ok(new
        {
            userId = playerStats.UserId,
            username = playerStats.Username,
            averageDifference = playerStats.AverageDifference,
            totalGuesses = playerStats.TotalGuesses
        });
    }
    
    [HttpGet("averageGuessDifference")]
    public async Task<ActionResult<double>> GetAverageGuessDifference([FromQuery] int groupId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();

        var groupMember = await _dbContext.GroupMembers
            .FirstOrDefaultAsync(gm => gm.UserId == userId && gm.GroupId == groupId);
        
        if (groupMember == null) return Unauthorized();

        var finishedGames = await _dbContext.RatingGuessingGames
            .Where(rg => rg.GroupId == groupId && !rg.IsActive)
            .Include(rg => rg.Movies)
            .Include(rg => rg.Guesses)
            .ToListAsync();

        if (!finishedGames.Any())
            return Ok(0.0);

        var allDifferences = finishedGames
            .SelectMany(game => game.Guesses.Select(guess => 
                Math.Abs(
                    game.Movies.FirstOrDefault(m => m.Id == guess.MovieId)?.Rating ?? 0 
                    - guess.RatingGuessValue
                )
            ))
            .ToList();

        if (!allDifferences.Any())
            return Ok(0.0);

        return Ok(Math.Round(allDifferences.Average(), 2));
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

        // Get genres from voting games
        var votingGenres = await _dbContext.MovieScores
            .Where(ms => ms.Game != null && !ms.Game.IsActive && ms.Game.GroupId == groupId)
            .Where(ms => ms.Movie != null)
            .Include(ms => ms.Movie)
            .Select(ms => new { ms.Movie!.Genre, Score = ms.MovieScoreValue })
            .ToListAsync();

        // Get genres from rating guessing games
        var ratingGames = await _dbContext.RatingGuessingGames
            .Where(rg => rg.GroupId == groupId && !rg.IsActive)
            .Include(rg => rg.Movies)
            .ToListAsync();

        var ratingGenres = ratingGames
            .SelectMany(rg => rg.Movies)
            .Select(m => new { m.Genre, Score = 1 }) // Each movie in rating game counts as 1
            .ToList();

        // Combine both sources
        var allGenres = votingGenres
            .Concat(ratingGenres)
            .GroupBy(x => x.Genre)
            .Select(g => new PopularGenreDto
            {
                Genre = g.Key.ToString(),
                TotalScore = g.Sum(x => x.Score)
            })
            .OrderByDescending(g => g.TotalScore)
            .FirstOrDefault();

        return allGenres == null ? NotFound() : Ok(allGenres);
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