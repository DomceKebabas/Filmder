using Filmder.Models;

namespace Filmder.Repositories;

public interface ITasteExplainerRepository
{
    Task<Dictionary<int, Rating>> GetUserRatingsWithMoviesAsync(string userId);
    Task<List<UserMovie>> GetUserMoviesWithMoviesAsync(string userId, int take);
    Task<List<Rating>> GetUserRatingsAsync(string userId);
    Task<List<UserMovie>> GetUserMoviesAsync(string userId);
    Task<List<SwipeHistory>> GetLikedSwipesWithMovieAsync(string userId);
    
}