using Microsoft.AspNetCore.Mvc;
using RedisPoC.Models;
using RedisPoC.Services;

namespace RedisPoC.Controllers;

/// <summary>
/// Controller for queue and stack operations using Redis lists
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class QueueController : ControllerBase
{
    private readonly IRedisService _redisService;
    private readonly ILogger<QueueController> _logger;

    public QueueController(IRedisService redisService, ILogger<QueueController> logger)
    {
        _redisService = redisService;
        _logger = logger;
    }

    /// <summary>
    /// Push an item to the queue (FIFO - push right, pop left)
    /// </summary>
    [HttpPost("enqueue/{queueName}")]
    public async Task<ActionResult<OperationResult>> Enqueue(string queueName, [FromBody] ListRequest request)
    {
        try
        {
            var key = $"queue:{queueName}";
            var length = await _redisService.PushToListAsync(key, request.Value, leftPush: false);
            
            return Ok(new OperationResult(
                true,
                "Item enqueued successfully",
                new { QueueName = queueName, Length = length }
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enqueuing item");
            return StatusCode(500, new OperationResult(false, ex.Message));
        }
    }

    /// <summary>
    /// Pop an item from the queue (FIFO)
    /// </summary>
    [HttpPost("dequeue/{queueName}")]
    public async Task<ActionResult<OperationResult>> Dequeue(string queueName)
    {
        try
        {
            var key = $"queue:{queueName}";
            var value = await _redisService.PopFromListAsync(key, leftPop: true);
            
            return Ok(new OperationResult(
                value != null,
                value != null ? "Item dequeued successfully" : "Queue is empty",
                new { QueueName = queueName, Value = value }
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dequeuing item");
            return StatusCode(500, new OperationResult(false, ex.Message));
        }
    }

    /// <summary>
    /// Push an item to the stack (LIFO - push left, pop left)
    /// </summary>
    [HttpPost("push/{stackName}")]
    public async Task<ActionResult<OperationResult>> Push(string stackName, [FromBody] ListRequest request)
    {
        try
        {
            var key = $"stack:{stackName}";
            var length = await _redisService.PushToListAsync(key, request.Value, leftPush: true);
            
            return Ok(new OperationResult(
                true,
                "Item pushed successfully",
                new { StackName = stackName, Length = length }
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pushing item");
            return StatusCode(500, new OperationResult(false, ex.Message));
        }
    }

    /// <summary>
    /// Pop an item from the stack (LIFO)
    /// </summary>
    [HttpPost("pop/{stackName}")]
    public async Task<ActionResult<OperationResult>> Pop(string stackName)
    {
        try
        {
            var key = $"stack:{stackName}";
            var value = await _redisService.PopFromListAsync(key, leftPop: true);
            
            return Ok(new OperationResult(
                value != null,
                value != null ? "Item popped successfully" : "Stack is empty",
                new { StackName = stackName, Value = value }
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error popping item");
            return StatusCode(500, new OperationResult(false, ex.Message));
        }
    }

    /// <summary>
    /// Get all items from a queue/stack without removing them
    /// </summary>
    [HttpGet("{listName}")]
    public async Task<ActionResult<OperationResult>> GetList(string listName, [FromQuery] string type = "queue")
    {
        try
        {
            var key = $"{type}:{listName}";
            var items = await _redisService.GetListRangeAsync(key);
            var length = await _redisService.GetListLengthAsync(key);
            
            return Ok(new OperationResult(
                true,
                "List retrieved successfully",
                new { ListName = listName, Type = type, Length = length, Items = items }
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting list");
            return StatusCode(500, new OperationResult(false, ex.Message));
        }
    }
}
