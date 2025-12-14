using Filmder.Models;

namespace Filmder.Interfaces;

public interface IEmojiGameService
{
    Task<EmojiPuzzle?> GetRandomPuzzleAsync(Difficulty difficulty);
    Task<IEnumerable<EmojiPuzzle>> GetAllPuzzlesAsync(Difficulty? difficulty);
}