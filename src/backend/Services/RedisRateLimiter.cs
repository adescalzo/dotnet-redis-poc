using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using StackExchange.Redis;

namespace RedisApp.Api.Services;

public class RedisSlidingWindowRateLimiter : RateLimiter
{
    private readonly IConnectionMultiplexer _redis;
    private readonly string _keyPrefix;
    private readonly int _permitLimit;
    private readonly TimeSpan _window;

    public RedisSlidingWindowRateLimiter(
        IConnectionMultiplexer redis,
        string keyPrefix,
        int permitLimit,
        TimeSpan window)
    {
        _redis = redis;
        _keyPrefix = keyPrefix;
        _permitLimit = permitLimit;
        _window = window;
    }

    public override TimeSpan? IdleDuration => TimeSpan.Zero;

    public override RateLimiterStatistics? GetStatistics() => null;

    protected override RateLimitLease AttemptAcquireCore(int permitCount)
    {
        // For synchronous calls, we need to block - but prefer using WaitAsync
        return AcquireAsyncCore(permitCount, CancellationToken.None).GetAwaiter().GetResult();
    }

    protected override async ValueTask<RateLimitLease> AcquireAsyncCore(int permitCount, CancellationToken cancellationToken)
    {
        var db = _redis.GetDatabase();
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var windowStart = now - (long)_window.TotalMilliseconds;
        var key = $"{_keyPrefix}:sliding";

        // Lua script for atomic sliding window rate limiting
        const string luaScript = """
            local key = KEYS[1]
            local now = tonumber(ARGV[1])
            local window_start = tonumber(ARGV[2])
            local limit = tonumber(ARGV[3])
            local window_ms = tonumber(ARGV[4])

            -- Remove expired entries
            redis.call('ZREMRANGEBYSCORE', key, '-inf', window_start)

            -- Count current entries
            local count = redis.call('ZCARD', key)

            if count < limit then
                -- Add new entry with current timestamp as score
                redis.call('ZADD', key, now, now .. ':' .. math.random())
                redis.call('PEXPIRE', key, window_ms)
                return {1, limit - count - 1}
            else
                -- Get oldest entry to calculate retry time
                local oldest = redis.call('ZRANGE', key, 0, 0, 'WITHSCORES')
                if #oldest > 0 then
                    local retry_after = oldest[2] + window_ms - now
                    return {0, retry_after}
                end
                return {0, window_ms}
            end
            """;

        var result = (RedisResult[]?)await db.ScriptEvaluateAsync(
            luaScript,
            [key],
            [now, windowStart, _permitLimit, (long)_window.TotalMilliseconds]);

        if (result != null && (int)result[0] == 1)
        {
            var remaining = (int)result[1];
            return new RedisRateLimitLease(true, remaining);
        }

        var retryAfterMs = result != null ? (long)result[1] : (long)_window.TotalMilliseconds;
        return new RedisRateLimitLease(false, 0, TimeSpan.FromMilliseconds(retryAfterMs));
    }
}

public class RedisRateLimitLease : RateLimitLease
{
    private readonly int _remainingPermits;
    private readonly TimeSpan? _retryAfter;

    public RedisRateLimitLease(bool isAcquired, int remainingPermits, TimeSpan? retryAfter = null)
    {
        IsAcquired = isAcquired;
        _remainingPermits = remainingPermits;
        _retryAfter = retryAfter;
    }

    public override bool IsAcquired { get; }

    public override IEnumerable<string> MetadataNames
    {
        get
        {
            if (_retryAfter.HasValue)
                yield return MetadataName.RetryAfter.Name;
        }
    }

    public override bool TryGetMetadata(string metadataName, out object? metadata)
    {
        if (metadataName == MetadataName.RetryAfter.Name && _retryAfter.HasValue)
        {
            metadata = _retryAfter.Value;
            return true;
        }

        metadata = null;
        return false;
    }
}

public class RedisRateLimiterPolicy : IRateLimiterPolicy<string>
{
    private readonly IConnectionMultiplexer _redis;

    public RedisRateLimiterPolicy(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected => null;

    public RateLimitPartition<string> GetPartition(HttpContext httpContext)
    {
        var clientId = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.Get(clientId, key =>
            new RedisSlidingWindowRateLimiter(
                _redis,
                $"rate-limit:{key}",
                permitLimit: 3,
                window: TimeSpan.FromSeconds(5)));
    }
}
