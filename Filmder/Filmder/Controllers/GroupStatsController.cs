using Filmder.DTOs;
using Filmder.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace Filmder.Controllers;

[EnableRateLimiting("DefaultBucket")]
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GroupStatsController(IGroupStatsService groupStatsService) : ControllerBase
{
    [HttpGet("playedGamesCount")]
    public async Task<ActionResult<int>> TotalGamesPlayed([FromQuery] int groupId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();

        var result = await groupStatsService.TotalGamesPlayedAsync(groupId, userId);
        return Ok(result);
    }

    [HttpGet("ratingGamesCount")]
    public async Task<ActionResult<int>> RatingGamesPlayed([FromQuery] int groupId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();

        var result = await groupStatsService.RatingGamesPlayedAsync(groupId, userId);
        return Ok(result);
    }

    [HttpGet("votingGamesCount")]
    public async Task<ActionResult<int>> VotingGamesPlayed([FromQuery] int groupId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();

        var result = await groupStatsService.VotingGamesPlayedAsync(groupId, userId);
        return Ok(result);
    }

    [HttpGet("bestRatingGuesser")]
    public async Task<ActionResult> GetBestRatingGuesser([FromQuery] int groupId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();

        var result = await groupStatsService.GetBestRatingGuesserAsync(groupId, userId);
        return Ok(result);
    }

    [HttpGet("averageGuessDifference")]
    public async Task<ActionResult<double>> GetAverageGuessDifference([FromQuery] int groupId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();

        var result = await groupStatsService.GetAverageGuessDifferenceAsync(groupId, userId);
        return Ok(result);
    }

    [HttpGet("highestVotedMovie")]
    public async Task<ActionResult<HighestRatedMovieDto>> HighestVotedMovie([FromQuery] int groupId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();

        var result = await groupStatsService.HighestVotedMovieAsync(groupId, userId);
        return Ok(result);
    }

    [HttpGet("highestVotedGenre")]
    public async Task<ActionResult<PopularGenreDto>> HighestVotedGenre([FromQuery] int groupId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();

        var result = await groupStatsService.HighestVotedGenreAsync(groupId, userId);
        return Ok(result);
    }

    [HttpGet("averageMovieScore")]
    public async Task<ActionResult<double>> GetAverageMovieScore([FromQuery] int groupId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();

        var result = await groupStatsService.GetAverageMovieScoreAsync(groupId, userId);
        return Ok(result);
    }

    [HttpGet("averageMovieDuration")]
    public async Task<ActionResult<double>> GetAverageMovieDuration([FromQuery] int groupId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();

        var result = await groupStatsService.GetAverageMovieDurationAsync(groupId, userId);
        return Ok(result);
    }
}
