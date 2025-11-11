using Microsoft.AspNetCore.Mvc;
using RedisPoC.Models;
using RedisPoC.Services;

namespace RedisPoC.Controllers;

/// <summary>
/// Controller for session management using Redis hash operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SessionController : ControllerBase
{
    private readonly IRedisService _redisService;
    private readonly ILogger<SessionController> _logger;

    public SessionController(IRedisService redisService, ILogger<SessionController> logger)
    {
        _redisService = redisService;
        _logger = logger;
    }

    /// <summary>
    /// Create or update a session field
    /// </summary>
    [HttpPost("{sessionId}")]
    public async Task<ActionResult<OperationResult>> SetSessionField(string sessionId, [FromBody] HashRequest request)
    {
        try
        {
            var key = $"session:{sessionId}";
            var result = await _redisService.SetHashAsync(key, request.Field, request.Value);
            
            return Ok(new OperationResult(
                result,
                "Session field set successfully",
                new { SessionId = sessionId, Field = request.Field }
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting session field");
            return StatusCode(500, new OperationResult(false, ex.Message));
        }
    }

    /// <summary>
    /// Get a specific session field
    /// </summary>
    [HttpGet("{sessionId}/{field}")]
    public async Task<ActionResult<OperationResult>> GetSessionField(string sessionId, string field)
    {
        try
        {
            var key = $"session:{sessionId}";
            var value = await _redisService.GetHashAsync(key, field);
            
            return Ok(new OperationResult(
                value != null,
                value != null ? "Field found" : "Field not found",
                new { SessionId = sessionId, Field = field, Value = value }
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session field");
            return StatusCode(500, new OperationResult(false, ex.Message));
        }
    }

    /// <summary>
    /// Get all session data
    /// </summary>
    [HttpGet("{sessionId}")]
    public async Task<ActionResult<OperationResult>> GetSession(string sessionId)
    {
        try
        {
            var key = $"session:{sessionId}";
            var data = await _redisService.GetAllHashAsync(key);
            
            return Ok(new OperationResult(
                data.Count > 0,
                data.Count > 0 ? "Session data retrieved" : "Session not found",
                new { SessionId = sessionId, Data = data }
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session");
            return StatusCode(500, new OperationResult(false, ex.Message));
        }
    }

    /// <summary>
    /// Delete a session field
    /// </summary>
    [HttpDelete("{sessionId}/{field}")]
    public async Task<ActionResult<OperationResult>> DeleteSessionField(string sessionId, string field)
    {
        try
        {
            var key = $"session:{sessionId}";
            var result = await _redisService.DeleteHashFieldAsync(key, field);
            
            return Ok(new OperationResult(
                result,
                result ? "Field deleted successfully" : "Field not found",
                new { SessionId = sessionId, Field = field }
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting session field");
            return StatusCode(500, new OperationResult(false, ex.Message));
        }
    }

    /// <summary>
    /// Delete entire session
    /// </summary>
    [HttpDelete("{sessionId}")]
    public async Task<ActionResult<OperationResult>> DeleteSession(string sessionId)
    {
        try
        {
            var key = $"session:{sessionId}";
            var result = await _redisService.DeleteKeyAsync(key);
            
            return Ok(new OperationResult(
                result,
                result ? "Session deleted successfully" : "Session not found",
                new { SessionId = sessionId }
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting session");
            return StatusCode(500, new OperationResult(false, ex.Message));
        }
    }
}
