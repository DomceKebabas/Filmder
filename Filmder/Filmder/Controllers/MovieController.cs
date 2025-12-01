using Filmder.Data;
using Filmder.DTOs;
using Filmder.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Filmder.Extensions;
using Filmder.Services;
using Microsoft.AspNetCore.RateLimiting;

namespace Filmder.Controllers;
[EnableRateLimiting("DefaultBucket")]
[ApiController]
[Route("api/[controller]")]
public class MovieController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly MovieImportService _importService;
    private readonly IMovieCacheService _movieCache;
    public MovieController(AppDbContext context, MovieImportService importService, IMovieCacheService movieCache)
    {
        _context = context;
        _importService = importService;
        _movieCache = movieCache;
    }
    
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<Movie>>> GetAllMovies([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var movies = await _context.Movies
            .OrderByDescending(m => m.Rating)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(movies);
        
        
    }
    
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<Movie>> GetMovieById(int id)
    {
        var movie = await _movieCache.GetMovieByIdAsync(id);

        if (movie == null)
            return NotFound($"Movie with ID {id} not found");

        return Ok(movie);
    }

    // Paieska pagal pavadinima
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<ActionResult<List<Movie>>> SearchMovies([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest("Search query cannot be empty");

        var movies = await _context.Movies
            .Where(m => m.Name.Contains(query) || 
                       m.Director.Contains(query) || 
                       m.Cast.Contains(query))
            .OrderByDescending(m => m.Rating)
            .Take(50)
            .ToListAsync();

        return Ok(movies);
    }

    // pagal zanra
    [HttpGet("genre/{genre}")]
    [AllowAnonymous]
    public async Task<ActionResult<List<Movie>>> GetMoviesByGenre(string genre, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (!MovieGenreParsingExtensions.TryParseGenre(genre, out var parsed))
            return BadRequest("Invalid genre");

        var movies = await _context.Movies
            .Where(m => m.Genre == parsed)
            .OrderByDescending(m => m.Rating)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(movies);
        
    }

    // admin funkcija (Speju taip darom)
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Movie>> CreateMovie([FromBody] CreateMovieDto dto)
    {
        if (!MovieGenreParsingExtensions.TryParseGenre(dto.Genre, out var createParsed))
            return BadRequest("Invalid genre");

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

        _context.Movies.Add(movie);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMovieById), new { id = movie.Id }, movie);
    }
    
    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult> UpdateMovie(int id, [FromBody] CreateMovieDto dto)
    {
        var movie = await _context.Movies.FindAsync(id);
        
        if (movie == null)
            return NotFound();

        movie.Name = dto.Name;
        if (!MovieGenreParsingExtensions.TryParseGenre(dto.Genre, out var updateParsed))
            return BadRequest("Invalid genre");
        movie.Genre = updateParsed;
        movie.Description = dto.Description;
        movie.ReleaseYear = dto.ReleaseYear;
        movie.Rating = dto.Rating;
        movie.PosterUrl = dto.PosterUrl;
        movie.TrailerUrl = dto.TrailerUrl;
        movie.Duration = dto.Duration;
        movie.Director = dto.Director;
        movie.Cast = dto.Cast;

        await _context.SaveChangesAsync();

        // Invalidate cache for this movie
        _movieCache.InvalidateCache(id);

        return NoContent();
    }
    
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<ActionResult> DeleteMovie(int id)
    {
        var movie = await _context.Movies.FindAsync(id);
        
        if (movie == null)
            return NotFound();

        _context.Movies.Remove(movie);
        await _context.SaveChangesAsync();

        // Invalidate cache for this movie
        _movieCache.InvalidateCache(id);
        
        return NoContent();
    }
    
    
    [HttpPost("import")]
    public async Task<IActionResult> ImportMovies()
    {
        int added = await _importService.ImportMoviesFromFileAsync(filePath: "movies.json");

        if (added == 0)
            return BadRequest(new { message = "No movies were imported. The file may be missing, empty, or contain only duplicates." });

        return Ok(new { message = $"{added} movies imported successfully." });
    }
    
    [HttpGet("daily")]
    [AllowAnonymous]
    public async Task<ActionResult<DailyMovieDto>> GetDailyMovie()
    {
        var today = DateTime.UtcNow.Date;
        int seed = today.DayOfYear + today.Year;

        int movieCount = await _context.Movies.CountAsync();
        if (movieCount == 0)
            return NotFound("No movies available.");

        int index = new Random(seed).Next(movieCount);

        var movie = await _context.Movies
            .OrderBy(m => m.Id)
            .Skip(index)
            .Take(1)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (movie == null)
            return NotFound("No movies found.");

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

        return Ok(dto);
    }




}