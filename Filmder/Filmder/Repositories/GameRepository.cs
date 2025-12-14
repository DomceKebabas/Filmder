using Filmder.Data;
using Filmder.DTOs;
using Filmder.Extensions;
using Filmder.Interfaces;
using Filmder.Models;
using Microsoft.EntityFrameworkCore;

namespace Filmder.Services;

public class GameRepository(AppDbContext dbContext) : IGameRepository
{
    public Game CreateGame(CreateGameDto dto)
    {
        var users = dbContext.Users
            .Where(u => dto.UserEmails.Contains(u.Email))
            .ToList();

        var game = new Game
        {
            Name = dto.Name,
            Users = users,
            GroupId = dto.GroupId,
            Movies = dto.Movies,
            MovieScores = dto.MovieScores
        };

        dbContext.Games.Add(game);
        dbContext.SaveChanges();

        return game;
    }

    public async Task VoteAsync(VoteDto dto)
    {
        var game = await dbContext.Games
            .Include(g => g.MovieScores)
            .FirstOrDefaultAsync(g => g.Id == dto.GameId);

        if (game == null) return;

        var movieScore = game.MovieScores.FirstOrDefault(ms => ms.MovieId == dto.MovieId);
        if (movieScore == null)
        {
            movieScore = new MovieScore
            {
                MovieId = dto.MovieId,
                GameId = dto.GameId,
                MovieScoreValue = dto.Score
            };
            game.MovieScores.Add(movieScore);
        }
        else
        {
            movieScore.MovieScoreValue += dto.Score;
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task<List<Movie>> GetMoviesByCriteriaAsync(
        string? genre,
        int? releaseDate,
        int? longestDurationMinutes,
        int movieCount)
    {
        var movies = dbContext.Movies.AsQueryable();

        if (longestDurationMinutes.HasValue)
            movies = movies.Where(m => m.Duration <= longestDurationMinutes.Value);

        if (releaseDate.HasValue)
            movies = movies.Where(m => m.ReleaseYear >= releaseDate.Value);

        if (!string.IsNullOrEmpty(genre))
        {
            if (MovieGenreParsingExtensions.TryParseGenre(genre, out var parsedGenre))
                movies = movies.Where(m => m.Genre == parsedGenre);
            else
                throw new Exception("Invalid genre");
        }

        return await movies.Take(movieCount).ToListAsync();
    }

    public async Task<List<Movie>> GetResultsAsync(int gameId)
    {
        var game = await dbContext.Games
            .Include(g => g.MovieScores)
            .Include(g => g.Movies)
            .FirstOrDefaultAsync(g => g.Id == gameId);

        if (game == null) return new();

        var topMovies = game.MovieScores
            .Take(5)
            .Select(ms => game.Movies.FirstOrDefault(m => m.Id == ms.MovieId))
            .ToList();

        topMovies.Sort();
        return topMovies;
    }

    public async Task<Game> EndGameAsync(int gameId)
    {
        var game = await dbContext.Games.FindAsync(gameId);
        if (game == null) throw new Exception("Game not found");

        game.IsActive = false;
        await dbContext.SaveChangesAsync();

        return game;
    }

    public async Task<List<Game>> GetActiveGamesAsync(int groupId)
    {
        return await dbContext.Games
            .Include(g => g.Users)
            .Where(g => g.IsActive && g.GroupId == groupId)
            .ToListAsync();
    }

    public async Task<object> GetGameResultsAsync(int gameId)
    {
        var game = await dbContext.Games
            .Include(g => g.MovieScores)
            .ThenInclude(ms => ms.Movie)
            .FirstOrDefaultAsync(g => g.Id == gameId);

        if (game == null) throw new Exception("Game not found");

        var movieScores = game.MovieScores
            .OrderByDescending(ms => ms.MovieScoreValue)
            .Select(ms => new
            {
                Score = ms.MovieScoreValue,
                Movie = new
                {
                    ms.Movie!.Id,
                    ms.Movie.Name,
                    ms.Movie.Genre,
                    ms.Movie.Description,
                    ms.Movie.ReleaseYear,
                    ms.Movie.Rating,
                    ms.Movie.PosterUrl,
                    ms.Movie.Duration,
                    ms.Movie.Director
                }
            })
            .ToList();

        return new
        {
            MovieScores = movieScores,
            GameInfo = new
            {
                game.Id,
                game.Name,
                PlayerCount = 0,
                game.IsActive
            }
        };
    }

    public async Task<List<object>> GetAllGamesByGroupAsync(int groupId, string userId)
    {
        var isMember = await dbContext.GroupMembers
            .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

        if (!isMember) throw new UnauthorizedAccessException();

        var games = await dbContext.Games
            .Include(g => g.Users)
            .Include(g => g.MovieScores)
            .ThenInclude(ms => ms.Movie)
            .Where(g => g.GroupId == groupId)
            .OrderByDescending(g => g.Id)
            .ToListAsync();

        return games.Select(g => new
        {
            g.Id,
            g.Name,
            g.IsActive,
            g.GroupId,
            Users = g.Users.Select(u => new { u.Id, u.Email }).ToList(),
            MovieScores = g.MovieScores
                .OrderByDescending(ms => ms.MovieScoreValue)
                .Select(ms => new
                {
                    ms.Id,
                    ms.MovieId,
                    ms.MovieScoreValue,
                    Movie = ms.Movie == null ? null : new
                    {
                        ms.Movie.Id,
                        ms.Movie.Name,
                        ms.Movie.Genre,
                        ms.Movie.Rating,
                        ms.Movie.ReleaseYear,
                        ms.Movie.PosterUrl,
                        ms.Movie.Duration
                    }
                }).ToList()
        }).Cast<object>().ToList();
    }
}
