using Microsoft.AspNetCore.Mvc;
using RedisPoC.Models;
using RedisPoC.Services;

namespace RedisPoC.Controllers;

/// <summary>
/// Controller for distributed locking operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LockController : ControllerBase
{
    private readonly IRedisService _redisService;
    private readonly ILogger<LockController> _logger;

    public LockController(IRedisService redisService, ILogger<LockController> logger)
    {
        _redisService = redisService;
        _logger = logger;
    }

    /// <summary>
    /// Acquire a distributed lock
    /// </summary>
    [HttpPost("acquire")]
    public async Task<ActionResult<OperationResult>> AcquireLock([FromBody] LockRequest request)
    {
        try
        {
            var expiry = TimeSpan.FromSeconds(request.ExpirySeconds);
            var acquired = await _redisService.AcquireLockAsync(request.Resource, request.Token, expiry);
            
            return Ok(new OperationResult(
                acquired,
                acquired ? "Lock acquired successfully" : "Failed to acquire lock (already locked)",
                new { Resource = request.Resource, Token = request.Token, ExpirySeconds = request.ExpirySeconds }
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acquiring lock");
            return StatusCode(500, new OperationResult(false, ex.Message));
        }
    }

    /// <summary>
    /// Release a distributed lock
    /// </summary>
    [HttpPost("release")]
    public async Task<ActionResult<OperationResult>> ReleaseLock([FromBody] LockRequest request)
    {
        try
        {
            var released = await _redisService.ReleaseLockAsync(request.Resource, request.Token);
            
            return Ok(new OperationResult(
                released,
                released ? "Lock released successfully" : "Failed to release lock (not owner or already released)",
                new { Resource = request.Resource, Token = request.Token }
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing lock");
            return StatusCode(500, new OperationResult(false, ex.Message));
        }
    }

    /// <summary>
    /// Demonstrate a critical section protected by a distributed lock
    /// </summary>
    [HttpPost("critical-section/{resource}")]
    public async Task<ActionResult<OperationResult>> ExecuteCriticalSection(string resource)
    {
        var token = Guid.NewGuid().ToString();
        var lockAcquired = false;
        
        try
        {
            // Try to acquire lock
            lockAcquired = await _redisService.AcquireLockAsync(resource, token, TimeSpan.FromSeconds(30));
            
            if (!lockAcquired)
            {
                return Conflict(new OperationResult(false, "Could not acquire lock - resource is busy"));
            }
            
            // Simulate some work
            _logger.LogInformation("Executing critical section for resource: {Resource}", System.Text.RegularExpressions.Regex.Replace(resource, @"[\r\n]", ""));
            await Task.Delay(2000); // Simulate 2 seconds of work
            
            // Increment a counter to show the work was done
            var counter = await _redisService.IncrementAsync($"counter:{resource}");
            
            return Ok(new OperationResult(
                true,
                "Critical section executed successfully",
                new { Resource = resource, Counter = counter, Token = token }
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in critical section");
            return StatusCode(500, new OperationResult(false, ex.Message));
        }
        finally
        {
            // Always release the lock
            if (lockAcquired)
            {
                await _redisService.ReleaseLockAsync(resource, token);
            }
        }
    }
}
