using Filmder.DTOs.HigherLower;
using Filmder.Interfaces;

namespace Filmder.Services;

public class HigherLowerService(IHigherLowerRepository repository)
    : IHigherLowerService
{
    public async Task<StartGameResponseDto> StartGameAsync(string userId)
        => await repository.StartGameAsync(userId);

    public async Task<GuessResultDto> SubmitGuessAsync(HigherLowerGuessDto dto, string userId)
        => await repository.SubmitGuessAsync(dto, userId);

    public async Task<HigherLowerStatsDto> GetMyStatsAsync(string userId)
        => await repository.GetMyStatsAsync(userId);

    public async Task<List<LeaderboardEntryDto>> GetLeaderboardAsync(int limit)
        => await repository.GetLeaderboardAsync(limit);

    public async Task<object> EndGameAsync(int gameId, string userId)
        => await repository.EndGameAsync(gameId, userId);
}