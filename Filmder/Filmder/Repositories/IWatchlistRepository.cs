using Filmder.Models;

namespace Filmder.Repositories;

public interface IWatchlistRepository
{
    Task<List<Rating>> GetUserHighRatingsAsync(string userId, int minScore = 7);
    Task<List<MovieScore>> GetUserGameVotesAsync(string userId, int minScoreValue = 50);
    Task<HashSet<int>> GetSeenMovieIdsAsync(string userId);
    Task<List<Movie>> GetMoviesByGenresAsync(List<MovieGenre> genres, HashSet<int> excludeIds, int count);
    Task<List<Movie>> GetTopRatedMoviesAsync(HashSet<int> excludeIds, int count);
    Task<List<Rating>> GetAllUserRatingsAsync(string userId);
}