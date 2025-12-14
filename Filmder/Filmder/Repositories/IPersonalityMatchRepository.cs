using Filmder.Models;

namespace Filmder.Repositories;

public interface IPersonalityMatchRepository
{
    Task<List<PersonalityQuestion>> GetActiveQuestionsAsync();
    Task<int> CountValidQuestionsAsync(List<int> questionIds);
    Task AddAnswersAsync(List<PersonalityAnswer> answers);
    Task AddMatchResultsAsync(List<PersonalityMatchResult> matchResults);
    Task<List<PersonalityMatchResult>> GetMatchResultsByUserAsync(string userId);
    Task<List<PersonalityAnswer>> GetAnswersByUserAsync(string userId);
    Task SaveChangesAsync();
}