using Filmder.DTOs;

namespace Filmder.Services;

public interface IMovieTriviaService
{
    Task<List<AvailableMovieDto>> GetAvailableMoviesAsync();
    Task<(bool Success, string? ErrorMessage, int? StatusCode, MovieTriviaDto? Trivia)> GenerateTriviaAsync(string userId, int movieId, int questionCount);
    (bool Success, string? ErrorMessage, int? StatusCode, TriviaResultDto? Result) SubmitAnswers(string userId, TriviaSubmissionDto submission);
}

public class AvailableMovieDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Genre { get; set; } = null!;
    public int ReleaseYear { get; set; }
    public double Rating { get; set; }
    public string? PosterUrl { get; set; }
    public string? Director { get; set; }
}