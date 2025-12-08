namespace Filmder.DTOs;

public class MovieTriviaDto
{
    public int MovieId { get; set; }
    public string MovieName { get; set; } = string.Empty;
    public List<TriviaQuestionDto> Questions { get; set; } = new();
}

public class TriviaQuestionDto
{
    public string Question { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new(); // 4 options
    public int CorrectAnswerIndex { get; set; } // 0-3
}

public class TriviaAnswerDto
{
    public int QuestionIndex { get; set; }
    public int SelectedAnswerIndex { get; set; }
}

public class TriviaResultDto
{
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public double Score { get; set; }
    public List<QuestionResultDto> QuestionResults { get; set; } = new();
}

public class QuestionResultDto
{
    public string Question { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public string UserAnswer { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = string.Empty;
}

public class TriviaSubmissionDto
{
    public int MovieId { get; set; }
    public List<TriviaAnswerDto> Answers { get; set; } = new();
}