using Filmder.Data;
using Filmder.Models;
using Microsoft.EntityFrameworkCore;

namespace Filmder.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Rating>> GetRatingsWithMovieAsync(string userId)
    {
        return await _context.Ratings
            .Where(r => r.UserId == userId)
            .Include(r => r.Movie)
            .ToListAsync();
    }

    public async Task<List<UserMovie>> GetUserMoviesWithDetailsAsync(string userId)
    {
        return await _context.UserMovies
            .Where(um => um.UserId == userId)
            .Include(um => um.Movie)
            .Include(um => um.Rating)
            .ToListAsync();
    }

    public async Task<UserMovie?> GetUserMovieAsync(string userId, int movieId)
    {
        return await _context.UserMovies
            .FirstOrDefaultAsync(um => um.UserId == userId && um.MovieId == movieId);
    }

    public async Task AddRatingAsync(Rating rating)
    {
        _context.Ratings.Add(rating);
        await _context.SaveChangesAsync();
    }

    public async Task AddUserMovieAsync(UserMovie userMovie)
    {
        _context.UserMovies.Add(userMovie);
        await _context.SaveChangesAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}