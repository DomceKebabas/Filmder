using Filmder.Data;
using Filmder.DTOs.HigherLower;
using Filmder.Interfaces;
using Filmder.Models;
using Microsoft.EntityFrameworkCore;

namespace Filmder.Services;

public class HigherLowerRepository(AppDbContext context) : IHigherLowerRepository
{
    public async Task<StartGameResponseDto> StartGameAsync(string userId)
    {
        var activeGames = await context.HigherLowerGames
            .Where(g => g.UserId == userId && g.IsActive)
            .ToListAsync();

        foreach (var game in activeGames)
        {
            game.IsActive = false;
            game.EndedAt = DateTime.UtcNow;
        }

        var bestStreak = await context.HigherLowerGames
            .Where(g => g.UserId == userId)
            .MaxAsync(g => (int?)g.BestStreak) ?? 0;

        var (movie1, movie2) = await GetRandomMoviePairAsync();
        if (movie1 == null || movie2 == null)
            throw new Exception("Not enough movies in database.");

        var newGame = new HigherLowerGame
        {
            UserId = userId,
            CurrentMovie = movie1,
            NextMovie = movie2,
            CurrentStreak = 0,
            BestStreak = bestStreak,
            IsActive = true
        };

        context.HigherLowerGames.Add(newGame);
        await context.SaveChangesAsync();

        return new StartGameResponseDto
        {
            GameId = newGame.Id,
            Comparison = BuildComparison(movie1, movie2, hideNextRating: true),
            CurrentStreak = 0,
            BestStreak = bestStreak
        };
    }

    public async Task<GuessResultDto> SubmitGuessAsync(HigherLowerGuessDto dto, string userId)
    {
        if (dto.Guess != "higher" && dto.Guess != "lower")
            throw new Exception("wrong input.");

        var game = await context.HigherLowerGames
            .Include(g => g.CurrentMovie)
            .Include(g => g.NextMovie)
            .FirstOrDefaultAsync(g => g.Id == dto.GameId && g.UserId == userId && g.IsActive);

        if (game == null)
            throw new Exception("Game not found.");

        var guessedMovie = game.NextMovie!;
        var guessedRating = guessedMovie.Rating;

        bool isCorrect = dto.Guess == "higher"
            ? guessedMovie.Rating >= game.CurrentMovie!.Rating
            : guessedMovie.Rating <= game.CurrentMovie!.Rating;

        context.HigherLowerGuesses.Add(new HigherLowerGuess
        {
            GameId = game.Id,
            Movie1Id = game.CurrentMovie!.Id,
            Movie2Id = guessedMovie.Id,
            GuessedHigher = dto.Guess,
            WasCorrect = isCorrect
        });

        if (!isCorrect)
        {
            game.IsActive = false;
            game.EndedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            return new GuessResultDto
            {
                WasCorrect = false,
                ActualRating = guessedRating,
                CurrentStreak = game.CurrentStreak,
                BestStreak = game.BestStreak,
                GameOver = true
            };
        }

        game.CurrentStreak++;
        if (game.CurrentStreak > game.BestStreak)
            game.BestStreak = game.CurrentStreak;

        var newNext = await GetRandomMovieAsync(new[] { guessedMovie.Id });
        if (newNext == null)
        {
            game.IsActive = false;
            game.EndedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            return new GuessResultDto
            {
                WasCorrect = true,
                ActualRating = guessedRating,
                CurrentStreak = game.CurrentStreak,
                BestStreak = game.BestStreak,
                GameOver = true
            };
        }

        game.CurrentMovieId = guessedMovie.Id;
        game.NextMovieId = newNext.Id;
        await context.SaveChangesAsync();

        return new GuessResultDto
        {
            WasCorrect = true,
            ActualRating = guessedRating,
            CurrentStreak = game.CurrentStreak,
            BestStreak = game.BestStreak,
            GameOver = false,
            NextComparison = BuildComparison(guessedMovie, newNext, hideNextRating: true)
        };
    }

    public async Task<HigherLowerStatsDto> GetMyStatsAsync(string userId)
    {
        var games = await context.HigherLowerGames
            .Where(g => g.UserId == userId)
            .ToListAsync();

        var guesses = await context.HigherLowerGuesses
            .Where(g => g.Game != null && g.Game.UserId == userId)
            .ToListAsync();

        return new HigherLowerStatsDto
        {
            TotalGames = games.Count,
            BestStreak = games.Any() ? games.Max(g => g.BestStreak) : 0,
            TotalCorrectGuesses = guesses.Count(g => g.WasCorrect),
            TotalGuesses = guesses.Count,
            AccuracyPercentage = guesses.Any()
                ? Math.Round((double)guesses.Count(g => g.WasCorrect) / guesses.Count * 100, 1)
                : 0
        };
    }

    public async Task<List<LeaderboardEntryDto>> GetLeaderboardAsync(int limit)
    {
        var leaderboard = await context.HigherLowerGames
            .Where(g => !g.IsActive)
            .GroupBy(g => g.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                BestStreak = g.Max(x => x.BestStreak),
                AchievedAt = g.OrderByDescending(x => x.BestStreak)
                    .ThenByDescending(x => x.EndedAt)
                    .First().EndedAt
            })
            .OrderByDescending(x => x.BestStreak)
            .Take(limit)
            .ToListAsync();

        var users = await context.Users
            .Where(u => leaderboard.Select(l => l.UserId).Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.UserName ?? u.Email ?? "Unknown");

        return leaderboard.Select(l => new LeaderboardEntryDto
        {
            Username = users.GetValueOrDefault(l.UserId, "Unknown"),
            BestStreak = l.BestStreak,
            AchievedAt = l.AchievedAt ?? DateTime.UtcNow
        }).ToList();
    }

    public async Task<object> EndGameAsync(int gameId, string userId)
    {
        var game = await context.HigherLowerGames
            .FirstOrDefaultAsync(g => g.Id == gameId && g.UserId == userId);

        if (game == null)
            throw new Exception("There was no game found.");

        game.IsActive = false;
        game.EndedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return new { message = "The game ended", finalStreak = game.CurrentStreak };
    }

    // ===== helpers (perkelti 1:1) =====

    private async Task<(Movie?, Movie?)> GetRandomMoviePairAsync()
    {
        var total = await context.Movies.CountAsync();
        if (total < 2) return (null, null);

        var random = new Random();
        var skip1 = random.Next(total);
        var skip2 = random.Next(total);
        while (skip2 == skip1)
            skip2 = random.Next(total);

        var movie1 = await context.Movies.Skip(skip1).FirstOrDefaultAsync();
        var movie2 = await context.Movies.Skip(skip2).FirstOrDefaultAsync();

        return (movie1, movie2);
    }

    private async Task<Movie?> GetRandomMovieAsync(int[] excludeIds)
    {
        var movies = await context.Movies
            .Where(m => !excludeIds.Contains(m.Id))
            .ToListAsync();

        if (!movies.Any()) return null;

        var random = new Random();
        return movies[random.Next(movies.Count)];
    }

    private static MovieComparisonDto BuildComparison(Movie current, Movie next, bool hideNextRating)
        => new()
        {
            CurrentMovie = new MovieBasicDto
            {
                Id = current.Id,
                Name = current.Name,
                Genre = current.Genre.ToString(),
                ReleaseYear = current.ReleaseYear,
                Rating = current.Rating,
                PosterUrl = current.PosterUrl
            },
            NextMovie = new MovieBasicDto
            {
                Id = next.Id,
                Name = next.Name,
                Genre = next.Genre.ToString(),
                ReleaseYear = next.ReleaseYear,
                Rating = hideNextRating ? null : next.Rating,
                PosterUrl = next.PosterUrl
            }
        };
}
