using Microsoft.AspNetCore.Mvc;
using RedisPoC.Models;
using RedisPoC.Services;

namespace RedisPoC.Controllers;

/// <summary>
/// Controller for counter and utility operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CounterController : ControllerBase
{
    private readonly IRedisService _redisService;
    private readonly ILogger<CounterController> _logger;

    public CounterController(IRedisService redisService, ILogger<CounterController> logger)
    {
        _redisService = redisService;
        _logger = logger;
    }

    /// <summary>
    /// Increment a counter
    /// </summary>
    [HttpPost("increment")]
    public async Task<ActionResult<OperationResult>> Increment([FromBody] CounterRequest request)
    {
        try
        {
            var value = await _redisService.IncrementAsync(request.Key, request.Value);
            
            return Ok(new OperationResult(
                true,
                "Counter incremented successfully",
                new { Key = request.Key, Value = value, IncrementedBy = request.Value }
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing counter");
            return StatusCode(500, new OperationResult(false, ex.Message));
        }
    }

    /// <summary>
    /// Decrement a counter
    /// </summary>
    [HttpPost("decrement")]
    public async Task<ActionResult<OperationResult>> Decrement([FromBody] CounterRequest request)
    {
        try
        {
            var value = await _redisService.DecrementAsync(request.Key, request.Value);
            
            return Ok(new OperationResult(
                true,
                "Counter decremented successfully",
                new { Key = request.Key, Value = value, DecrementedBy = request.Value }
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrementing counter");
            return StatusCode(500, new OperationResult(false, ex.Message));
        }
    }

    /// <summary>
    /// Get current counter value
    /// </summary>
    [HttpGet("{key}")]
    public async Task<ActionResult<OperationResult>> GetCounter(string key)
    {
        try
        {
            var value = await _redisService.GetStringAsync(key);
            
            if (value == null)
            {
                return NotFound(new OperationResult(false, "Counter not found", new { Key = key, Value = 0 }));
            }
            
            if (long.TryParse(value, out var numericValue))
            {
                return Ok(new OperationResult(true, "Counter retrieved", new { Key = key, Value = numericValue }));
            }
            
            return BadRequest(new OperationResult(false, "Key exists but is not a valid counter"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting counter");
            return StatusCode(500, new OperationResult(false, ex.Message));
        }
    }

    /// <summary>
    /// Reset a counter
    /// </summary>
    [HttpDelete("{key}")]
    public async Task<ActionResult<OperationResult>> ResetCounter(string key)
    {
        try
        {
            var result = await _redisService.DeleteKeyAsync(key);
            
            return Ok(new OperationResult(
                result,
                result ? "Counter reset successfully" : "Counter not found",
                new { Key = key }
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting counter");
            return StatusCode(500, new OperationResult(false, ex.Message));
        }
    }
}
