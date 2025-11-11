using Microsoft.AspNetCore.Mvc;
using RedisPoC.Models;
using RedisPoC.Services;

namespace RedisPoC.Controllers;

/// <summary>
/// Controller for leaderboard operations using Redis sorted sets
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LeaderboardController : ControllerBase
{
    private readonly IRedisService _redisService;
    private readonly ILogger<LeaderboardController> _logger;

    public LeaderboardController(IRedisService redisService, ILogger<LeaderboardController> logger)
    {
        _redisService = redisService;
        _logger = logger;
    }

    /// <summary>
    /// Add or update a player's score
    /// </summary>
    [HttpPost("{leaderboardName}")]
    public async Task<ActionResult<OperationResult>> AddScore(string leaderboardName, [FromBody] SortedSetRequest request)
    {
        try
        {
            var key = $"leaderboard:{leaderboardName}";
            var result = await _redisService.AddToSortedSetAsync(key, request.Member, request.Score);
            var rank = await _redisService.GetRankAsync(key, request.Member, ascending: false);
            
            return Ok(new OperationResult(
                result,
                "Score added successfully",
                new { Leaderboard = leaderboardName, Member = request.Member, Score = request.Score, Rank = rank.HasValue ? (long?)(rank.Value + 1) : null }
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding score");
            return StatusCode(500, new OperationResult(false, ex.Message));
        }
    }

    /// <summary>
    /// Get top N players (highest scores)
    /// </summary>
    [HttpGet("{leaderboardName}/top/{count}")]
    public async Task<ActionResult<OperationResult>> GetTopPlayers(string leaderboardName, int count = 10)
    {
        try
        {
            var key = $"leaderboard:{leaderboardName}";
            var players = await _redisService.GetSortedSetRangeAsync(key, 0, count - 1, ascending: false);
            
            var leaderboard = players.Select((p, index) => new LeaderboardEntry(
                p.member,
                p.score,
                index + 1
            )).ToList();
            
            return Ok(new OperationResult(
                true,
                "Top players retrieved",
                new { Leaderboard = leaderboardName, Count = leaderboard.Count, Players = leaderboard }
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top players");
            return StatusCode(500, new OperationResult(false, ex.Message));
        }
    }

    /// <summary>
    /// Get a player's rank and score
    /// </summary>
    [HttpGet("{leaderboardName}/player/{playerName}")]
    public async Task<ActionResult<OperationResult>> GetPlayerRank(string leaderboardName, string playerName)
    {
        try
        {
            var key = $"leaderboard:{leaderboardName}";
            var score = await _redisService.GetScoreAsync(key, playerName);
            
            if (!score.HasValue)
            {
                return NotFound(new OperationResult(false, "Player not found in leaderboard"));
            }
            
            var rank = await _redisService.GetRankAsync(key, playerName, ascending: false);
            
            return Ok(new OperationResult(
                true,
                "Player rank retrieved",
                new LeaderboardEntry(playerName, score.Value, rank.HasValue ? rank.Value + 1 : null)
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting player rank");
            return StatusCode(500, new OperationResult(false, ex.Message));
        }
    }

    /// <summary>
    /// Get players around a specific player (context leaderboard)
    /// </summary>
    [HttpGet("{leaderboardName}/around/{playerName}")]
    public async Task<ActionResult<OperationResult>> GetPlayersAround(string leaderboardName, string playerName, [FromQuery] int range = 5)
    {
        try
        {
            var key = $"leaderboard:{leaderboardName}";
            var rank = await _redisService.GetRankAsync(key, playerName, ascending: false);
            
            if (!rank.HasValue)
            {
                return NotFound(new OperationResult(false, "Player not found in leaderboard"));
            }
            
            var start = Math.Max(0, rank.Value - range);
            var stop = rank.Value + range;
            
            var players = await _redisService.GetSortedSetRangeAsync(key, start, stop, ascending: false);
            
            var leaderboard = players.Select((p, index) => new LeaderboardEntry(
                p.member,
                p.score,
                start + index + 1
            )).ToList();
            
            return Ok(new OperationResult(
                true,
                "Players around retrieved",
                new { Leaderboard = leaderboardName, PlayerName = playerName, Players = leaderboard }
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting players around");
            return StatusCode(500, new OperationResult(false, ex.Message));
        }
    }
}
