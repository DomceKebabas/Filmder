using Filmder.Models;
using Filmder.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Filmder.Controllers;
[EnableRateLimiting("ExpensiveDaily")]
[ApiController]
[Route("api/emoji-game")]
public class EmojiGameController : ControllerBase
{
    private readonly IAIService _ai;

    public EmojiGameController(IAIService ai)
    {
        _ai = ai;
    }

    [HttpGet("puzzle")]
    public async Task<IActionResult> GetPuzzle([FromQuery] Difficulty difficulty)
    {
        var puzzle = await _ai.EmojiSequence(difficulty);
        return Ok(puzzle);
    }
}