using Filmder.Data;
using Filmder.Models;
using Microsoft.EntityFrameworkCore;

namespace Filmder.Repositories;

public class SwipeRepository : ISwipeRepository
{
    private readonly AppDbContext _context;

    public SwipeRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<int>> GetSwipedMovieIdsAsync(string userId)
    {
        return await _context.SwipeHistories
            .Where(sh => sh.UserId == userId)
            .Select(sh => sh.MovieId)
            .ToListAsync();
    }

    public async Task<SwipeHistory?> GetSwipeAsync(string userId, int movieId)
    {
        return await _context.SwipeHistories
            .FirstOrDefaultAsync(sh => sh.UserId == userId && sh.MovieId == movieId);
    }

    public async Task<SwipeHistory?> GetSwipeByIdAsync(int swipeId, string userId)
    {
        return await _context.SwipeHistories
            .FirstOrDefaultAsync(sh => sh.Id == swipeId && sh.UserId == userId);
    }

    public async Task AddSwipeAsync(SwipeHistory swipeHistory)
    {
        _context.SwipeHistories.Add(swipeHistory);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteSwipeAsync(SwipeHistory swipe)
    {
        _context.SwipeHistories.Remove(swipe);
        await _context.SaveChangesAsync();
    }

    public async Task<List<SwipeHistory>> GetSwipeHistoryAsync(string userId, bool? onlyLikes, int page, int pageSize)
    {
        IQueryable<SwipeHistory> query = _context.SwipeHistories
            .Where(sh => sh.UserId == userId);

        if (onlyLikes.HasValue)
        {
            query = query.Where(sh => sh.IsLike == onlyLikes.Value);
        }

        return await query
            .Include(sh => sh.Movie)
            .OrderByDescending(sh => sh.SwipedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<Movie>> GetLikedMoviesAsync(string userId, int page, int pageSize)
    {
        return await _context.SwipeHistories
            .Where(sh => sh.UserId == userId && sh.IsLike)
            .Include(sh => sh.Movie)
            .OrderByDescending(sh => sh.SwipedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(sh => sh.Movie)
            .ToListAsync();
    }

    public async Task<int> GetTotalSwipesCountAsync(string userId)
    {
        return await _context.SwipeHistories
            .Where(sh => sh.UserId == userId)
            .CountAsync();
    }

    public async Task<int> GetTotalLikesCountAsync(string userId)
    {
        return await _context.SwipeHistories
            .Where(sh => sh.UserId == userId && sh.IsLike)
            .CountAsync();
    }

    public async Task<string?> GetFavoriteGenreAsync(string userId)
    {
        return await _context.SwipeHistories
            .Where(sh => sh.UserId == userId && sh.IsLike)
            .Include(sh => sh.Movie)
            .GroupBy(sh => sh.Movie.Genre)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key.ToString())
            .FirstOrDefaultAsync();
    }

    public async Task<List<SwipeHistory>> GetLikedSwipesWithMovieAsync(string userId)
    {
        return await _context.SwipeHistories
            .Where(sh => sh.UserId == userId && sh.IsLike)
            .Include(sh => sh.Movie)
            .ToListAsync();
    }
}