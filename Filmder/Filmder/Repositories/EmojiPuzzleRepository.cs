using Filmder.Data;
using Filmder.Models;
using Filmder.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Filmder.Repositories;

public class EmojiPuzzleRepository(AppDbContext context) : IEmojiPuzzleRepository
{
    public async Task<EmojiPuzzle?> GetRandomPuzzleAsync(Difficulty difficulty)
    {
        var puzzles = await context.EmojiPuzzles
            .Where(p => p.Difficulty == difficulty && p.IsActive)
            .ToListAsync();

        if (puzzles.Count == 0)
            return null;

        var index = Random.Shared.Next(puzzles.Count);
        return puzzles[index];
    }

    public async Task<IEnumerable<EmojiPuzzle>> GetAllPuzzlesAsync(Difficulty? difficulty)
    {
        var query = context.EmojiPuzzles.Where(p => p.IsActive);

        if (difficulty.HasValue)
            query = query.Where(p => p.Difficulty == difficulty.Value);

        return await query.ToListAsync();
    }
}