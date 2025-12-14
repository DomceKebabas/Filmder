using Filmder.Data;
using Filmder.Models;
using Microsoft.EntityFrameworkCore;

namespace Filmder.Repositories;

public class PersonalityMatchRepository : IPersonalityMatchRepository
{
    private readonly AppDbContext _context;

    public PersonalityMatchRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<PersonalityQuestion>> GetActiveQuestionsAsync()
    {
        return await _context.PersonalityQuestions
            .Where(q => q.IsActive)
            .OrderBy(q => q.OrderIndex)
            .ToListAsync();
    }

    public async Task<int> CountValidQuestionsAsync(List<int> questionIds)
    {
        return await _context.PersonalityQuestions
            .Where(q => questionIds.Contains(q.Id) && q.IsActive)
            .CountAsync();
    }

    public async Task AddAnswersAsync(List<PersonalityAnswer> answers)
    {
        _context.PersonalityAnswers.AddRange(answers);
    }

    public async Task AddMatchResultsAsync(List<PersonalityMatchResult> matchResults)
    {
        _context.PersonalityMatchResults.AddRange(matchResults);
    }

    public async Task<List<PersonalityMatchResult>> GetMatchResultsByUserAsync(string userId)
    {
        return await _context.PersonalityMatchResults
            .Where(pmr => pmr.UserId == userId)
            .OrderByDescending(pmr => pmr.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<PersonalityAnswer>> GetAnswersByUserAsync(string userId)
    {
        return await _context.PersonalityAnswers
            .Where(pa => pa.UserId == userId)
            .Include(pa => pa.Question)
            .OrderByDescending(pa => pa.AnsweredAt)
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}