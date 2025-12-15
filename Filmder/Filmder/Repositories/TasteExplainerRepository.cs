using Filmder.Data;
using Filmder.Models;
using Microsoft.EntityFrameworkCore;

namespace Filmder.Repositories;

public class TasteExplainerRepository : ITasteExplainerRepository
{
    private readonly AppDbContext _context;

    public TasteExplainerRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Dictionary<int, Rating>> GetUserRatingsWithMoviesAsync(string userId)
    {
        return await _context.Ratings
            .Where(r => r.UserId == userId)
            .Include(r => r.Movie)
            .ToDictionaryAsync(r => r.MovieId);
    }

    public async Task<List<UserMovie>> GetUserMoviesWithMoviesAsync(string userId, int take)
    {
        return await _context.UserMovies
            .Where(um => um.UserId == userId)
            .Include(um => um.Movie)
            .OrderByDescending(um => um.WatchedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<List<Rating>> GetUserRatingsAsync(string userId)
    {
        return await _context.Ratings
            .Where(r => r.UserId == userId)
            .Include(r => r.Movie)
            .ToListAsync();
    }

    public async Task<List<UserMovie>> GetUserMoviesAsync(string userId)
    {
        return await _context.UserMovies
            .Where(um => um.UserId == userId)
            .Include(um => um.Movie)
            .ToListAsync();
    }
    
    public async Task<List<SwipeHistory>> GetLikedSwipesWithMovieAsync(string userId)
    {
        return await _context.SwipeHistories
            .Where(s => s.UserId == userId && s.IsLike)
            .Include(s => s.Movie)
            .ToListAsync();
    }
}