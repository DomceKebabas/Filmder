using Filmder.DTOs;

namespace Filmder.Interfaces;

public interface IMoodRepository
{
    Task<MoodMovieResponseDto> GetMovieByMoodAsync(MoodDto moodDto);
    List<object> GetAvailableMoods();
}