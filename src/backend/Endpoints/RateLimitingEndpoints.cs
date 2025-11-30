namespace RedisApp.Api.Endpoints;

public static class RateLimitingEndpoints
{
    public static RouteGroupBuilder MapRateLimitingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/ratelimit")
            .WithTags("Rate Limiting")
            .WithDescription("Demonstrates Redis-backed rate limiting (3 requests per 5-second window)");

        group.MapGet("/test", () =>
        {
            return Results.Ok(new
            {
                Message = "Request processed successfully",
                ProcessedAt = DateTime.UtcNow
            });
        })
        .WithName("TestRateLimit")
        .WithDescription("A rate-limited endpoint. Only 3 requests per 5-second window are allowed.")
        .RequireRateLimiting("redis-sliding-window");

        group.MapGet("/unlimited", () =>
        {
            return Results.Ok(new
            {
                Message = "This endpoint has no rate limiting",
                ProcessedAt = DateTime.UtcNow
            });
        })
        .WithName("UnlimitedEndpoint")
        .WithDescription("An endpoint without rate limiting for comparison.");

        return group;
    }
}
