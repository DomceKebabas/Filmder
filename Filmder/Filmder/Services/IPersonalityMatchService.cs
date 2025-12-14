using Filmder.DTOs;

namespace Filmder.Services;

public interface IPersonalityMatchService
{
    Task<(bool Success, string? ErrorMessage, int? StatusCode, PersonalityQuizDto? Quiz)> GetQuestionsAsync();
    Task<(bool Success, string? ErrorMessage, int? StatusCode, PersonalityMatchResultDto? Result)> MatchPersonalityAsync(string userId, PersonalityQuizSubmissionDto submission);
    Task<List<PersonalityMatchResultDto>> GetMatchHistoryAsync(string userId);
    Task<List<AnswerGroupDto>> GetUserAnswersAsync(string userId, int limit);
}

public class AnswerGroupDto
{
    public DateTime SubmittedAt { get; set; }
    public List<AnswerDetailDto> Answers { get; set; } = new();
}

public class AnswerDetailDto
{
    public int QuestionId { get; set; }
    public string Question { get; set; } = null!;
    public string Answer { get; set; } = null!;
}