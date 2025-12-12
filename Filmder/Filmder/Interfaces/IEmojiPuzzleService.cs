using Filmder.DTOs;
using Filmder.Models;

namespace Filmder.Interfaces;

public interface IEmojiPuzzleService
{
    Task<EmojiPuzzleDto?> GetRandomPuzzleAsync(Difficulty difficulty);
    Task<List<EmojiPuzzleDto>> GetAllPuzzlesAsync(Difficulty? difficulty = null);
}