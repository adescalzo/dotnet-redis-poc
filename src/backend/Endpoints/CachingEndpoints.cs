using System.Diagnostics;
using Microsoft.Extensions.Caching.Hybrid;

namespace RedisApp.Api.Endpoints;

public static class CachingEndpoints
{
    public static RouteGroupBuilder MapCachingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/caching")
            .WithTags("Hybrid Caching")
            .WithDescription("Demonstrates Redis hybrid caching (distributed + in-memory)");

        group.MapGet("/data", async (HybridCache cache, CancellationToken ct) =>
        {
            var factoryCalled = false;
            var stopwatch = Stopwatch.StartNew();

            var data = await cache.GetOrCreateAsync(
                "cached-items",
                async token =>
                {
                    factoryCalled = true;
                    // Simulate fetching data from a slow source
                    await Task.Delay(100, token);
                    return GenerateSampleData();
                },
                new HybridCacheEntryOptions
                {
                    Expiration = TimeSpan.FromSeconds(30),
                    LocalCacheExpiration = TimeSpan.FromSeconds(10)
                },
                cancellationToken: ct);

            stopwatch.Stop();

            // Determine cache source based on behavior:
            // - Factory called: data was generated fresh (cache miss)
            // - Factory not called + fast (<5ms): likely L1 (in-memory)
            // - Factory not called + slower (>5ms): likely L2 (Redis)
            var source = factoryCalled
                ? "Source (generated)"
                : stopwatch.ElapsedMilliseconds < 5
                    ? "L1 (Memory)"
                    : "L2 (Redis)";

            return Results.Ok(new
            {
                data.GeneratedAt,
                data.Items,
                CacheInfo = new
                {
                    Source = source,
                    ElapsedMs = stopwatch.ElapsedMilliseconds,
                    FactoryCalled = factoryCalled
                }
            });
        })
        .WithName("GetCachedData")
        .WithDescription("Returns cached data with cache source info. L1=Memory (10s TTL), L2=Redis (30s TTL).");

        group.MapDelete("/invalidate", async (HybridCache cache, CancellationToken ct) =>
        {
            await cache.RemoveAsync("cached-items", ct);
            return Results.Ok(new { Message = "Cache invalidated successfully" });
        })
        .WithName("InvalidateCache")
        .WithDescription("Invalidates the cached data, forcing a fresh generation on next request.");

        return group;
    }

    private static CachedDataResponse GenerateSampleData()
    {
        var items = Enumerable.Range(1, 5).Select(i => new CachedItem
        {
            Id = i,
            Name = $"Item {i}",
            Value = Random.Shared.Next(1, 100)
        }).ToList();

        return new CachedDataResponse
        {
            GeneratedAt = DateTime.UtcNow,
            Items = items
        };
    }
}

public class CachedDataResponse
{
    public DateTime GeneratedAt { get; set; }
    public List<CachedItem> Items { get; set; } = [];
}

public class CachedItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}
