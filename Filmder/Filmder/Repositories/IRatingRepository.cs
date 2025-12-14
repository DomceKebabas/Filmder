using Filmder.Models;

namespace Filmder.Repositories;

public interface IRatingRepository
{
    Task<Rating?> GetByUserAndMovieAsync(string userId, int movieId);
    Task<List<Rating>> GetByMovieIdAsync(int movieId);
    Task<List<Rating>> GetByMovieIdWithUserAsync(int movieId);
    Task AddAsync(Rating rating);
    Task SaveChangesAsync();
}