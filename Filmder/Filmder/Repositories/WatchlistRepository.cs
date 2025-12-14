using Filmder.Data;
using Filmder.Models;
using Microsoft.EntityFrameworkCore;

namespace Filmder.Repositories;

public class WatchlistRepository : IWatchlistRepository
{
    private readonly AppDbContext _context;

    public WatchlistRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Rating>> GetUserHighRatingsAsync(string userId, int minScore = 7)
    {
        return await _context.Ratings
            .Include(r => r.Movie)
            .Where(r => r.UserId == userId && r.Score >= minScore)
            .ToListAsync();
    }

    public async Task<List<Rating>> GetAllUserRatingsAsync(string userId)
    {
        return await _context.Ratings
            .Include(r => r.Movie)
            .Where(r => r.UserId == userId)
            .ToListAsync();
    }

    public async Task<List<MovieScore>> GetUserGameVotesAsync(string userId, int minScoreValue = 50)
    {
        return await _context.MovieScores
            .Include(ms => ms.Movie)
            .Include(ms => ms.Game)
            .Where(ms => ms.Game != null 
                && ms.Game.Users.Any(u => u.Id == userId) 
                && ms.MovieScoreValue > minScoreValue)
            .ToListAsync();
    }

    public async Task<HashSet<int>> GetSeenMovieIdsAsync(string userId)
    {
        return await _context.Ratings
            .Where(r => r.UserId == userId)
            .Select(r => r.MovieId)
            .Union(_context.SwipeHistories
                .Where(sh => sh.UserId == userId)
                .Select(sh => sh.MovieId))
            .ToHashSetAsync();
    }

    public async Task<List<Movie>> GetMoviesByGenresAsync(
        List<MovieGenre> genres, 
        HashSet<int> excludeIds, 
        int count)
    {
        return await _context.Movies
            .Where(m => !excludeIds.Contains(m.Id) && genres.Contains(m.Genre))
            .OrderByDescending(m => m.Rating)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<Movie>> GetTopRatedMoviesAsync(HashSet<int> excludeIds, int count)
    {
        return await _context.Movies
            .Where(m => !excludeIds.Contains(m.Id))
            .OrderByDescending(m => m.Rating)
            .Take(count)
            .ToListAsync();
    }
}