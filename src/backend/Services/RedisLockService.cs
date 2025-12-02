using StackExchange.Redis;

namespace RedisApp.Api.Services;

public class RedisLockService(IConnectionMultiplexer redis, ILogger<RedisLockService> logger)
{
    public async Task<(bool acquired, string? lockValue)> TryAcquireLockAsync(
        string lockKey,
        TimeSpan expiry,
        CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        var lockValue = Guid.NewGuid().ToString();

        var acquired = await db.StringSetAsync(
            lockKey,
            lockValue,
            expiry,
            When.NotExists);

        if (acquired)
        {
            logger.LogInformation("Lock acquired: {LockKey} with value {LockValue}", lockKey, lockValue);
        }
        else
        {
            logger.LogWarning("Failed to acquire lock: {LockKey}", lockKey);
        }

        return (acquired, acquired ? lockValue : null);
    }

    public async Task ReleaseLockAsync(string lockKey, string lockValue)
    {
        var db = redis.GetDatabase();

        // Only release the lock if we own it (compare the value)
        const string luaScript = """
            if redis.call('get', KEYS[1]) == ARGV[1] then
                return redis.call('del', KEYS[1])
            else
                return 0
            end
            """;

        var result = await db.ScriptEvaluateAsync(luaScript, [lockKey], [lockValue]);

        if ((int)result == 1)
        {
            logger.LogInformation("Lock released: {LockKey}", lockKey);
        }
        else
        {
            logger.LogWarning("Failed to release lock (not owner or expired): {LockKey}", lockKey);
        }
    }

    public async Task<TimeSpan?> GetLockTtlAsync(string lockKey)
    {
        var db = redis.GetDatabase();
        var ttl = await db.KeyTimeToLiveAsync(lockKey);
        return ttl;
    }
}
