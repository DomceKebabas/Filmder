using Filmder.Models;

namespace Filmder.Interfaces;

public interface IEmojiPuzzleRepository
{
    Task<EmojiPuzzle?> GetRandomPuzzleAsync(Difficulty difficulty);
    Task<IEnumerable<EmojiPuzzle>> GetAllPuzzlesAsync(Difficulty? difficulty);
}