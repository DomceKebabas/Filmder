namespace Filmder.DTOs;

public class TasteExplanationDto
{
    public string OverallTasteProfile { get; set; } = string.Empty;
    public List<TasteInsightDto> Insights { get; set; } = new();
    public List<string> FavoriteThemes { get; set; } = new();
    public List<string> PreferredDirectors { get; set; } = new();
    public string WatchingPersonality { get; set; } = string.Empty;
}

public class TasteInsightDto
{
    public string Category { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public List<string> ExampleMovies { get; set; } = new();
}