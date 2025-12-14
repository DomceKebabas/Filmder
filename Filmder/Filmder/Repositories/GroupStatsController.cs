using Filmder.Data;
using Filmder.DTOs;
using Filmder.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Filmder.Services;

public class GroupStatsRepository(AppDbContext dbContext) : IGroupStatsRepository
{
    private async Task EnsureMemberAsync(int groupId, string userId)
    {
        var member = await dbContext.GroupMembers
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

        if (member == null)
            throw new UnauthorizedAccessException();
    }

    public async Task<int> TotalGamesPlayedAsync(int groupId, string userId)
    {
        await EnsureMemberAsync(groupId, userId);

        var voting = await dbContext.Games
            .CountAsync(g => g.GroupId == groupId && !g.IsActive);

        var rating = await dbContext.RatingGuessingGames
            .CountAsync(rg => rg.GroupId == groupId && !rg.IsActive);

        return voting + rating;
    }

    public async Task<int> RatingGamesPlayedAsync(int groupId, string userId)
    {
        await EnsureMemberAsync(groupId, userId);

        return await dbContext.RatingGuessingGames
            .CountAsync(rg => rg.GroupId == groupId && !rg.IsActive);
    }

    public async Task<int> VotingGamesPlayedAsync(int groupId, string userId)
    {
        await EnsureMemberAsync(groupId, userId);

        return await dbContext.Games
            .CountAsync(g => g.GroupId == groupId && !g.IsActive);
    }

    public async Task<object> GetBestRatingGuesserAsync(int groupId, string userId)
    {
        await EnsureMemberAsync(groupId, userId);

        var finishedGames = await dbContext.RatingGuessingGames
            .Where(rg => rg.GroupId == groupId && !rg.IsActive)
            .Include(rg => rg.Movies)
            .Include(rg => rg.Guesses).ThenInclude(g => g.User)
            .ToListAsync();

        if (!finishedGames.Any())
            throw new Exception("No finished rating games");

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
            .Select(g => new
            {
                UserId = g.Key,
                Username = g.First().User?.UserName ?? g.First().User?.Email ?? "Unknown",
                AverageDifference = Math.Round(g.Average(x => x.Difference), 2),
                TotalGuesses = g.Count()
            })
            .OrderBy(x => x.AverageDifference)
            .FirstOrDefault();

        if (playerStats == null)
            throw new Exception("No guesses found");

        return new
        {
            userId = playerStats.UserId,
            username = playerStats.Username,
            averageDifference = playerStats.AverageDifference,
            totalGuesses = playerStats.TotalGuesses
        };
    }

    public async Task<double> GetAverageGuessDifferenceAsync(int groupId, string userId)
    {
        await EnsureMemberAsync(groupId, userId);

        var finishedGames = await dbContext.RatingGuessingGames
            .Where(rg => rg.GroupId == groupId && !rg.IsActive)
            .Include(rg => rg.Movies)
            .Include(rg => rg.Guesses)
            .ToListAsync();

        if (!finishedGames.Any())
            return 0.0;

        var diffs = finishedGames
            .SelectMany(g => g.Guesses.Select(guess =>
                Math.Abs(
                    g.Movies.FirstOrDefault(m => m.Id == guess.MovieId)?.Rating ?? 0
                    - guess.RatingGuessValue
                )))
            .ToList();

        return diffs.Any() ? Math.Round(diffs.Average(), 2) : 0.0;
    }

    public async Task<HighestRatedMovieDto> HighestVotedMovieAsync(int groupId, string userId)
    {
        await EnsureMemberAsync(groupId, userId);

        var movie = await dbContext.MovieScores
            .Where(ms => ms.Game != null && !ms.Game.IsActive && ms.Game.GroupId == groupId)
            .Where(ms => ms.Movie != null)
            .OrderByDescending(ms => ms.MovieScoreValue)
            .Include(ms => ms.Movie)
            .Select(ms => new HighestRatedMovieDto
            {
                Id = ms.Movie!.Id,
                Name = ms.Movie.Name,
                Genre = ms.Movie.Genre.ToString(),
                Description = ms.Movie.Description,
                ReleaseYear = ms.Movie.ReleaseYear,
                Rating = ms.Movie.Rating,
                PosterUrl = ms.Movie.PosterUrl ?? string.Empty,
                TrailerUrl = ms.Movie.TrailerUrl ?? string.Empty,
                Duration = ms.Movie.Duration,
                Director = ms.Movie.Director,
                Cast = ms.Movie.Cast,
                CreatedAt = ms.Movie.CreatedAt,
                Score = ms.MovieScoreValue
            })
            .FirstOrDefaultAsync();

        if (movie == null)
            throw new Exception();

        return movie;
    }

    public async Task<PopularGenreDto> HighestVotedGenreAsync(int groupId, string userId)
    {
        await EnsureMemberAsync(groupId, userId);

        var votingGenres = await dbContext.MovieScores
            .Where(ms => ms.Game != null && !ms.Game.IsActive && ms.Game.GroupId == groupId)
            .Where(ms => ms.Movie != null)
            .Include(ms => ms.Movie)
            .Select(ms => new { ms.Movie!.Genre, Score = ms.MovieScoreValue })
            .ToListAsync();

        var ratingGenres = await dbContext.RatingGuessingGames
            .Where(rg => rg.GroupId == groupId && !rg.IsActive)
            .Include(rg => rg.Movies)
            .SelectMany(rg => rg.Movies)
            .Select(m => new { m.Genre, Score = 1 })
            .ToListAsync();

        var result = votingGenres
            .Concat(ratingGenres)
            .GroupBy(x => x.Genre)
            .Select(g => new PopularGenreDto
            {
                Genre = g.Key.ToString(),
                TotalScore = g.Sum(x => x.Score)
            })
            .OrderByDescending(g => g.TotalScore)
            .FirstOrDefault();

        if (result == null)
            throw new Exception();

        return result;
    }

    public async Task<double> GetAverageMovieScoreAsync(int groupId, string userId)
    {
        await EnsureMemberAsync(groupId, userId);

        var query = dbContext.MovieScores
            .Where(ms => ms.Game != null && !ms.Game.IsActive && ms.Game.GroupId == groupId);

        return await query.AnyAsync()
            ? Math.Round(await query.AverageAsync(ms => ms.MovieScoreValue), 2)
            : 0.0;
    }

    public async Task<double> GetAverageMovieDurationAsync(int groupId, string userId)
    {
        await EnsureMemberAsync(groupId, userId);

        var query = dbContext.MovieScores
            .Where(ms => ms.Game != null && !ms.Game.IsActive && ms.Game.GroupId == groupId)
            .Where(ms => ms.Movie != null)
            .Include(ms => ms.Movie);

        return await query.AnyAsync()
            ? Math.Round(await query.AverageAsync(ms => ms.Movie!.Duration), 2)
            : 0.0;
    }
}
