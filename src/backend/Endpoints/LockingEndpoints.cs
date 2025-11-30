using RedisApp.Api.Services;

namespace RedisApp.Api.Endpoints;

public static class LockingEndpoints
{
    public static RouteGroupBuilder MapLockingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/locking")
            .WithTags("Distributed Locking")
            .WithDescription("Demonstrates Redis distributed locking to ensure exclusive access to resources");

        group.MapPost("/execute", async (RedisLockService lockService, ILogger<Program> logger) =>
        {
            const string lockKey = "exclusive-resource-lock";
            var lockExpiry = TimeSpan.FromSeconds(5);

            var (acquired, lockValue) = await lockService.TryAcquireLockAsync(lockKey, lockExpiry);

            if (!acquired)
            {
                var ttl = await lockService.GetLockTtlAsync(lockKey);
                return Results.Conflict(new
                {
                    Message = "Resource is locked by another process",
                    LockKey = lockKey,
                    RemainingLockTime = ttl?.TotalSeconds
                });
            }

            try
            {
                logger.LogInformation("Starting exclusive operation...");

                // Simulate work that requires exclusive access
                await Task.Delay(TimeSpan.FromSeconds(5));

                logger.LogInformation("Exclusive operation completed");

                return Results.Ok(new
                {
                    Message = "Exclusive operation completed successfully",
                    LockKey = lockKey,
                    ExecutionTime = "5 seconds"
                });
            }
            finally
            {
                await lockService.ReleaseLockAsync(lockKey, lockValue!);
            }
        })
        .WithName("ExecuteWithLock")
        .WithDescription("Acquires a distributed lock for 5 seconds, executes an exclusive operation, then releases the lock.");

        group.MapGet("/status", async (RedisLockService lockService) =>
        {
            const string lockKey = "exclusive-resource-lock";
            var ttl = await lockService.GetLockTtlAsync(lockKey);

            return Results.Ok(new
            {
                LockKey = lockKey,
                IsLocked = ttl.HasValue,
                RemainingLockTime = ttl?.TotalSeconds
            });
        })
        .WithName("GetLockStatus")
        .WithDescription("Returns the current status of the distributed lock.");

        return group;
    }
}
