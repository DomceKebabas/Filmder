using Filmder.DTOs;
using Filmder.Repositories;

namespace Filmder.Services;

public class TasteExplainerService : ITasteExplainerService
{
    private readonly ITasteExplainerRepository _repository;
    private readonly IAIService _aiService;

    public TasteExplainerService(ITasteExplainerRepository repository, IAIService aiService)
    {
        _repository = repository;
        _aiService = aiService;
    }

    public async Task<TasteExplanationDto?> GetTasteExplanationAsync(string userId)
    {
        var userRatings = await _repository.GetUserRatingsWithMoviesAsync(userId);
        var userMovies = await _repository.GetUserMoviesWithMoviesAsync(userId, 50);

        // Build watched movies list, joining ratings from the Ratings table
        var watchedMovies = userMovies
            .Select(um => new UserMovieTasteDto
            {
                MovieName = um.Movie.Name,
                Genre = um.Movie.Genre.ToString(),
                ReleaseYear = um.Movie.ReleaseYear,
                Director = um.Movie.Director,
                UserRating = userRatings.TryGetValue(um.MovieId, out var rating) ? rating.Score : null,
                UserComment = userRatings.GetValueOrDefault(um.MovieId)?.Comment,
                WatchedAt = um.WatchedAt
            })
            .ToList();

        var userMovieIds = userMovies.Select(um => um.MovieId).ToHashSet();
        var ratingsWithoutUserMovie = userRatings
            .Where(kvp => !userMovieIds.Contains(kvp.Key))
            .Select(kvp => new UserMovieTasteDto
            {
                MovieName = kvp.Value.Movie.Name,
                Genre = kvp.Value.Movie.Genre.ToString(),
                ReleaseYear = kvp.Value.Movie.ReleaseYear,
                Director = kvp.Value.Movie.Director,
                UserRating = kvp.Value.Score,
                UserComment = kvp.Value.Comment,
                WatchedAt = kvp.Value.CreatedAt
            })
            .ToList();

        watchedMovies.AddRange(ratingsWithoutUserMovie);

        if (HasNoWatchedMovies(watchedMovies))
        {
            return null; // Controller will return NotFound
        }

        if (HasNoRatedMovies(watchedMovies))
        {
            return GetNoRatingsResponse(watchedMovies);
        }

        // Filter to only movies with ratings for better analysis
        var ratedMovies = watchedMovies.Where(m => m.UserRating.HasValue).ToList();

        // Use rated movies for AI analysis (better insights)
        return await _aiService.ExplainUserTaste(ratedMovies);
    }

    public async Task<object?> GetTasteSummaryAsync(string userId)
    {
        var userRatings = await _repository.GetUserRatingsAsync(userId);
        var userMovies = await _repository.GetUserMoviesAsync(userId);

        var ratingsByMovieId = userRatings.ToDictionary(r => r.MovieId);

        var summary = new
        {
            totalWatched = userMovies.Count,
            totalRated = userRatings.Count,
            averageRating = userRatings.Any() ? Math.Round(userRatings.Average(r => r.Score), 1) : 0,
            favoriteGenre = userRatings.Any()
                ? userRatings
                    .GroupBy(r => r.Movie.Genre)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()?.Key.ToString() ?? "None yet"
                : userMovies.Any()
                    ? userMovies
                        .GroupBy(um => um.Movie.Genre)
                        .OrderByDescending(g => g.Count())
                        .FirstOrDefault()?.Key.ToString() ?? "None yet"
                    : "None yet",
            recentlyWatched = userMovies
                .OrderByDescending(um => um.WatchedAt)
                .Take(5)
                .Select(um => new
                {
                    um.Movie.Name,
                    um.Movie.Genre,
                    Rating = ratingsByMovieId.TryGetValue(um.MovieId, out var r) ? r.Score : (int?)null,
                    um.WatchedAt
                })
                .ToList()
        };

        return summary;
    }

    public bool HasNoWatchedMovies(List<UserMovieTasteDto> watchedMovies)
    {
        return !watchedMovies.Any();
    }

    public bool HasNoRatedMovies(List<UserMovieTasteDto> watchedMovies)
    {
        return !watchedMovies.Any(m => m.UserRating.HasValue);
    }

    public TasteExplanationDto GetNoRatingsResponse(List<UserMovieTasteDto> watchedMovies)
    {
        return new TasteExplanationDto
        {
            OverallTasteProfile = "You've watched some movies, but haven't rated them yet. Rate your watched movies to get a detailed taste analysis!",
            WatchingPersonality = "The Silent Watcher - Enjoys movies but keeps opinions private",
            Insights = new List<TasteInsightDto>
            {
                new TasteInsightDto
                {
                    Category = "Viewing Habits",
                    Explanation = $"You've watched {watchedMovies.Count} movies. Start rating them to unlock detailed insights!",
                    ExampleMovies = watchedMovies.Take(3).Select(m => m.MovieName).ToList()
                }
            }
        };
    }
}