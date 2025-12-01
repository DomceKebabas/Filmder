using System.Security.Claims;
using Filmder.Data;
using Filmder.DTOs.HigherLower;
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
public class HigherLowerController : ControllerBase
{
    private readonly AppDbContext _context;

    public HigherLowerController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("start")]
    public async Task<ActionResult<StartGameResponseDto>> StartGame()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();
        
        var activeGames = await _context.HigherLowerGames
            .Where(g => g.UserId == userId && g.IsActive)
            .ToListAsync();
        
        foreach (var game in activeGames)
        {
            game.IsActive = false;
            game.EndedAt = DateTime.UtcNow;
        }
        
        var bestStreak = await _context.HigherLowerGames
            .Where(g => g.UserId == userId)
            .MaxAsync(g => (int?)g.BestStreak) ?? 0;
        
        var (movie1, movie2) = await GetRandomMoviePair();
        
        if (movie1 == null || movie2 == null)
            return BadRequest( new { message = "Not enough movies in database." });
        
        var newGame = new HigherLowerGame
        {
            UserId = userId,
            CurrentMovie = movie1,
            NextMovie = movie2,
            CurrentStreak = 0,
            BestStreak = bestStreak,
            IsActive = true
        };
        
        _context.HigherLowerGames.Add(newGame);
        await _context.SaveChangesAsync();
        
        var response = new StartGameResponseDto
        {
            GameId = newGame.Id,
            Comparison = new MovieComparisonDto
            {
                CurrentMovie = new MovieBasicDto
                {
                    Id = movie1.Id,
                    Name = movie1.Name,
                    Genre = movie1.Genre.ToString(),
                    ReleaseYear = movie1.ReleaseYear,
                    Rating = movie1.Rating,
                    PosterUrl = movie1.PosterUrl
                },
                NextMovie = new MovieBasicDto
                {
                    Id = movie2.Id,
                    Name = movie2.Name,
                    Genre = movie2.Genre.ToString(),
                    ReleaseYear = movie2.ReleaseYear,
                    Rating = null, 
                    PosterUrl = movie2.PosterUrl
                }
            },
            CurrentStreak = 0,
            BestStreak = bestStreak
        };

        return Ok(response);
    }

    [HttpPost("guess")]
    public async Task<ActionResult<GuessResultDto>> SubmitGuess([FromBody] HigherLowerGuessDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        if (dto.Guess != "higher" && dto.Guess != "lower")
            return BadRequest(new { message = "wrong input." });

        var game = await _context.HigherLowerGames
            .Include(g => g.CurrentMovie)
            .Include(g => g.NextMovie)
            .FirstOrDefaultAsync(g => g.Id == dto.GameId && g.UserId == userId && g.IsActive);

        if (game == null)
            return NotFound(new { message = "Game not found." });

        if (game.CurrentMovie == null || game.NextMovie == null)
            return BadRequest(new { message = "Game state is incorrect" });
        
        var guessedMovieRating = game.NextMovie.Rating;
        var guessedMovie = game.NextMovie;

        bool isCorrect = false;
        if (dto.Guess == "higher")
            isCorrect = game.NextMovie.Rating >= game.CurrentMovie.Rating;
        else
            isCorrect = game.NextMovie.Rating <= game.CurrentMovie.Rating;

        var guess = new HigherLowerGuess
        {
            GameId = game.Id,
            Movie1Id = game.CurrentMovie.Id,
            Movie2Id = game.NextMovie.Id,
            GuessedHigher = dto.Guess,
            WasCorrect = isCorrect
        };

        _context.HigherLowerGuesses.Add(guess);

        if (isCorrect)
        {
            game.CurrentStreak++;
            if (game.CurrentStreak > game.BestStreak)
                game.BestStreak = game.CurrentStreak;

            var newCurrentMovie = guessedMovie;
            
            var newNextMovie = await GetRandomMovie(excludeIds: new[] { newCurrentMovie.Id });

            if (newNextMovie == null)
            {
                game.IsActive = false;
                game.EndedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new GuessResultDto
                {
                    WasCorrect = true,
                    ActualRating = guessedMovieRating,
                    CurrentStreak = game.CurrentStreak,
                    BestStreak = game.BestStreak,
                    GameOver = true,
                    NextComparison = null
                });
            }

            game.CurrentMovieId = newCurrentMovie.Id;
            game.NextMovieId = newNextMovie.Id;

            await _context.SaveChangesAsync();

            var response = new GuessResultDto
            {
                WasCorrect = true,
                ActualRating = guessedMovieRating,
                CurrentStreak = game.CurrentStreak,
                BestStreak = game.BestStreak,
                GameOver = false,
                NextComparison = new MovieComparisonDto
                {
                    CurrentMovie = new MovieBasicDto
                    {
                        Id = newCurrentMovie.Id,
                        Name = newCurrentMovie.Name,
                        Genre = newCurrentMovie.Genre.ToString(),
                        ReleaseYear = newCurrentMovie.ReleaseYear,
                        Rating = newCurrentMovie.Rating,
                        PosterUrl = newCurrentMovie.PosterUrl
                    },
                    NextMovie = new MovieBasicDto
                    {
                        Id = newNextMovie.Id,
                        Name = newNextMovie.Name,
                        Genre = newNextMovie.Genre.ToString(),
                        ReleaseYear = newNextMovie.ReleaseYear,
                        Rating = null,
                        PosterUrl = newNextMovie.PosterUrl
                    }
                }
            };
            return Ok(response);
        }
        else 
        {
            game.IsActive = false;
            game.EndedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new GuessResultDto
            {
                WasCorrect = false,
                ActualRating = guessedMovieRating,
                CurrentStreak = game.CurrentStreak,
                BestStreak = game.BestStreak,
                GameOver = true,
                NextComparison = null
            });
        }
    }

    [HttpGet("stats")]
    public async Task<ActionResult<HigherLowerStatsDto>> GetMyStats()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();
        
        var games = await _context.HigherLowerGames
            .Where(g => g.UserId == userId)
            .ToListAsync();

        var guesses = await _context.HigherLowerGuesses
            .Where(g => g.Game != null && g.Game.UserId == userId)
            .ToListAsync();

        var stats = new HigherLowerStatsDto
        {
            TotalGames = games.Count,
            BestStreak = games.Any() ? games.Max(g => g.BestStreak) : 0,
            TotalCorrectGuesses = guesses.Count(g => g.WasCorrect),
            TotalGuesses = guesses.Count,
            AccuracyPercentage = guesses.Any()
                ? Math.Round((double)guesses.Count(g => g.WasCorrect) / guesses.Count * 100, 1)
                : 0
        };

        return Ok(stats);
    }
    
    [HttpGet("leaderboard")]
    public async Task<ActionResult<List<LeaderboardEntryDto>>> GetLeaderboard([FromQuery] int limit = 10)
    {
        var leaderboard = await _context.HigherLowerGames
            .Where(g => !g.IsActive)
            .GroupBy(g => g.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                BestStreak = g.Max(game => game.BestStreak),
                AchievedAt = g.OrderByDescending(game => game.BestStreak)
                    .ThenByDescending(game => game.EndedAt)
                    .First().EndedAt
            })
            .OrderByDescending(x => x.BestStreak)
            .Take(limit)
            .ToListAsync();

        var userIds = leaderboard.Select(l => l.UserId).ToList();
        var users = await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.UserName ?? u.Email ?? "Unknown");

        var result = leaderboard.Select(l => new LeaderboardEntryDto
        {
            Username = users.GetValueOrDefault(l.UserId, "Unknown"),
            BestStreak = l.BestStreak,
            AchievedAt = l.AchievedAt ?? DateTime.UtcNow
        }).ToList();

        return Ok(result);
    }

    [HttpDelete("end-game/{gameId}")]
    public async Task<ActionResult> EndGame(int gameId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();
        
        var game = await _context.HigherLowerGames
            .FirstOrDefaultAsync(g => g.Id == gameId && g.UserId == userId);
        
        if (game  == null) 
            return NotFound("There was no game found.");
        
        game.IsActive = false;
        game.EndedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        
        return Ok(new { message = "The game ended", finalStreak  = game.CurrentStreak });
    }

    // Helper methods
    private async Task<(Movie?, Movie?)> GetRandomMoviePair()
    {
        var totalMovies = await _context.Movies.CountAsync();
        if (totalMovies < 2) return (null, null);

        var random = new Random();
        var skip1 = random.Next(totalMovies);
        var skip2 = random.Next(totalMovies);
        
        while (skip2 == skip1)
            skip2 = random.Next(totalMovies);

        var movie1 = await _context.Movies.Skip(skip1).FirstOrDefaultAsync();
        var movie2 = await _context.Movies.Skip(skip2).FirstOrDefaultAsync();

        return (movie1, movie2);
    }

    private async Task<Movie?> GetRandomMovie(int[] excludeIds)
    {
        var availableMovies = await _context.Movies
            .Where(m => !excludeIds.Contains(m.Id))
            .ToListAsync();

        if (!availableMovies.Any()) return null;

        var random = new Random();
        var index = random.Next(availableMovies.Count);
        return availableMovies[index];
    }
}