using Filmder.Data;
using Filmder.DTOs;
using Filmder.Interfaces;
using Filmder.Models;
using Microsoft.EntityFrameworkCore;

namespace Filmder.Services;

public class EmojiPuzzleService : IEmojiPuzzleService
{
    private readonly AppDbContext _context;
    private readonly Random _random = new();

    public EmojiPuzzleService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<EmojiPuzzleDto?> GetRandomPuzzleAsync(Difficulty difficulty)
    {
        var puzzles = await _context.EmojiPuzzles
            .Where(p => p.Difficulty == difficulty && p.IsActive)
            .ToListAsync();

        if (!puzzles.Any())
            return null;

        var puzzle = puzzles[_random.Next(puzzles.Count)];

        return MapToDto(puzzle);
    }

    public async Task<List<EmojiPuzzleDto>> GetAllPuzzlesAsync(Difficulty? difficulty = null)
    {
        var query = _context.EmojiPuzzles.Where(p => p.IsActive);

        if (difficulty.HasValue)
            query = query.Where(p => p.Difficulty == difficulty.Value);

        var puzzles = await query.ToListAsync();

        return puzzles.Select(MapToDto).ToList();
    }

    private EmojiPuzzleDto MapToDto(EmojiPuzzle puzzle)
    {
        var options = new List<string>
        {
            puzzle.Option1,
            puzzle.Option2,
            puzzle.Option3,
            puzzle.Option4
        };
        
        for (int i = options.Count - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (options[i], options[j]) = (options[j], options[i]);
        }

        return new EmojiPuzzleDto
        {
            Movie = puzzle.Movie,
            Emoji = puzzle.Emoji,
            Options = options
        };
    }
}