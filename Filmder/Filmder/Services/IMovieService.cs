using Filmder.DTOs;
using Filmder.Models;

namespace Filmder.Services;

public interface IMovieService
{
    Task<List<Movie>> GetAllMoviesAsync(int page, int pageSize);
    Task<Movie?> GetMovieByIdAsync(int id);
    Task<List<Movie>> SearchMoviesAsync(string query);
    Task<(bool Success, string? ErrorMessage, List<Movie>? Movies)> GetMoviesByGenreAsync(string genre, int page, int pageSize);
    Task<(bool Success, string? ErrorMessage, Movie? Movie)> CreateMovieAsync(CreateMovieDto dto);
    Task<(bool Success, string? ErrorMessage)> UpdateMovieAsync(int id, CreateMovieDto dto);
    Task<(bool Success, string? ErrorMessage)> DeleteMovieAsync(int id);
    Task<int> ImportMoviesAsync();
    Task<(bool Success, string? ErrorMessage, DailyMovieDto? DailyMovie)> GetDailyMovieAsync();
}