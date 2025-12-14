using System.Text.Json;
using Filmder.DTOs;
using Filmder.Models;
using Filmder.Repositories;

namespace Filmder.Services;

public class PersonalityMatchService : IPersonalityMatchService
{
    private readonly IPersonalityMatchRepository _repository;
    private readonly IAIService _aiService;

    public PersonalityMatchService(IPersonalityMatchRepository repository, IAIService aiService)
    {
        _repository = repository;
        _aiService = aiService;
    }

    public async Task<(bool Success, string? ErrorMessage, int? StatusCode, PersonalityQuizDto? Quiz)> GetQuestionsAsync()
    {
        var questions = await _repository.GetActiveQuestionsAsync();

        if (!questions.Any())
        {
            return (false, "No personality questions found. Please contact administrator.", 404, null);
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

        return (true, null, null, quiz);
    }

    public async Task<(bool Success, string? ErrorMessage, int? StatusCode, PersonalityMatchResultDto? Result)> MatchPersonalityAsync(string userId, PersonalityQuizSubmissionDto submission)
    {
        if (submission.Answers == null || submission.Answers.Count < 5)
        {
            return (false, "Please answer at least 5 questions to get accurate results.", 400, null);
        }

        var questionIds = submission.Answers.Select(a => a.QuestionId).ToList();
        var validQuestions = await _repository.CountValidQuestionsAsync(questionIds);

        if (validQuestions != questionIds.Count)
        {
            return (false, "Some questions are invalid or no longer active.", 400, null);
        }

        try
        {
            var answers = submission.Answers.Select(a => new PersonalityAnswer
            {
                UserId = userId,
                QuestionId = a.QuestionId,
                Answer = a.Answer,
                AnsweredAt = DateTime.UtcNow
            }).ToList();

            await _repository.AddAnswersAsync(answers);
            await _repository.SaveChangesAsync();

            var result = await _aiService.MatchPersonalityToCharacters(submission);

            if (result.Matches == null || !result.Matches.Any())
            {
                return (false, "Unable to generate personality matches at this time. Please try again.", 500, null);
            }

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

            await _repository.AddMatchResultsAsync(matchResults);
            await _repository.SaveChangesAsync();

            return (true, null, null, result);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, 500, null);
        }
    }

    public async Task<List<PersonalityMatchResultDto>> GetMatchHistoryAsync(string userId)
    {
        var results = await _repository.GetMatchResultsByUserAsync(userId);

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

        return groupedResults;
    }

    public async Task<List<AnswerGroupDto>> GetUserAnswersAsync(string userId, int limit)
    {
        var allAnswers = await _repository.GetAnswersByUserAsync(userId);

        var groupedAnswers = allAnswers
            .GroupBy(pa => new DateTime(pa.AnsweredAt.Year, pa.AnsweredAt.Month, pa.AnsweredAt.Day,
                                        pa.AnsweredAt.Hour, pa.AnsweredAt.Minute, pa.AnsweredAt.Second))
            .OrderByDescending(g => g.Key)
            .Take(limit)
            .Select(g => new AnswerGroupDto
            {
                SubmittedAt = g.Key,
                Answers = g.Select(a => new AnswerDetailDto
                {
                    QuestionId = a.QuestionId,
                    Question = a.Question.Question,
                    Answer = a.Answer
                }).ToList()
            })
            .ToList();

        return groupedAnswers;
    }
}