using System.Security.Claims;
using Filmder.DTOs;
using Filmder.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Filmder.Controllers;

[EnableRateLimiting("ExpensiveDaily")]
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasteExplainerController : ControllerBase
{
    private readonly ITasteExplainerService _tasteExplainerService;

    public TasteExplainerController(ITasteExplainerService tasteExplainerService)
    {
        _tasteExplainerService = tasteExplainerService;
    }

    [HttpGet("explain")]
    public async Task<ActionResult<TasteExplanationDto>> ExplainMyTaste()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        try
        {
            var explanation = await _tasteExplainerService.GetTasteExplanationAsync(userId);

            if (explanation == null)
            {
                return NotFound(new { message = "No watched movies found. Start watching and rating movies to get your taste analysis!" });
            }

            return Ok(explanation);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to analyze your taste", details = ex.Message });
        }
    }

    [HttpGet("summary")]
    public async Task<ActionResult> GetTasteSummary()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var summary = await _tasteExplainerService.GetTasteSummaryAsync(userId);

        return Ok(summary);
    }
}