using Filmder.DTOs;

namespace Filmder.Interfaces;

public interface IGroupStatsRepository
{
    Task<int> TotalGamesPlayedAsync(int groupId, string userId);
    Task<int> RatingGamesPlayedAsync(int groupId, string userId);
    Task<int> VotingGamesPlayedAsync(int groupId, string userId);
    Task<object> GetBestRatingGuesserAsync(int groupId, string userId);
    Task<double> GetAverageGuessDifferenceAsync(int groupId, string userId);
    Task<HighestRatedMovieDto> HighestVotedMovieAsync(int groupId, string userId);
    Task<PopularGenreDto> HighestVotedGenreAsync(int groupId, string userId);
    Task<double> GetAverageMovieScoreAsync(int groupId, string userId);
    Task<double> GetAverageMovieDurationAsync(int groupId, string userId);
}