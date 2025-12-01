using System.Security.Claims;
using System.Text.Json;
using Filmder.Data;
using Filmder.DTOs;
using Filmder.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Filmder.Controllers;
[EnableRateLimiting("ExpensiveDaily")]
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PersonalityMatchController : ControllerBase
{
    private readonly IAIService _aiService;
    private readonly AppDbContext _context;

    public PersonalityMatchController(IAIService aiService, AppDbContext context)
    {
        _aiService = aiService;
        _context = context;
    }

    [HttpGet("questions")]
    [AllowAnonymous]
    public async Task<ActionResult<PersonalityQuizDto>> GetQuestions()
    {
        var questions = await _context.PersonalityQuestions
            .Where(q => q.IsActive)
            .OrderBy(q => q.OrderIndex)
            .ToListAsync();

        if (!questions.Any())
        {
            return NotFound(new { message = "No personality questions found. Please contact administrator." });
        }

        var quiz = new PersonalityQuizDto
        {
            Questions = questions.Select(q => new PersonalityQuestionDto
            {
                Id = q.Id,
                Question = q.Question,
                Options = JsonSerializer.Deserialize<List<string>>(q.Options) ?? new List<string>(),
                OrderIndex = q.OrderIndex
            }).ToList()
        };

        return Ok(quiz);
    }
    
    [HttpPost("match")]
    public async Task<ActionResult<PersonalityMatchResultDto>> MatchPersonality([FromBody] PersonalityQuizSubmissionDto submission)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        if (submission.Answers == null || submission.Answers.Count < 5)
        {
            return BadRequest(new { message = "Please answer at least 5 questions to get accurate results." });
        }

        // Validate that all question IDs exist
        var questionIds = submission.Answers.Select(a => a.QuestionId).ToList();
        var validQuestions = await _context.PersonalityQuestions
            .Where(q => questionIds.Contains(q.Id) && q.IsActive)
            .CountAsync();

        if (validQuestions != questionIds.Count)
        {
            return BadRequest(new { message = "Some questions are invalid or no longer active." });
        }

        try
        {
            // Save answers to database
            var answers = submission.Answers.Select(a => new PersonalityAnswer
            {
                UserId = userId,
                QuestionId = a.QuestionId,
                Answer = a.Answer,
                AnsweredAt = DateTime.UtcNow
            }).ToList();

            _context.PersonalityAnswers.AddRange(answers);
            await _context.SaveChangesAsync();

            // Get AI matches
            var result = await _aiService.MatchPersonalityToCharacters(submission);
            
            if (result.Matches == null || !result.Matches.Any())
            {
                return StatusCode(500, new { message = "Unable to generate personality matches at this time. Please try again." });
            }

            // Save match results to database
            var matchResults = result.Matches.Select(m => new PersonalityMatchResult
            {
                UserId = userId,
                CharacterName = m.CharacterName,
                MovieOrSeries = m.MovieOrSeries,
                MatchPercentage = m.MatchPercentage,
                Explanation = m.Explanation,
                ImageUrl = m.ImageUrl,
                PersonalityProfile = result.PersonalityProfile,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            _context.PersonalityMatchResults.AddRange(matchResults);
            await _context.SaveChangesAsync();

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while analyzing your personality.", details = ex.Message });
        }
    }

    [HttpGet("history")]
    public async Task<ActionResult<List<PersonalityMatchResultDto>>> GetMatchHistory()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var results = await _context.PersonalityMatchResults
            .Where(pmr => pmr.UserId == userId)
            .OrderByDescending(pmr => pmr.CreatedAt)
            .ToListAsync();

        // Group by CreatedAt after retrieving from database
        var groupedResults = results
            .GroupBy(r => new DateTime(r.CreatedAt.Year, r.CreatedAt.Month, r.CreatedAt.Day, 
                                       r.CreatedAt.Hour, r.CreatedAt.Minute, r.CreatedAt.Second))
            .Select(g => new PersonalityMatchResultDto
            {
                PersonalityProfile = g.First().PersonalityProfile,
                Matches = g.Select(r => new CharacterMatchDto
                {
                    CharacterName = r.CharacterName,
                    MovieOrSeries = r.MovieOrSeries,
                    MatchPercentage = r.MatchPercentage,
                    Explanation = r.Explanation,
                    ImageUrl = r.ImageUrl ?? string.Empty
                }).OrderByDescending(m => m.MatchPercentage).ToList()
            })
            .ToList();

        return Ok(groupedResults);
    }

    [HttpGet("my-answers")]
    public async Task<ActionResult> GetMyAnswers([FromQuery] int? limit = 1)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        // First, get all answers with their questions
        var allAnswers = await _context.PersonalityAnswers
            .Where(pa => pa.UserId == userId)
            .Include(pa => pa.Question)
            .OrderByDescending(pa => pa.AnsweredAt)
            .ToListAsync();

        // Then group in memory
        var groupedAnswers = allAnswers
            .GroupBy(pa => new DateTime(pa.AnsweredAt.Year, pa.AnsweredAt.Month, pa.AnsweredAt.Day, 
                                        pa.AnsweredAt.Hour, pa.AnsweredAt.Minute, pa.AnsweredAt.Second))
            .OrderByDescending(g => g.Key)
            .Take(limit ?? 1)
            .Select(g => new
            {
                SubmittedAt = g.Key,
                Answers = g.Select(a => new
                {
                    QuestionId = a.QuestionId,
                    Question = a.Question.Question,
                    Answer = a.Answer
                }).ToList()
            })
            .ToList();

        return Ok(groupedAnswers);
    }
}