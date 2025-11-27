using Filmder.DTOs;
using Filmder.Models;

public interface IAIService
{
    Task<string> GenerateText(string prompt);

    Task<string> EmojiSequence(Difficulty difficulty);
    Task<PersonalityMatchResultDto> MatchPersonalityToCharacters(PersonalityQuizSubmissionDto submission);
    Task<TasteExplanationDto> ExplainUserTaste(List<UserMovieTasteDto> watchedMovies);
    Task<PersonalizedPlaylistDto> GeneratePersonalizedPlaylist(List<UserMovieTasteDto> recentActivity, int count = 10);
}