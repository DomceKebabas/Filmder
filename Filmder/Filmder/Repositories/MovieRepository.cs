using Filmder.Data;
using Filmder.Models;
using Microsoft.EntityFrameworkCore;

namespace Filmder.Repositories;

public class MovieRepository : IMovieRepository
{
    private readonly AppDbContext _context;

    public MovieRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Movie?> GetByIdAsync(int movieId)
    {
        return await _context.Movies.FindAsync(movieId);
    }

    public async Task<List<Movie>> GetAllAsync(int page, int pageSize)
    {
        return await _context.Movies
            .OrderByDescending(m => m.Rating)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<Movie>> SearchAsync(string query, int limit)
    {
        return await _context.Movies
            .Where(m => m.Name.Contains(query) ||
                       m.Director.Contains(query) ||
                       m.Cast.Contains(query))
            .OrderByDescending(m => m.Rating)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<Movie>> GetByGenreAsync(MovieGenre genre, int page, int pageSize)
    {
        return await _context.Movies
            .Where(m => m.Genre == genre)
            .OrderByDescending(m => m.Rating)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountAsync()
    {
        return await _context.Movies.CountAsync();
    }

    public async Task<Movie?> GetByIndexAsync(int index)
    {
        return await _context.Movies
            .OrderBy(m => m.Id)
            .Skip(index)
            .Take(1)
            .AsNoTracking()
            .FirstOrDefaultAsync();
    }

    public async Task AddAsync(Movie movie)
    {
        _context.Movies.Add(movie);
    }

    public async Task DeleteAsync(Movie movie)
    {
        _context.Movies.Remove(movie);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
    
    public async Task<bool> ExistsAsync(int movieId)
    {
        return await _context.Movies.AnyAsync(m => m.Id == movieId);
    }

    public async Task<Movie?> GetRandomMovieAsync(List<int> excludeIds, string? genre, int? minYear, int? maxDuration)
    {
        var query = _context.Movies.AsQueryable();

        if (excludeIds.Any())
        {
            query = query.Where(m => !excludeIds.Contains(m.Id));
        }

        if (!string.IsNullOrEmpty(genre))
        {
            if (Enum.TryParse<MovieGenre>(genre, true, out var parsedGenre))
            {
                query = query.Where(m => m.Genre == parsedGenre);
            }
        }

        if (minYear.HasValue)
        {
            query = query.Where(m => m.ReleaseYear >= minYear.Value);
        }

        if (maxDuration.HasValue)
        {
            query = query.Where(m => m.Duration <= maxDuration.Value);
        }

        var totalMovies = await query.CountAsync();
        
        if (totalMovies == 0)
            return null;

        var random = new Random();
        var randomSkip = random.Next(0, totalMovies);
        
        return await query
            .Skip(randomSkip)
            .FirstOrDefaultAsync();
    }
}