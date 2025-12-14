using Filmder.Models;
using Filmder.Interfaces;

namespace Filmder.Services;

public class EmojiGameService(IEmojiPuzzleRepository puzzleRepository)
    : IEmojiGameService
{
    public async Task<EmojiPuzzle?> GetRandomPuzzleAsync(Difficulty difficulty)
    {
        return await puzzleRepository.GetRandomPuzzleAsync(difficulty);
    }

    public async Task<IEnumerable<EmojiPuzzle>> GetAllPuzzlesAsync(Difficulty? difficulty)
    {
        return await puzzleRepository.GetAllPuzzlesAsync(difficulty);
    }
}