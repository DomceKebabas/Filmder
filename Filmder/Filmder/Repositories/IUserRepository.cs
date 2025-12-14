using Filmder.Models;

namespace Filmder.Repositories;

public interface IUserRepository
{
    Task<List<Rating>> GetRatingsWithMovieAsync(string userId);
    Task<List<UserMovie>> GetUserMoviesWithDetailsAsync(string userId);
    Task<UserMovie?> GetUserMovieAsync(string userId, int movieId);
    Task AddRatingAsync(Rating rating);
    Task AddUserMovieAsync(UserMovie userMovie);
    Task SaveChangesAsync();
}