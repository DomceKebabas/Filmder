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
public class GuessRatingGameController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public GuessRatingGameController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private async Task CheckIfGameShouldFinish(GuessRatingGame game)
    {
        if (DateTime.UtcNow >= game.ExpiresAt)
        {
            game.IsActive = false;
            await _dbContext.SaveChangesAsync();
            return;
        }
        
        int memberCount = game.Group.GroupMembers.Count;
        
        int playersFinished = game.Guesses
            .GroupBy(g => g.UserId)
            .Count(g => g.Count() >= game.TotalMovies);
        
        if (playersFinished >= memberCount && memberCount > 0)
        {
            game.IsActive = false;
            await _dbContext.SaveChangesAsync();
        }
    }
    
    [HttpGet("groups/{groupId}/active-games")]
    public async Task<ActionResult<List<object>>> GetActiveGames(int groupId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest("User not authenticated");

        try
        {
            var isMember = await _dbContext.GroupMembers
                .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

            if (!isMember) return Forbid();

            // Check and update expired games
            var expiredGames = await _dbContext.RatingGuessingGames
                .Where(g => g.GroupId == groupId && g.IsActive && g.ExpiresAt <= DateTime.UtcNow)
                .ToListAsync();

            foreach (var expiredGame in expiredGames)
            {
                expiredGame.IsActive = false;
            }
            if (expiredGames.Any())
            {
                await _dbContext.SaveChangesAsync();
            }

            var activeGames = await _dbContext.RatingGuessingGames
                .Where(g => g.GroupId == groupId && g.IsActive)
                .Include(g => g.Guesses)
                .Include(g => g.Group)
                    .ThenInclude(g => g.GroupMembers)
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();

            var result = activeGames.Select(g => {
                var memberCount = g.Group.GroupMembers.Count;
                var playerProgress = g.Group.GroupMembers.Select(m => {
                    var guessCount = g.Guesses.Count(guess => guess.UserId == m.UserId);
                    return new {
                        userId = m.UserId,
                        guessCount = guessCount,
                        completed = guessCount >= g.TotalMovies
                    };
                }).ToList();

                return new
                {
                    id = g.Id,
                    groupId = g.GroupId,
                    creatorId = g.UserId,
                    userId = g.UserId, // Keep for backward compatibility
                    isActive = g.IsActive,
                    createdAt = g.CreatedAt,
                    expiresAt = g.ExpiresAt,
                    totalMovies = g.TotalMovies,
                    playerProgress = playerProgress,
                    participantCount = g.Guesses.Select(x => x.UserId).Distinct().Count()
                };
            }).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to get active games", message = ex.Message });
        }
    }
    
    [HttpGet("groups/{groupId}/past-games")]
    public async Task<ActionResult<List<object>>> GetPastGames(int groupId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest("User not authenticated");

        try
        {
            var isMember = await _dbContext.GroupMembers
                .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

            if (!isMember) return Forbid();

            var pastGames = await _dbContext.RatingGuessingGames
                .Where(g => g.GroupId == groupId && !g.IsActive)
                .Include(g => g.Guesses)
                .OrderByDescending(g => g.CreatedAt)
                .Take(20)
                .Select(g => new
                {
                    id = g.Id,
                    groupId = g.GroupId,
                    creatorId = g.UserId,
                    userId = g.UserId,
                    isActive = g.IsActive,
                    createdAt = g.CreatedAt,
                    expiresAt = g.ExpiresAt,
                    totalMovies = g.TotalMovies,
                    participantCount = g.Guesses.Select(x => x.UserId).Distinct().Count()
                })
                .ToListAsync();

            return Ok(pastGames);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to get past games", message = ex.Message });
        }
    }
    
    [HttpGet("games/{gameId}/my-guesses")]
    public async Task<ActionResult<List<object>>> GetMyGuesses(int gameId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest("User not authenticated");

        try
        {
            var game = await _dbContext.RatingGuessingGames
                .Include(g => g.Group)
                    .ThenInclude(g => g.GroupMembers)
                .FirstOrDefaultAsync(g => g.Id == gameId);

            if (game == null) return NotFound("Game not found");

            var userPartOfGroup = game.Group.GroupMembers.Any(gm => gm.UserId == userId);
            if (!userPartOfGroup) return Forbid();

            var guesses = await _dbContext.MovieRatingGuesses
                .Where(g => g.GuessRatingGameId == gameId && g.UserId == userId)
                .Select(g => new
                {
                    id = g.Id,
                    movieId = g.MovieId,
                    ratingGuessValue = g.RatingGuessValue
                })
                .ToListAsync();

            return Ok(guesses);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to get guesses", message = ex.Message });
        }
    }
    
    [HttpPost("groups/{groupId}/guessing-games")]
    public async Task<ActionResult<object>> CreateGame(int groupId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest("User not authenticated");

        try
        {
            var isMember = await _dbContext.GroupMembers
                .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

            if (!isMember) return Forbid();

            var movieCount = await _dbContext.Movies.CountAsync();

            if (movieCount == 0)
                return NotFound("No movies available");

            // Get 10 random movies
            var random = new Random(Guid.NewGuid().GetHashCode());
            var allMovieIds = await _dbContext.Movies.Select(m => m.Id).ToListAsync();
            var selectedIds = allMovieIds.OrderBy(x => random.Next()).Take(10).ToList();
            
            var movies = await _dbContext.Movies
                .Where(m => selectedIds.Contains(m.Id))
                .ToListAsync();

            if (!movies.Any())
                return NotFound("No movies found");

            var guessRatingGame = new GuessRatingGame
            {
                GroupId = groupId,
                UserId = userId, // Creator
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                TotalMovies = movies.Count
            };

            _dbContext.RatingGuessingGames.Add(guessRatingGame);
            await _dbContext.SaveChangesAsync();

            guessRatingGame.Movies = movies;
            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                id = guessRatingGame.Id,
                groupId = guessRatingGame.GroupId,
                creatorId = guessRatingGame.UserId,
                userId = guessRatingGame.UserId,
                isActive = guessRatingGame.IsActive,
                movieCount = movies.Count,
                expiresAt = guessRatingGame.ExpiresAt,
                totalMovies = guessRatingGame.TotalMovies
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                error = "Failed to create game",
                message = ex.Message,
                details = ex.InnerException?.Message
            });
        }
    }
    
    [HttpGet("games/{gameId}/guessing-games")]
    public async Task<ActionResult<List<object>>> GetGameMovies(int gameId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest("User not authenticated");

        try
        {
            var game = await _dbContext.RatingGuessingGames
                .Include(g => g.Movies)
                .Include(g => g.Group)
                    .ThenInclude(g => g.GroupMembers)
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == gameId);

            if (game == null) return NotFound("Game not found");

            // Any group member can access the game
            var userPartOfGroup = game.Group.GroupMembers.Any(gm => gm.UserId == userId);
            if (!userPartOfGroup) return Forbid();

            if (!game.Movies.Any()) return NotFound("No movies in this game");

            var movieList = game.Movies.Select(m => new
            {
                id = m.Id,
                name = m.Name,
                genre = m.Genre.ToString(),
                description = m.Description,
                releaseYear = m.ReleaseYear,
                rating = m.Rating,
                posterUrl = m.PosterUrl,
                trailerUrl = m.TrailerUrl,
                duration = m.Duration,
                director = m.Director,
                cast = m.Cast
            }).ToList();

            return Ok(movieList);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                error = "Failed to get movies",
                message = ex.Message
            });
        }
    }
    
    [HttpPost("games/{gameId}/movies/{movieId}/guesses")]
    public async Task<ActionResult<object>> GuessRating(int gameId, int movieId, [FromBody] RatingGuessDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest("User not authenticated");

        try
        {
            var game = await _dbContext.RatingGuessingGames
                .Include(g => g.Movies)
                .Include(g => g.Group)
                    .ThenInclude(g => g.GroupMembers)
                .Include(g => g.Guesses)
                .FirstOrDefaultAsync(g => g.Id == gameId);

            if (game == null) return NotFound("Game not found");
            
            var userPartOfGroup = game.Group.GroupMembers.Any(gm => gm.UserId == userId);
            if (!userPartOfGroup) return Forbid();

            if (!game.IsActive)
                return BadRequest("Game has ended");

            if (!game.Movies.Any(m => m.Id == movieId))
                return BadRequest("Movie not in this game");

            var existingGuess = game.Guesses
                .FirstOrDefault(g => g.UserId == userId && g.MovieId == movieId);

            if (existingGuess != null)
                return BadRequest("You already guessed this movie");

            var guess = new MovieRatingGuess
            {
                GuessRatingGameId = gameId,
                MovieId = movieId,
                UserId = userId,
                RatingGuessValue = dto.RatingGuessValue
            };

            _dbContext.MovieRatingGuesses.Add(guess);
            await _dbContext.SaveChangesAsync();

            // Reload game with fresh data to check if it should finish
            game = await _dbContext.RatingGuessingGames
                .Include(g => g.Group)
                    .ThenInclude(g => g.GroupMembers)
                .Include(g => g.Guesses)
                .FirstOrDefaultAsync(g => g.Id == gameId);

            await CheckIfGameShouldFinish(game!);

            return Ok(new
            {
                id = guess.Id,
                gameId = guess.GuessRatingGameId,
                movieId = guess.MovieId,
                userId = guess.UserId,
                ratingGuessValue = guess.RatingGuessValue
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                error = "Failed to submit guess",
                message = ex.Message,
                details = ex.InnerException?.Message
            });
        }
    }
    
    [HttpPost("games/{gameId}/finish")]
    public async Task<ActionResult<object>> FinishGame(int gameId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest("User not authenticated");

        try
        {
            var game = await _dbContext.RatingGuessingGames
                .Include(g => g.Group)
                    .ThenInclude(g => g.GroupMembers)
                .Include(g => g.Guesses)
                .FirstOrDefaultAsync(g => g.Id == gameId);

            if (game == null) return NotFound("Game not found");

            var userPartOfGroup = game.Group.GroupMembers.Any(gm => gm.UserId == userId);
            if (!userPartOfGroup) return Forbid();

            // Only allow finishing if user is creator or has completed all guesses
            var userGuessCount = game.Guesses.Count(g => g.UserId == userId);
            var isCreator = game.UserId == userId;
            var hasCompletedAllGuesses = userGuessCount >= game.TotalMovies;

            if (!isCreator && !hasCompletedAllGuesses)
            {
                return BadRequest("You must complete all guesses before finishing the game");
            }

            game.IsActive = false;
            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                id = game.Id,
                isActive = game.IsActive
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                error = "Failed to finish game",
                message = ex.Message
            });
        }
    }
    
    [HttpGet("games/{gameId}/results")]
    public async Task<ActionResult> GetGameResults(int gameId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest("User not authenticated");

        try
        {
            var game = await _dbContext.RatingGuessingGames
                .Include(g => g.Group)
                    .ThenInclude(g => g.GroupMembers)
                        .ThenInclude(m => m.User)
                .Include(g => g.Movies)
                .Include(g => g.Guesses)
                    .ThenInclude(g => g.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == gameId);

            if (game == null) return NotFound("Game not found");

            var userPartOfGroup = game.Group.GroupMembers.Any(gm => gm.UserId == userId);
            if (!userPartOfGroup) return Forbid();
            
            var results = game.Guesses
                .GroupBy(g => g.UserId)
                .Select(group => {
                    var totalDiff = group.Sum(g =>
                    {
                        var movie = game.Movies.FirstOrDefault(m => m.Id == g.MovieId);
                        return movie == null ? 0 : Math.Abs(movie.Rating - g.RatingGuessValue);
                    });
                    var avgDiff = group.Average(g =>
                    {
                        var movie = game.Movies.FirstOrDefault(m => m.Id == g.MovieId);
                        return movie == null ? 0 : Math.Abs(movie.Rating - g.RatingGuessValue);
                    });

                    return new
                    {
                        userId = group.Key,
                        username = group.FirstOrDefault()?.User?.UserName
                            ?? group.FirstOrDefault()?.User?.Email
                            ?? "Unknown",
                        totalDifference = Math.Round(totalDiff, 2),
                        averageDifference = Math.Round(avgDiff, 2),
                        guessCount = group.Count(),
                        completed = group.Count() >= game.TotalMovies
                    };
                })
                .OrderBy(x => x.totalDifference)
                .ToList();

            return Ok(results);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                error = "Failed to get results",
                message = ex.Message
            });
        }
    }
    
    [HttpGet("games/{gameId}/status")]
    public async Task<ActionResult> GetGameStatus(int gameId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest("User not authenticated");

        try
        {
            var game = await _dbContext.RatingGuessingGames
                .Include(g => g.Group)
                    .ThenInclude(g => g.GroupMembers)
                        .ThenInclude(m => m.User)
                .Include(g => g.Guesses)
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == gameId);

            if (game == null) return NotFound("Game not found");

            var userPartOfGroup = game.Group.GroupMembers.Any(gm => gm.UserId == userId);
            if (!userPartOfGroup) return Forbid();

            var playerStatus = game.Group.GroupMembers.Select(m => {
                var guessCount = game.Guesses.Count(g => g.UserId == m.UserId);
                return new
                {
                    userId = m.UserId,
                    username = m.User?.UserName ?? m.User?.Email ?? "Unknown",
                    guessCount = guessCount,
                    totalMovies = game.TotalMovies,
                    completed = guessCount >= game.TotalMovies,
                    progress = game.TotalMovies > 0 ? Math.Round((double)guessCount / game.TotalMovies * 100, 0) : 0
                };
            }).ToList();

            return Ok(new
            {
                gameId = game.Id,
                isActive = game.IsActive,
                totalMovies = game.TotalMovies,
                expiresAt = game.ExpiresAt,
                creatorId = game.UserId,
                players = playerStatus
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to get game status", message = ex.Message });
        }
    }
}