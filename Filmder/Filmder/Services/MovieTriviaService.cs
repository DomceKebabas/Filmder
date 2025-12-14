using Filmder.DTOs;
using Filmder.Repositories;

namespace Filmder.Services;

public class MovieTriviaService : IMovieTriviaService
{
    private readonly IMovieTriviaRepository _repository;
    private readonly IAIService _aiService;
    private static readonly Dictionary<string, MovieTriviaDto> _triviaCache = new();

    public MovieTriviaService(IMovieTriviaRepository repository, IAIService aiService)
    {
        _repository = repository;
        _aiService = aiService;
    }

    public async Task<List<AvailableMovieDto>> GetAvailableMoviesAsync()
    {
        var movies = await _repository.GetTopRatedMoviesAsync(50);

        return movies.Select(m => new AvailableMovieDto
        {
            Id = m.Id,
            Name = m.Name,
            Genre = m.Genre.ToString(),
            ReleaseYear = m.ReleaseYear,
            Rating = m.Rating,
            PosterUrl = m.PosterUrl,
            Director = m.Director
        }).ToList();
    }

    public async Task<(bool Success, string? ErrorMessage, int? StatusCode, MovieTriviaDto? Trivia)> GenerateTriviaAsync(string userId, int movieId, int questionCount)
    {
        var movie = await _repository.GetByIdAsync(movieId);
        if (movie == null)
        {
            return (false, "Movie not found", 404, null);
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
                return (false, "Failed to generate trivia questions", 500, null);
            }

            var cacheKey = $"{userId}_{movieId}";
            _triviaCache[cacheKey] = trivia;

            return (true, null, null, trivia);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, 500, null);
        }
    }

    public (bool Success, string? ErrorMessage, int? StatusCode, TriviaResultDto? Result) SubmitAnswers(string userId, TriviaSubmissionDto submission)
    {
        if (submission.Answers == null || !submission.Answers.Any())
        {
            return (false, "No answers provided", 400, null);
        }

        var cacheKey = $"{userId}_{submission.MovieId}";
        if (!_triviaCache.TryGetValue(cacheKey, out var trivia))
        {
            return (false, "No questions found. Generate trivia first.", 400, null);
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

        return (true, null, null, result);
    }
}