using Microsoft.AspNetCore.Mvc;
using RedisPoC.Models;
using RedisPoC.Services;

namespace RedisPoC.Controllers;

/// <summary>
/// Controller for pub/sub messaging operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MessagingController : ControllerBase
{
    private readonly IRedisService _redisService;
    private readonly ILogger<MessagingController> _logger;

    public MessagingController(IRedisService redisService, ILogger<MessagingController> logger)
    {
        _redisService = redisService;
        _logger = logger;
    }

    /// <summary>
    /// Publish a message to a channel
    /// </summary>
    [HttpPost("publish")]
    public async Task<ActionResult<OperationResult>> PublishMessage([FromBody] PublishRequest request)
    {
        try
        {
            await _redisService.PublishMessageAsync(request.Channel, request.Message);
            
            return Ok(new OperationResult(
                true,
                "Message published successfully",
                new { Channel = request.Channel, Message = request.Message }
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing message");
            return StatusCode(500, new OperationResult(false, ex.Message));
        }
    }

    /// <summary>
    /// Subscribe to a channel (Note: In a real application, use WebSockets or SignalR for real-time updates)
    /// This endpoint demonstrates the subscription capability but won't maintain a long-lived connection
    /// </summary>
    [HttpPost("subscribe/{channel}")]
    public async Task<ActionResult<OperationResult>> Subscribe(string channel)
    {
        try
        {
            // In a real application, you would use SignalR or WebSockets for this
            // This is just to demonstrate the API capability
            await _redisService.SubscribeAsync(channel, (message) =>
            {
                _logger.LogInformation("Received message on channel {Channel}: {Message}", channel, message);
            });
            
            return Ok(new OperationResult(
                true,
                $"Subscribed to channel '{channel}'. Messages will be logged.",
                new { Channel = channel }
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to channel");
            return StatusCode(500, new OperationResult(false, ex.Message));
        }
    }
}
