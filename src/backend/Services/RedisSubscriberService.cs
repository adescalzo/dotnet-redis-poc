using Microsoft.AspNetCore.SignalR;
using RedisApp.Api.Hubs;
using StackExchange.Redis;

namespace RedisApp.Api.Services;

public class RedisSubscriberService : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<RedisSubscriberService> _logger;

    public RedisSubscriberService(
        IConnectionMultiplexer redis,
        IHubContext<NotificationHub> hubContext,
        ILogger<RedisSubscriberService> logger)
    {
        _redis = redis;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var subscriber = _redis.GetSubscriber();

        await subscriber.SubscribeAsync(
            RedisChannel.Literal("notifications"),
            async (channel, message) =>
            {
                _logger.LogInformation("Received message on channel {Channel}: {Message}", channel, message);

                await _hubContext.Clients.All.SendAsync(
                    "ReceiveNotification",
                    new
                    {
                        Channel = channel.ToString(),
                        Message = message.ToString(),
                        ReceivedAt = DateTime.UtcNow
                    },
                    stoppingToken);
            });

        _logger.LogInformation("Redis subscriber started, listening on 'notifications' channel");

        // Keep the service running
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
