using Filmder.Data;
using Filmder.Models;
using Microsoft.EntityFrameworkCore;

namespace Filmder.Repositories;

public class RatingRepository : IRatingRepository
{
    private readonly AppDbContext _context;

    public RatingRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Rating?> GetByUserAndMovieAsync(string userId, int movieId)
    {
        return await _context.Ratings
            .FirstOrDefaultAsync(r => r.UserId == userId && r.MovieId == movieId);
    }

    public async Task<List<Rating>> GetByMovieIdAsync(int movieId)
    {
        return await _context.Ratings
            .Where(r => r.MovieId == movieId)
            .ToListAsync();
    }

    public async Task<List<Rating>> GetByMovieIdWithUserAsync(int movieId)
    {
        return await _context.Ratings
            .Where(r => r.MovieId == movieId)
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(Rating rating)
    {
        _context.Ratings.Add(rating);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}