using Filmder.Models;
using Filmder.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Filmder.Controllers;

[EnableRateLimiting("DefaultBucket")]
[ApiController]
[Route("api/emoji-game")]
public class EmojiGameController : ControllerBase
{
    private readonly IEmojiPuzzleService _puzzleService;

    public EmojiGameController(IEmojiPuzzleService puzzleService)
    {
        _puzzleService = puzzleService;
    }

    [HttpGet("puzzle")]
    public async Task<IActionResult> GetPuzzle([FromQuery] Difficulty difficulty)
    {
        var puzzle = await _puzzleService.GetRandomPuzzleAsync(difficulty);
        
        if (puzzle == null)
            return NotFound(new { message = $"No puzzles found for difficulty: {difficulty}" });
        
        return Ok(puzzle);
    }

    [HttpGet("puzzles")]
    public async Task<IActionResult> GetAllPuzzles([FromQuery] Difficulty? difficulty = null)
    {
        var puzzles = await _puzzleService.GetAllPuzzlesAsync(difficulty);
        return Ok(puzzles);
    }
}