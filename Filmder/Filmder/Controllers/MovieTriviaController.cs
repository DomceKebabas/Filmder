using System.Security.Claims;
using Filmder.Data;
using Filmder.DTOs;
using Filmder.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Filmder.Controllers;

[EnableRateLimiting("ExpensiveDaily")]
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MovieTriviaController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAIService _aiService;
    private static readonly Dictionary<string, MovieTriviaDto> _triviaCache = new();

    public MovieTriviaController(AppDbContext context, IAIService aiService)
    {
        _context = context;
        _aiService = aiService;
    }

    [HttpGet("available-movies")]
    public async Task<ActionResult<List<object>>> GetAvailableMovies()
    {
        var movies = await _context.Movies
            .OrderByDescending(m => m.Rating)
            .Take(50)
            .Select(m => new
            {
                m.Id,
                m.Name,
                m.Genre,
                m.ReleaseYear,
                m.Rating,
                m.PosterUrl,
                m.Director
            })
            .ToListAsync();

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

        var movie = await _context.Movies.FindAsync(movieId);
        if (movie == null)
        {
            return NotFound(new { message = "Movie not found" });
        }

        try
        {
            var trivia = await _aiService.GenerateMovieTrivia(
                movie.Name,
                movie.ReleaseYear,
                movie.Genre.ToString(),
                movie.Director,
                movie.Description,
                questionCount
            );

            if (trivia == null || !trivia.Questions.Any())
            {
                return StatusCode(500, new { message = "Failed to generate trivia questions" });
            }

            var cacheKey = $"{userId}_{movieId}";
            _triviaCache[cacheKey] = trivia;

            return Ok(trivia);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error generating trivia", details = ex.Message });
        }
    }

    [HttpPost("submit")]
    public Task<ActionResult<TriviaResultDto>> SubmitAnswers([FromBody] TriviaSubmissionDto submission)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Task.FromResult<ActionResult<TriviaResultDto>>(Unauthorized());

        if (submission.Answers == null || !submission.Answers.Any())
        {
            return Task.FromResult<ActionResult<TriviaResultDto>>(BadRequest(new { message = "No answers provided" }));
        }

        var cacheKey = $"{userId}_{submission.MovieId}";
        if (!_triviaCache.TryGetValue(cacheKey, out var trivia))
        {
            return Task.FromResult<ActionResult<TriviaResultDto>>(
                BadRequest(new { message = "No questions found. Generate trivia first." }));
        }

        int correctCount = 0;
        var results = new List<QuestionResultDto>();

        foreach (var answer in submission.Answers)
        {
            if (answer.QuestionIndex >= 0 && answer.QuestionIndex < trivia.Questions.Count)
            {
                var question = trivia.Questions[answer.QuestionIndex];
                bool isCorrect = answer.SelectedAnswerIndex == question.CorrectAnswerIndex;
                
                if (isCorrect) correctCount++;

                results.Add(new QuestionResultDto
                {
                    Question = question.Question,
                    IsCorrect = isCorrect,
                    UserAnswer = question.Options[answer.SelectedAnswerIndex],
                    CorrectAnswer = question.Options[question.CorrectAnswerIndex]
                });
            }
        }

        _triviaCache.Remove(cacheKey);

        double score = Math.Round((double)correctCount / trivia.Questions.Count * 100, 1);

        var result = new TriviaResultDto
        {
            TotalQuestions = trivia.Questions.Count,
            CorrectAnswers = correctCount,
            Score = score,
            QuestionResults = results
        };

        return Task.FromResult<ActionResult<TriviaResultDto>>(Ok(result));
    }
}

public class TriviaSubmissionDto
{
    public int MovieId { get; set; }
    public List<TriviaAnswerDto> Answers { get; set; } = new();
}