using Filmder.Models;

namespace Filmder.Repositories;

public interface IPersonalizedPlaylistRepository
{
    Task<Dictionary<int, Rating>> GetUserRatingsAsync(string userId);
    Task<List<UserMovie>> GetRecentUserMoviesAsync(string userId, DateTime since, int take);
    Task<List<UserMovie>> GetAllUserMoviesAsync(string userId, int take);
    Task<List<Movie>> GetMoviesByIdsAsync(List<int> movieIds);
    Task<HashSet<int>> GetWatchedMovieIdsAsync(string userId);
    Task<Movie?> FindMatchingMovieAsync(string movieName, int releaseYear, HashSet<int> excludeIds);
    Task<List<int>> GetHighRatedMovieIdsAsync(string userId, int minScore);
    Task<List<MovieGenre>> GetFavoriteGenresAsync(List<int> movieIds, int take);
    Task<List<Movie>> GetUnwatchedMoviesByGenresAsync(HashSet<int> watchedIds, List<MovieGenre> genres, double minRating);
}