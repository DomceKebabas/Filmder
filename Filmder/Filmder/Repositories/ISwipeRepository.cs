using Filmder.Models;

namespace Filmder.Repositories;

public interface ISwipeRepository
{
    Task<List<int>> GetSwipedMovieIdsAsync(string userId);
    Task<SwipeHistory?> GetSwipeAsync(string userId, int movieId);
    Task<SwipeHistory?> GetSwipeByIdAsync(int swipeId, string userId);
    Task AddSwipeAsync(SwipeHistory swipeHistory);
    Task DeleteSwipeAsync(SwipeHistory swipe);
    Task<List<SwipeHistory>> GetSwipeHistoryAsync(string userId, bool? onlyLikes, int page, int pageSize);
    Task<List<Movie>> GetLikedMoviesAsync(string userId, int page, int pageSize);
    Task<int> GetTotalSwipesCountAsync(string userId);
    Task<int> GetTotalLikesCountAsync(string userId);
    Task<string?> GetFavoriteGenreAsync(string userId);
    Task<List<SwipeHistory>> GetLikedSwipesWithMovieAsync(string userId);
}