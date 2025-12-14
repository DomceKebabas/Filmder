using Filmder.DTOs;

namespace Filmder.Interfaces;

public interface IMoodService
{
    Task<MoodMovieResponseDto> GetMovieByMoodAsync(MoodDto moodDto);
    List<object> GetAvailableMoods();
}