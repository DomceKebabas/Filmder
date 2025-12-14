using Filmder.Data;
using Filmder.Models;
using Microsoft.EntityFrameworkCore;

namespace Filmder.Repositories;

public class PersonalizedPlaylistRepository : IPersonalizedPlaylistRepository
{
    private readonly AppDbContext _context;

    public PersonalizedPlaylistRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Dictionary<int, Rating>> GetUserRatingsAsync(string userId)
    {
        return await _context.Ratings
            .Where(r => r.UserId == userId)
            .ToDictionaryAsync(r => r.MovieId);
    }

    public async Task<List<UserMovie>> GetRecentUserMoviesAsync(string userId, DateTime since, int take)
    {
        return await _context.UserMovies
            .Where(um => um.UserId == userId && um.WatchedAt >= since)
            .Include(um => um.Movie)
            .OrderByDescending(um => um.WatchedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<List<UserMovie>> GetAllUserMoviesAsync(string userId, int take)
    {
        return await _context.UserMovies
            .Where(um => um.UserId == userId)
            .Include(um => um.Movie)
            .OrderByDescending(um => um.WatchedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<List<Movie>> GetMoviesByIdsAsync(List<int> movieIds)
    {
        return await _context.Movies
            .Where(m => movieIds.Contains(m.Id))
            .ToListAsync();
    }

    public async Task<HashSet<int>> GetWatchedMovieIdsAsync(string userId)
    {
        return await _context.UserMovies
            .Where(um => um.UserId == userId)
            .Select(um => um.MovieId)
            .ToHashSetAsync();
    }

    public async Task<Movie?> FindMatchingMovieAsync(string movieName, int releaseYear, HashSet<int> excludeIds)
    {
        return await _context.Movies
            .Where(m => !excludeIds.Contains(m.Id))
            .Where(m => m.Name.Contains(movieName) || movieName.Contains(m.Name))
            .Where(m => Math.Abs(m.ReleaseYear - releaseYear) <= 1)
            .FirstOrDefaultAsync();
    }

    public async Task<List<int>> GetHighRatedMovieIdsAsync(string userId, int minScore)
    {
        return await _context.Ratings
            .Where(r => r.UserId == userId && r.Score >= minScore)
            .Select(r => r.MovieId)
            .ToListAsync();
    }

    public async Task<List<MovieGenre>> GetFavoriteGenresAsync(List<int> movieIds, int take)
    {
        return await _context.Movies
            .Where(m => movieIds.Contains(m.Id))
            .GroupBy(m => m.Genre)
            .OrderByDescending(g => g.Count())
            .Take(take)
            .Select(g => g.Key)
            .ToListAsync();
    }

    public async Task<List<Movie>> GetUnwatchedMoviesByGenresAsync(HashSet<int> watchedIds, List<MovieGenre> genres, double minRating)
    {
        return await _context.Movies
            .Where(m => !watchedIds.Contains(m.Id))
            .Where(m => genres.Contains(m.Genre))
            .Where(m => m.Rating >= minRating)
            .ToListAsync();
    }
}