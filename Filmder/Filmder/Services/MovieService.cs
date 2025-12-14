using Filmder.DTOs;
using Filmder.Extensions;
using Filmder.Models;
using Filmder.Repositories;

namespace Filmder.Services;

public class MovieService : IMovieService
{
    private readonly IMovieRepository _repository;
    private readonly IMovieCacheService _movieCache;
    private readonly MovieImportService _importService;

    public MovieService(IMovieRepository repository, IMovieCacheService movieCache, MovieImportService importService)
    {
        _repository = repository;
        _movieCache = movieCache;
        _importService = importService;
    }

    public async Task<List<Movie>> GetAllMoviesAsync(int page, int pageSize)
    {
        return await _repository.GetAllAsync(page, pageSize);
    }

    public async Task<Movie?> GetMovieByIdAsync(int id)
    {
        return await _movieCache.GetMovieByIdAsync(id);
    }

    public async Task<List<Movie>> SearchMoviesAsync(string query)
    {
        return await _repository.SearchAsync(query, 50);
    }

    public async Task<(bool Success, string? ErrorMessage, List<Movie>? Movies)> GetMoviesByGenreAsync(string genre, int page, int pageSize)
    {
        if (!MovieGenreParsingExtensions.TryParseGenre(genre, out var parsed))
        {
            return (false, "Invalid genre", null);
        }

        var movies = await _repository.GetByGenreAsync(parsed, page, pageSize);
        return (true, null, movies);
    }

    public async Task<(bool Success, string? ErrorMessage, Movie? Movie)> CreateMovieAsync(CreateMovieDto dto)
    {
        if (!MovieGenreParsingExtensions.TryParseGenre(dto.Genre, out var createParsed))
        {
            return (false, "Invalid genre", null);
        }

        var movie = new Movie
        {
            Name = dto.Name,
            Genre = createParsed,
            Description = dto.Description,
            ReleaseYear = dto.ReleaseYear,
            Rating = dto.Rating,
            PosterUrl = dto.PosterUrl,
            TrailerUrl = dto.TrailerUrl,
            Duration = dto.Duration,
            Director = dto.Director,
            Cast = dto.Cast
        };

        await _repository.AddAsync(movie);
        await _repository.SaveChangesAsync();

        return (true, null, movie);
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateMovieAsync(int id, CreateMovieDto dto)
    {
        var movie = await _repository.GetByIdAsync(id);

        if (movie == null)
        {
            return (false, "Movie not found");
        }

        if (!MovieGenreParsingExtensions.TryParseGenre(dto.Genre, out var updateParsed))
        {
            return (false, "Invalid genre");
        }

        movie.Name = dto.Name;
        movie.Genre = updateParsed;
        movie.Description = dto.Description;
        movie.ReleaseYear = dto.ReleaseYear;
        movie.Rating = dto.Rating;
        movie.PosterUrl = dto.PosterUrl;
        movie.TrailerUrl = dto.TrailerUrl;
        movie.Duration = dto.Duration;
        movie.Director = dto.Director;
        movie.Cast = dto.Cast;

        await _repository.SaveChangesAsync();

        _movieCache.InvalidateCache(id);

        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteMovieAsync(int id)
    {
        var movie = await _repository.GetByIdAsync(id);

        if (movie == null)
        {
            return (false, "Movie not found");
        }

        await _repository.DeleteAsync(movie);
        await _repository.SaveChangesAsync();

        _movieCache.InvalidateCache(id);

        return (true, null);
    }

    public async Task<int> ImportMoviesAsync()
    {
        return await _importService.ImportMoviesFromFileAsync(filePath: "movies.json");
    }

    public async Task<(bool Success, string? ErrorMessage, DailyMovieDto? DailyMovie)> GetDailyMovieAsync()
    {
        var today = DateTime.UtcNow.Date;
        int seed = today.DayOfYear + today.Year;

        int movieCount = await _repository.GetCountAsync();
        if (movieCount == 0)
        {
            return (false, "No movies available.", null);
        }

        int index = new Random(seed).Next(movieCount);

        var movie = await _repository.GetByIndexAsync(index);

        if (movie == null)
        {
            return (false, "No movies found.", null);
        }

        var nextReset = today.AddDays(1);
        var timeRemaining = nextReset - DateTime.UtcNow;

        var dto = new DailyMovieDto
        {
            Name = movie.Name,
            Genre = movie.Genre.ToString(),
            Description = movie.Description,
            PosterUrl = movie.PosterUrl ?? string.Empty,
            TrailerUrl = movie.TrailerUrl ?? string.Empty,
            Rating = movie.Rating,
            ReleaseYear = movie.ReleaseYear,
            CountdownSeconds = (int)timeRemaining.TotalSeconds,
            NextUpdate = nextReset
        };

        return (true, null, dto);
    }
}