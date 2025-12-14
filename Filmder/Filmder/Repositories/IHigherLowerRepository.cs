using Filmder.DTOs.HigherLower;

namespace Filmder.Interfaces;

public interface IHigherLowerRepository
{
    Task<StartGameResponseDto> StartGameAsync(string userId);
    Task<GuessResultDto> SubmitGuessAsync(HigherLowerGuessDto dto, string userId);
    Task<HigherLowerStatsDto> GetMyStatsAsync(string userId);
    Task<List<LeaderboardEntryDto>> GetLeaderboardAsync(int limit);
    Task<object> EndGameAsync(int gameId, string userId);
}