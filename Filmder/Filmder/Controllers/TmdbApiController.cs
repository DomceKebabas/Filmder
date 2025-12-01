using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

[EnableRateLimiting("ExpensiveDaily")]
[ApiController]
[Route("api/[controller]")]
public class TmdbApiController : ControllerBase
{
    private readonly TmdbApiService _tmdb;

    public TmdbApiController(TmdbApiService tmdb)
    {
        _tmdb = tmdb;
    }

    [HttpGet("trending")]
    public async Task<IActionResult> Trending([FromQuery] int page = 1)
    {
        return Ok(await _tmdb.GetTrendingMovies(page));
    }

    [HttpGet("popular")]
    public async Task<IActionResult> Popular([FromQuery] int page = 1)
    {
        return Ok(await _tmdb.GetPopularMovies(page));
    }

    [HttpGet("toprated")]
    public async Task<IActionResult> TopRated([FromQuery] int page = 1)
    {
        return Ok(await _tmdb.GetTopRatedMovies(page));
    }

    [HttpGet("upcoming")]
    public async Task<IActionResult> Upcoming([FromQuery] int page = 1)
    {
        return Ok(await _tmdb.GetUpcomingMovies(page));
    }
}