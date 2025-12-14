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
public class PersonalityMatchController : ControllerBase
{
    private readonly IPersonalityMatchService _personalityMatchService;

    public PersonalityMatchController(IPersonalityMatchService personalityMatchService)
    {
        _personalityMatchService = personalityMatchService;
    }

    [HttpGet("questions")]
    [AllowAnonymous]
    public async Task<ActionResult<PersonalityQuizDto>> GetQuestions()
    {
        var (success, errorMessage, statusCode, quiz) = await _personalityMatchService.GetQuestionsAsync();

        if (!success)
        {
            return NotFound(new { message = errorMessage });
        }

        return Ok(quiz);
    }

    [HttpPost("match")]
    public async Task<ActionResult<PersonalityMatchResultDto>> MatchPersonality([FromBody] PersonalityQuizSubmissionDto submission)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var (success, errorMessage, statusCode, result) = await _personalityMatchService.MatchPersonalityAsync(userId, submission);

        if (!success)
        {
            return statusCode switch
            {
                400 => BadRequest(new { message = errorMessage }),
                500 => StatusCode(500, new { message = "An error occurred while analyzing your personality.", details = errorMessage }),
                _ => BadRequest(new { message = errorMessage })
            };
        }

        return Ok(result);
    }

    [HttpGet("history")]
    public async Task<ActionResult<List<PersonalityMatchResultDto>>> GetMatchHistory()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var results = await _personalityMatchService.GetMatchHistoryAsync(userId);

        return Ok(results);
    }

    [HttpGet("my-answers")]
    public async Task<ActionResult> GetMyAnswers([FromQuery] int? limit = 1)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var groupedAnswers = await _personalityMatchService.GetUserAnswersAsync(userId, limit ?? 1);

        return Ok(groupedAnswers);
    }
}