using Filmder.DTOs;
using Filmder.Interfaces;

namespace Filmder.Services;

public class GroupStatsService(IGroupStatsRepository repository) : IGroupStatsService
{
    public async Task<int> TotalGamesPlayedAsync(int groupId, string userId)
        => await repository.TotalGamesPlayedAsync(groupId, userId);

    public async Task<int> RatingGamesPlayedAsync(int groupId, string userId)
        => await repository.RatingGamesPlayedAsync(groupId, userId);

    public async Task<int> VotingGamesPlayedAsync(int groupId, string userId)
        => await repository.VotingGamesPlayedAsync(groupId, userId);

    public async Task<object> GetBestRatingGuesserAsync(int groupId, string userId)
        => await repository.GetBestRatingGuesserAsync(groupId, userId);

    public async Task<double> GetAverageGuessDifferenceAsync(int groupId, string userId)
        => await repository.GetAverageGuessDifferenceAsync(groupId, userId);

    public async Task<HighestRatedMovieDto> HighestVotedMovieAsync(int groupId, string userId)
        => await repository.HighestVotedMovieAsync(groupId, userId);

    public async Task<PopularGenreDto> HighestVotedGenreAsync(int groupId, string userId)
        => await repository.HighestVotedGenreAsync(groupId, userId);

    public async Task<double> GetAverageMovieScoreAsync(int groupId, string userId)
        => await repository.GetAverageMovieScoreAsync(groupId, userId);

    public async Task<double> GetAverageMovieDurationAsync(int groupId, string userId)
        => await repository.GetAverageMovieDurationAsync(groupId, userId);
}