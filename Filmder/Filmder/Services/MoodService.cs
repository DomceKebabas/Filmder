using Filmder.DTOs;
using Filmder.Interfaces;

namespace Filmder.Services;

public class MoodService(IMoodRepository repository) : IMoodService
{
    public async Task<MoodMovieResponseDto> GetMovieByMoodAsync(MoodDto moodDto)
        => await repository.GetMovieByMoodAsync(moodDto);

    public List<object> GetAvailableMoods()
        => repository.GetAvailableMoods();
}