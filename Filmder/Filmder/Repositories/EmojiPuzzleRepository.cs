using Filmder.Models;
using Filmder.Interfaces;

namespace Filmder.Services;

public class EmojiPuzzleRepository : IEmojiPuzzleRepository
{
    private readonly List<EmojiPuzzle> _puzzles = new();

    public async Task<EmojiPuzzle?> GetRandomPuzzleAsync(Difficulty difficulty)
    {
        var puzzles = _puzzles.Where(p => p.Difficulty == difficulty).ToList();

        if (!puzzles.Any())
            return null;

        var index = Random.Shared.Next(puzzles.Count);
        return await Task.FromResult(puzzles[index]);
    }

    public async Task<IEnumerable<EmojiPuzzle>> GetAllPuzzlesAsync(Difficulty? difficulty)
    {
        var result = difficulty == null
            ? _puzzles
            : _puzzles.Where(p => p.Difficulty == difficulty);

        return await Task.FromResult(result);
    }
}