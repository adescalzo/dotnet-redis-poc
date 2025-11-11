using Microsoft.AspNetCore.Mvc;
using RedisPoC.Models;
using RedisPoC.Services;

namespace RedisPoC.Controllers;

/// <summary>
/// Controller for simple key-value caching operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CacheController : ControllerBase
{
    private readonly IRedisService _redisService;
    private readonly ILogger<CacheController> _logger;

    public CacheController(IRedisService redisService, ILogger<CacheController> logger)
    {
        _redisService = redisService;
        _logger = logger;
    }

    /// <summary>
    /// Store a value in cache with optional expiry
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<OperationResult>> SetCache([FromBody] CacheRequest request)
    {
        try
        {
            var expiry = request.ExpirySeconds.HasValue 
                ? TimeSpan.FromSeconds(request.ExpirySeconds.Value) 
                : (TimeSpan?)null;
            
            var result = await _redisService.SetStringAsync(request.Key, request.Value, expiry);
            
            return Ok(new OperationResult(
                result,
                result ? "Value cached successfully" : "Failed to cache value",
                new { Key = request.Key, ExpirySeconds = request.ExpirySeconds }
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache");
            return StatusCode(500, new OperationResult(false, ex.Message));
        }
    }

    /// <summary>
    /// Retrieve a value from cache
    /// </summary>
    [HttpGet("{key}")]
    public async Task<ActionResult<OperationResult>> GetCache(string key)
    {
        try
        {
            var value = await _redisService.GetStringAsync(key);
            var exists = value != null;
            
            var ttl = exists ? await _redisService.GetTimeToLiveAsync(key) : null;
            
            return Ok(new OperationResult(
                exists,
                exists ? "Value found" : "Value not found",
                new { Key = key, Value = value, TimeToLive = ttl?.TotalSeconds }
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache");
            return StatusCode(500, new OperationResult(false, ex.Message));
        }
    }

    /// <summary>
    /// Delete a value from cache
    /// </summary>
    [HttpDelete("{key}")]
    public async Task<ActionResult<OperationResult>> DeleteCache(string key)
    {
        try
        {
            var result = await _redisService.DeleteKeyAsync(key);
            return Ok(new OperationResult(
                result,
                result ? "Key deleted successfully" : "Key not found",
                new { Key = key }
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting cache");
            return StatusCode(500, new OperationResult(false, ex.Message));
        }
    }

    /// <summary>
    /// Check if a key exists in cache
    /// </summary>
    [HttpHead("{key}")]
    public async Task<IActionResult> KeyExists(string key)
    {
        try
        {
            var exists = await _redisService.KeyExistsAsync(key);
            return exists ? Ok() : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking key existence");
            return StatusCode(500);
        }
    }
}
