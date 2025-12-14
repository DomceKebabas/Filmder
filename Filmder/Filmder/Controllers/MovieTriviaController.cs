using System.Security.Claims;
using Filmder.DTOs;
using Filmder.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Filmder.Controllers;

[EnableRateLimiting("ExpensiveDaily")]
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MovieTriviaController : ControllerBase
{
    private readonly IMovieTriviaService _movieTriviaService;

    public MovieTriviaController(IMovieTriviaService movieTriviaService)
    {
        _movieTriviaService = movieTriviaService;
    }

    [HttpGet("available-movies")]
    public async Task<ActionResult<List<AvailableMovieDto>>> GetAvailableMovies()
    {
        var movies = await _movieTriviaService.GetAvailableMoviesAsync();
        return Ok(movies);
    }

    [HttpGet("generate/{movieId}")]
    public async Task<ActionResult<MovieTriviaDto>> GenerateTrivia(int movieId, [FromQuery] int questionCount = 10)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        if (questionCount < 5 || questionCount > 20)
        {
            return BadRequest(new { message = "Question count must be between 5 and 20" });
        }

        var (success, errorMessage, statusCode, trivia) = await _movieTriviaService.GenerateTriviaAsync(userId, movieId, questionCount);

        if (!success)
        {
            return statusCode switch
            {
                404 => NotFound(new { message = errorMessage }),
                500 => StatusCode(500, new { message = "Error generating trivia", details = errorMessage }),
                _ => BadRequest(new { message = errorMessage })
            };
        }

        return Ok(trivia);
    }

    [HttpPost("submit")]
    public ActionResult<TriviaResultDto> SubmitAnswers([FromBody] TriviaSubmissionDto submission)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var (success, errorMessage, statusCode, result) = _movieTriviaService.SubmitAnswers(userId, submission);

        if (!success)
        {
            return BadRequest(new { message = errorMessage });
        }

        return Ok(result);
    }
}