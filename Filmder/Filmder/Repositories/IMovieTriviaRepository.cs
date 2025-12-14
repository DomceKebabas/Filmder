using Filmder.Models;

namespace Filmder.Repositories;

public interface IMovieTriviaRepository
{
    Task<List<Movie>> GetTopRatedMoviesAsync(int count);
    Task<Movie?> GetByIdAsync(int movieId);
}