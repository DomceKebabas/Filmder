using Filmder.DTOs;
using Filmder.Models;
using Filmder.Repositories;

namespace Filmder.Services;

public class WatchlistService : IWatchlistService
{
    private readonly IWatchlistRepository _repository;

    public WatchlistService(IWatchlistRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<WatchlistMovieDto>> GenerateWatchlistAsync(string userId, int count)
    {
        var genreScores = InitializeGenreScores();
        
        var ratings = await _repository.GetUserHighRatingsAsync(userId);
        foreach (var rating in ratings)
        {
            if (rating.Movie != null)
            {
                genreScores[rating.Movie.Genre] += (rating.Score - 6) * 2.0;
            }
        }

        var gameVotes = await _repository.GetUserGameVotesAsync(userId);
        foreach (var vote in gameVotes)
        {
            if (vote.Movie != null && vote.Game != null)
            {
                genreScores[vote.Movie.Genre] += vote.MovieScoreValue / 100.0;
            }
        }

        var topGenres = GetTopGenres(genreScores, 3);
        var seenMovieIds = await _repository.GetSeenMovieIdsAsync(userId);

        var movies = topGenres.Any()
            ? await _repository.GetMoviesByGenresAsync(topGenres, seenMovieIds, count)
            : await _repository.GetTopRatedMoviesAsync(seenMovieIds, count);

        return movies.Select(m => MapToWatchlistDto(m, genreScores)).ToList();
    }

    public async Task<UserPreferencesDto> GetUserPreferencesAsync(string userId)
    {
        var genreScores = new Dictionary<string, double>();

        var ratings = await _repository.GetAllUserRatingsAsync(userId);
        foreach (var rating in ratings)
        {
            if (rating.Movie == null) continue;

            var genre = rating.Movie.Genre.ToString();
            genreScores.TryAdd(genre, 0);

            if (rating.Score >= 7)
            {
                genreScores[genre] += (rating.Score - 6) * 2.0;
            }
        }

        var gameVotes = await _repository.GetUserGameVotesAsync(userId);
        foreach (var vote in gameVotes)
        {
            if (vote.Movie == null || vote.Game == null) continue;

            var genre = vote.Movie.Genre.ToString();
            genreScores.TryAdd(genre, 0);
            genreScores[genre] += vote.MovieScoreValue / 100.0;
        }

        var favoriteGenre = genreScores
            .OrderByDescending(kvp => kvp.Value)
            .FirstOrDefault();

        return new UserPreferencesDto
        {
            FavoriteGenre = favoriteGenre.Key ?? "None",
            GenreScores = genreScores
                .OrderByDescending(kvp => kvp.Value)
                .ToDictionary(kvp => kvp.Key, kvp => Math.Round(kvp.Value, 2)),
            TotalRatings = ratings.Count,
            TotalGameVotes = gameVotes.Count
        };
    }

    private static Dictionary<MovieGenre, double> InitializeGenreScores()
    {
        var scores = new Dictionary<MovieGenre, double>();
        foreach (MovieGenre genre in Enum.GetValues(typeof(MovieGenre)))
        {
            scores[genre] = 0;
        }
        return scores;
    }

    private static List<MovieGenre> GetTopGenres(Dictionary<MovieGenre, double> genreScores, int count)
    {
        return genreScores
            .Where(kvp => kvp.Value > 0)
            .OrderByDescending(kvp => kvp.Value)
            .Take(count)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    private static WatchlistMovieDto MapToWatchlistDto(Movie movie, Dictionary<MovieGenre, double> genreScores)
    {
        return new WatchlistMovieDto
        {
            Id = movie.Id,
            Name = movie.Name,
            Genre = movie.Genre.ToString(),
            Description = movie.Description,
            ReleaseYear = movie.ReleaseYear,
            Rating = movie.Rating,
            PosterUrl = movie.PosterUrl ?? string.Empty,
            TrailerUrl = movie.TrailerUrl ?? string.Empty,
            Duration = movie.Duration,
            Director = movie.Director,
            Cast = movie.Cast,
            RecommendationScore = CalculateRecommendationScore(movie, genreScores.GetValueOrDefault(movie.Genre, 0))
        };
    }

    private static double CalculateRecommendationScore(Movie movie, double genreScore)
    {
        var normalizedGenreScore = Math.Min(genreScore / 10.0, 10.0);
        return Math.Round((movie.Rating * 0.6 + normalizedGenreScore * 0.4) * 10, 2);
    }
}