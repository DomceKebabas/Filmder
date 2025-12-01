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

            var random = new Random(Guid.NewGuid().GetHashCode());
            int skip = random.Next(Math.Max(0, movieCount - 10));

            var movies = await _dbContext.Movies
                .OrderBy(m => m.Id)
                .Skip(skip)
                .Take(10)
                .ToListAsync();

            if (!movies.Any())
                return NotFound("No movies found");

            var guessRatingGame = new GuessRatingGame
            {
                GroupId = groupId,
                UserId = userId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.RatingGuessingGames.Add(guessRatingGame);
            await _dbContext.SaveChangesAsync();

            guessRatingGame.Movies = movies;
            await _dbContext.SaveChangesAsync();

            return Ok(new 
            { 
                id = guessRatingGame.Id,
                groupId = guessRatingGame.GroupId,
                userId = guessRatingGame.UserId,
                isActive = guessRatingGame.IsActive,
                movieCount = movies.Count
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
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
            return StatusCode(500, new { 
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
            return StatusCode(500, new { 
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
                .FirstOrDefaultAsync(g => g.Id == gameId);
                
            if (game == null) return NotFound("Game not found");

            var userPartOfGroup = game.Group.GroupMembers.Any(gm => gm.UserId == userId);
            if (!userPartOfGroup) return Forbid();

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
            return StatusCode(500, new { 
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
                .Select(group => new
                {
                    userId = group.Key,
                    username = group.FirstOrDefault()?.User?.UserName 
                        ?? group.FirstOrDefault()?.User?.Email 
                        ?? group.Key,
                    totalDifference = group.Sum(g =>
                    {
                        var movie = game.Movies.FirstOrDefault(m => m.Id == g.MovieId);
                        return movie == null ? 0 : Math.Abs(movie.Rating - g.RatingGuessValue);
                    }),
                    averageDifference = group.Average(g =>
                    {
                        var movie = game.Movies.FirstOrDefault(m => m.Id == g.MovieId);
                        return movie == null ? 0 : Math.Abs(movie.Rating - g.RatingGuessValue);
                    }),
                    guessCount = group.Count()
                })
                .OrderBy(x => x.totalDifference)
                .ToList();

            return Ok(results);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                error = "Failed to get results", 
                message = ex.Message 
            });
        }
    }
}