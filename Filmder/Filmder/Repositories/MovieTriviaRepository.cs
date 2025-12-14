using Filmder.Data;
using Filmder.Models;
using Microsoft.EntityFrameworkCore;

namespace Filmder.Repositories;

public class MovieTriviaRepository : IMovieTriviaRepository
{
    private readonly AppDbContext _context;

    public MovieTriviaRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Movie>> GetTopRatedMoviesAsync(int count)
    {
        return await _context.Movies
            .OrderByDescending(m => m.Rating)
            .Take(count)
            .ToListAsync();
    }

    public async Task<Movie?> GetByIdAsync(int movieId)
    {
        return await _context.Movies.FindAsync(movieId);
    }
}