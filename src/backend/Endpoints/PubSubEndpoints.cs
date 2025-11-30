using StackExchange.Redis;

namespace RedisApp.Api.Endpoints;

public static class PubSubEndpoints
{
    public static RouteGroupBuilder MapPubSubEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/pubsub")
            .WithTags("Pub/Sub")
            .WithDescription("Demonstrates Redis Pub/Sub with SignalR for real-time notifications");

        group.MapPost("/trigger", async (IConnectionMultiplexer redis) =>
        {
            var subscriber = redis.GetSubscriber();
            var channel = RedisChannel.Literal("notifications");

            // Publish first message
            await subscriber.PublishAsync(channel, $"First notification at {DateTime.UtcNow:HH:mm:ss.fff}");

            // Small delay between messages
            await Task.Delay(500);

            // Publish second message
            await subscriber.PublishAsync(channel, $"Second notification at {DateTime.UtcNow:HH:mm:ss.fff}");

            return Results.Ok(new
            {
                Message = "Two notifications published to Redis channel",
                Channel = "notifications",
                PublishedAt = DateTime.UtcNow
            });
        })
        .WithName("TriggerNotifications")
        .WithDescription("Publishes two messages to the Redis 'notifications' channel. Connected SignalR clients will receive these in real-time.");

        return group;
    }
}
