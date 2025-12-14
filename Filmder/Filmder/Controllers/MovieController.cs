using Filmder.DTOs;
using Filmder.Models;
using Filmder.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Filmder.Controllers;

[EnableRateLimiting("DefaultBucket")]
[ApiController]
[Route("api/[controller]")]
public class MovieController : ControllerBase
{
    private readonly IMovieService _movieService;

    public MovieController(IMovieService movieService)
    {
        _movieService = movieService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<Movie>>> GetAllMovies([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var movies = await _movieService.GetAllMoviesAsync(page, pageSize);
        return Ok(movies);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<Movie>> GetMovieById(int id)
    {
        var movie = await _movieService.GetMovieByIdAsync(id);

        if (movie == null)
            return NotFound($"Movie with ID {id} not found");

        return Ok(movie);
    }

    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<ActionResult<List<Movie>>> SearchMovies([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest("Search query cannot be empty");

        var movies = await _movieService.SearchMoviesAsync(query);
        return Ok(movies);
    }

    [HttpGet("genre/{genre}")]
    [AllowAnonymous]
    public async Task<ActionResult<List<Movie>>> GetMoviesByGenre(string genre, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var (success, errorMessage, movies) = await _movieService.GetMoviesByGenreAsync(genre, page, pageSize);

        if (!success)
            return BadRequest(errorMessage);

        return Ok(movies);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Movie>> CreateMovie([FromBody] CreateMovieDto dto)
    {
        var (success, errorMessage, movie) = await _movieService.CreateMovieAsync(dto);

        if (!success)
            return BadRequest(errorMessage);

        return CreatedAtAction(nameof(GetMovieById), new { id = movie!.Id }, movie);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult> UpdateMovie(int id, [FromBody] CreateMovieDto dto)
    {
        var (success, errorMessage) = await _movieService.UpdateMovieAsync(id, dto);

        if (!success)
        {
            if (errorMessage == "Movie not found")
                return NotFound();
            return BadRequest(errorMessage);
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<ActionResult> DeleteMovie(int id)
    {
        var (success, errorMessage) = await _movieService.DeleteMovieAsync(id);

        if (!success)
            return NotFound();

        return NoContent();
    }

    [HttpPost("import")]
    public async Task<IActionResult> ImportMovies()
    {
        int added = await _movieService.ImportMoviesAsync();

        if (added == 0)
            return BadRequest(new { message = "No movies were imported. The file may be missing, empty, or contain only duplicates." });

        return Ok(new { message = $"{added} movies imported successfully." });
    }

    [HttpGet("daily")]
    [AllowAnonymous]
    public async Task<ActionResult<DailyMovieDto>> GetDailyMovie()
    {
        var (success, errorMessage, dailyMovie) = await _movieService.GetDailyMovieAsync();

        if (!success)
            return NotFound(errorMessage);

        return Ok(dailyMovie);
    }
}