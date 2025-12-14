using Filmder.Data;
using Filmder.DTOs;
using Filmder.Interfaces;
using Filmder.Models;
using Microsoft.EntityFrameworkCore;

namespace Filmder.Services;

public class GuessRatingGameRepository(AppDbContext dbContext)
    : IGuessRatingGameRepository
{
    private async Task EnsureMemberAsync(int groupId, string userId)
    {
        var isMember = await dbContext.GroupMembers
            .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

        if (!isMember)
            throw new UnauthorizedAccessException();
    }

    private async Task CheckIfGameShouldFinishAsync(GuessRatingGame game)
    {
        if (DateTime.UtcNow >= game.ExpiresAt)
        {
            game.IsActive = false;
            await dbContext.SaveChangesAsync();
            return;
        }

        var memberCount = game.Group.GroupMembers.Count;

        var playersFinished = game.Guesses
            .GroupBy(g => g.UserId)
            .Count(g => g.Count() >= game.TotalMovies);

        if (playersFinished >= memberCount && memberCount > 0)
        {
            game.IsActive = false;
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task<List<object>> GetActiveGamesAsync(int groupId, string userId)
    {
        await EnsureMemberAsync(groupId, userId);

        var expired = await dbContext.RatingGuessingGames
            .Where(g => g.GroupId == groupId && g.IsActive && g.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync();

        if (expired.Any())
        {
            expired.ForEach(g => g.IsActive = false);
            await dbContext.SaveChangesAsync();
        }

        var games = await dbContext.RatingGuessingGames
            .Where(g => g.GroupId == groupId && g.IsActive)
            .Include(g => g.Guesses)
            .Include(g => g.Group).ThenInclude(g => g.GroupMembers)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();

        return games.Select(g =>
        {
            var progress = g.Group.GroupMembers.Select(m =>
            {
                var count = g.Guesses.Count(x => x.UserId == m.UserId);
                return new
                {
                    userId = m.UserId,
                    guessCount = count,
                    completed = count >= g.TotalMovies
                };
            }).ToList();

            return new
            {
                id = g.Id,
                groupId = g.GroupId,
                creatorId = g.UserId,
                userId = g.UserId,
                isActive = g.IsActive,
                createdAt = g.CreatedAt,
                expiresAt = g.ExpiresAt,
                totalMovies = g.TotalMovies,
                playerProgress = progress,
                participantCount = g.Guesses.Select(x => x.UserId).Distinct().Count()
            };
        }).Cast<object>().ToList();
    }

    public async Task<List<object>> GetPastGamesAsync(int groupId, string userId)
    {
        await EnsureMemberAsync(groupId, userId);

        return await dbContext.RatingGuessingGames
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
            .Cast<object>()
            .ToListAsync();
    }

    public async Task<List<object>> GetMyGuessesAsync(int gameId, string userId)
    {
        var game = await dbContext.RatingGuessingGames
            .Include(g => g.Group).ThenInclude(g => g.GroupMembers)
            .FirstOrDefaultAsync(g => g.Id == gameId);

        if (game == null)
            throw new Exception("Game not found");

        if (!game.Group.GroupMembers.Any(m => m.UserId == userId))
            throw new UnauthorizedAccessException();

        return await dbContext.MovieRatingGuesses
            .Where(g => g.GuessRatingGameId == gameId && g.UserId == userId)
            .Select(g => new
            {
                id = g.Id,
                movieId = g.MovieId,
                ratingGuessValue = g.RatingGuessValue
            })
            .Cast<object>()
            .ToListAsync();
    }

    public async Task<object> CreateGameAsync(int groupId, string userId)
    {
        await EnsureMemberAsync(groupId, userId);

        var movieIds = await dbContext.Movies.Select(m => m.Id).ToListAsync();
        if (!movieIds.Any())
            throw new Exception("No movies available");

        var random = new Random(Guid.NewGuid().GetHashCode());
        var selected = movieIds.OrderBy(_ => random.Next()).Take(10).ToList();

        var movies = await dbContext.Movies
            .Where(m => selected.Contains(m.Id))
            .ToListAsync();

        var game = new GuessRatingGame
        {
            GroupId = groupId,
            UserId = userId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            TotalMovies = movies.Count
        };

        dbContext.RatingGuessingGames.Add(game);
        await dbContext.SaveChangesAsync();

        game.Movies = movies;
        await dbContext.SaveChangesAsync();

        return new
        {
            id = game.Id,
            groupId = game.GroupId,
            creatorId = game.UserId,
            userId = game.UserId,
            isActive = game.IsActive,
            movieCount = movies.Count,
            expiresAt = game.ExpiresAt,
            totalMovies = game.TotalMovies
        };
    }

    public async Task<List<object>> GetGameMoviesAsync(int gameId, string userId)
    {
        var game = await dbContext.RatingGuessingGames
            .Include(g => g.Movies)
            .Include(g => g.Group).ThenInclude(g => g.GroupMembers)
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == gameId);

        if (game == null)
            throw new Exception("Game not found");

        if (!game.Group.GroupMembers.Any(m => m.UserId == userId))
            throw new UnauthorizedAccessException();

        return game.Movies.Select(m => new
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
        }).Cast<object>().ToList();
    }

    public async Task<object> GuessRatingAsync(int gameId, int movieId, RatingGuessDto dto, string userId)
    {
        var game = await dbContext.RatingGuessingGames
            .Include(g => g.Movies)
            .Include(g => g.Group).ThenInclude(g => g.GroupMembers)
            .Include(g => g.Guesses)
            .FirstOrDefaultAsync(g => g.Id == gameId);

        if (game == null)
            throw new Exception("Game not found");

        if (!game.Group.GroupMembers.Any(m => m.UserId == userId))
            throw new UnauthorizedAccessException();

        if (!game.IsActive)
            throw new Exception("Game has ended");

        if (!game.Movies.Any(m => m.Id == movieId))
            throw new Exception("Movie not in game");

        if (game.Guesses.Any(g => g.UserId == userId && g.MovieId == movieId))
            throw new Exception("Already guessed");

        var guess = new MovieRatingGuess
        {
            GuessRatingGameId = gameId,
            MovieId = movieId,
            UserId = userId,
            RatingGuessValue = dto.RatingGuessValue
        };

        dbContext.MovieRatingGuesses.Add(guess);
        await dbContext.SaveChangesAsync();

        game = await dbContext.RatingGuessingGames
            .Include(g => g.Group).ThenInclude(g => g.GroupMembers)
            .Include(g => g.Guesses)
            .FirstOrDefaultAsync(g => g.Id == gameId);

        await CheckIfGameShouldFinishAsync(game!);

        return new
        {
            id = guess.Id,
            gameId = guess.GuessRatingGameId,
            movieId = guess.MovieId,
            userId = guess.UserId,
            ratingGuessValue = guess.RatingGuessValue
        };
    }

    public async Task<object> FinishGameAsync(int gameId, string userId)
    {
        var game = await dbContext.RatingGuessingGames
            .Include(g => g.Group).ThenInclude(g => g.GroupMembers)
            .Include(g => g.Guesses)
            .FirstOrDefaultAsync(g => g.Id == gameId);

        if (game == null)
            throw new Exception("Game not found");

        if (!game.Group.GroupMembers.Any(m => m.UserId == userId))
            throw new UnauthorizedAccessException();

        var userGuessCount = game.Guesses.Count(g => g.UserId == userId);
        var isCreator = game.UserId == userId;

        if (!isCreator && userGuessCount < game.TotalMovies)
            throw new Exception("You must complete all guesses before finishing");

        game.IsActive = false;
        await dbContext.SaveChangesAsync();

        return new { id = game.Id, isActive = game.IsActive };
    }

    public async Task<List<object>> GetGameResultsAsync(int gameId, string userId)
    {
        var game = await dbContext.RatingGuessingGames
            .Include(g => g.Group).ThenInclude(g => g.GroupMembers).ThenInclude(m => m.User)
            .Include(g => g.Movies)
            .Include(g => g.Guesses).ThenInclude(g => g.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == gameId);

        if (game == null)
            throw new Exception("Game not found");

        if (!game.Group.GroupMembers.Any(m => m.UserId == userId))
            throw new UnauthorizedAccessException();

        return game.Guesses
            .GroupBy(g => g.UserId)
            .Select(group =>
            {
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
                    username = group.First().User?.UserName
                        ?? group.First().User?.Email
                        ?? "Unknown",
                    totalDifference = Math.Round(totalDiff, 2),
                    averageDifference = Math.Round(avgDiff, 2),
                    guessCount = group.Count(),
                    completed = group.Count() >= game.TotalMovies
                };
            })
            .OrderBy(x => x.totalDifference)
            .Cast<object>()
            .ToList();
    }

    public async Task<object> GetGameStatusAsync(int gameId, string userId)
    {
        var game = await dbContext.RatingGuessingGames
            .Include(g => g.Group).ThenInclude(g => g.GroupMembers).ThenInclude(m => m.User)
            .Include(g => g.Guesses)
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == gameId);

        if (game == null)
            throw new Exception("Game not found");

        if (!game.Group.GroupMembers.Any(m => m.UserId == userId))
            throw new UnauthorizedAccessException();

        var players = game.Group.GroupMembers.Select(m =>
        {
            var count = game.Guesses.Count(g => g.UserId == m.UserId);
            return new
            {
                userId = m.UserId,
                username = m.User?.UserName ?? m.User?.Email ?? "Unknown",
                guessCount = count,
                totalMovies = game.TotalMovies,
                completed = count >= game.TotalMovies,
                progress = game.TotalMovies > 0
                    ? Math.Round((double)count / game.TotalMovies * 100, 0)
                    : 0
            };
        }).ToList();

        return new
        {
            gameId = game.Id,
            isActive = game.IsActive,
            totalMovies = game.TotalMovies,
            expiresAt = game.ExpiresAt,
            creatorId = game.UserId,
            players
        };
    }
}
