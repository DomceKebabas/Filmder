using Filmder.Models;
using Filmder.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Filmder.Controllers;

[EnableRateLimiting("DefaultBucket")]
[ApiController]
[Route("api/emoji-game")]
public class EmojiGameController(IEmojiGameService emojiGameService) : ControllerBase
{
    [HttpGet("puzzle")]
    public async Task<IActionResult> GetPuzzle([FromQuery] Difficulty difficulty)
    {
        var puzzle = await emojiGameService.GetRandomPuzzleAsync(difficulty);

        if (puzzle == null)
            return NotFound(new { message = $"No puzzles found for difficulty: {difficulty}" });

        return Ok(puzzle);
    }

    [HttpGet("puzzles")]
    public async Task<IActionResult> GetAllPuzzles([FromQuery] Difficulty? difficulty = null)
    {
        var puzzles = await emojiGameService.GetAllPuzzlesAsync(difficulty);
        return Ok(puzzles);
    }
}