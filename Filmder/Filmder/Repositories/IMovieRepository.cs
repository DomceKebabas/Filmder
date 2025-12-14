using Filmder.Models;

namespace Filmder.Repositories;

public interface IMovieRepository
{
    Task<Movie?> GetByIdAsync(int movieId);
    Task<List<Movie>> GetAllAsync(int page, int pageSize);
    Task<List<Movie>> SearchAsync(string query, int limit);
    Task<List<Movie>> GetByGenreAsync(MovieGenre genre, int page, int pageSize);
    Task<int> GetCountAsync();
    Task<Movie?> GetByIndexAsync(int index);
    Task AddAsync(Movie movie);
    Task DeleteAsync(Movie movie);
    Task SaveChangesAsync();
}