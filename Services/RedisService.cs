using StackExchange.Redis;
using System.Text.Json;

namespace RedisPoC.Services;

/// <summary>
/// Implementation of Redis service using StackExchange.Redis
/// </summary>
public class RedisService : IRedisService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly ILogger<RedisService> _logger;

    public RedisService(IConnectionMultiplexer redis, ILogger<RedisService> logger)
    {
        _redis = redis;
        _db = redis.GetDatabase();
        _logger = logger;
    }

    #region String Operations (Key-Value Cache)

    public async Task<bool> SetStringAsync(string key, string value, TimeSpan? expiry = null)
    {
        try
        {
            return await _db.StringSetAsync(key, value, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting string key: {Key}", key);
            throw;
        }
    }

    public async Task<string?> GetStringAsync(string key)
    {
        try
        {
            var value = await _db.StringGetAsync(key);
            return value.HasValue ? value.ToString() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting string key: {Key}", key);
            throw;
        }
    }

    public async Task<bool> DeleteKeyAsync(string key)
    {
        try
        {
            return await _db.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting key: {Key}", key);
            throw;
        }
    }

    public async Task<bool> KeyExistsAsync(string key)
    {
        try
        {
            return await _db.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking key existence: {Key}", key);
            throw;
        }
    }

    #endregion

    #region Hash Operations (Session Management)

    public async Task<bool> SetHashAsync(string key, string hashField, string value)
    {
        try
        {
            return await _db.HashSetAsync(key, hashField, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting hash field: {Key}.{Field}", key, hashField);
            throw;
        }
    }

    public async Task<string?> GetHashAsync(string key, string hashField)
    {
        try
        {
            var value = await _db.HashGetAsync(key, hashField);
            return value.HasValue ? value.ToString() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hash field: {Key}.{Field}", key, hashField);
            throw;
        }
    }

    public async Task<Dictionary<string, string>> GetAllHashAsync(string key)
    {
        try
        {
            var entries = await _db.HashGetAllAsync(key);
            return entries.ToDictionary(
                e => e.Name.ToString(),
                e => e.Value.ToString()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all hash fields: {Key}", key);
            throw;
        }
    }

    public async Task<bool> DeleteHashFieldAsync(string key, string hashField)
    {
        try
        {
            return await _db.HashDeleteAsync(key, hashField);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting hash field: {Key}.{Field}", key, hashField);
            throw;
        }
    }

    #endregion

    #region List Operations (Queue/Stack)

    public async Task<long> PushToListAsync(string key, string value, bool leftPush = true)
    {
        try
        {
            return leftPush 
                ? await _db.ListLeftPushAsync(key, value)
                : await _db.ListRightPushAsync(key, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pushing to list: {Key}", key);
            throw;
        }
    }

    public async Task<string?> PopFromListAsync(string key, bool leftPop = true)
    {
        try
        {
            var value = leftPop
                ? await _db.ListLeftPopAsync(key)
                : await _db.ListRightPopAsync(key);
            return value.HasValue ? value.ToString() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error popping from list: {Key}", key);
            throw;
        }
    }

    public async Task<List<string>> GetListRangeAsync(string key, long start = 0, long stop = -1)
    {
        try
        {
            var values = await _db.ListRangeAsync(key, start, stop);
            return values.Select(v => v.ToString()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting list range: {Key}", key);
            throw;
        }
    }

    public async Task<long> GetListLengthAsync(string key)
    {
        try
        {
            return await _db.ListLengthAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting list length: {Key}", key);
            throw;
        }
    }

    #endregion

    #region Set Operations (Unique collections)

    public async Task<bool> AddToSetAsync(string key, string value)
    {
        try
        {
            return await _db.SetAddAsync(key, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding to set: {Key}", key);
            throw;
        }
    }

    public async Task<bool> RemoveFromSetAsync(string key, string value)
    {
        try
        {
            return await _db.SetRemoveAsync(key, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing from set: {Key}", key);
            throw;
        }
    }

    public async Task<bool> IsMemberOfSetAsync(string key, string value)
    {
        try
        {
            return await _db.SetContainsAsync(key, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking set membership: {Key}", key);
            throw;
        }
    }

    public async Task<List<string>> GetSetMembersAsync(string key)
    {
        try
        {
            var values = await _db.SetMembersAsync(key);
            return values.Select(v => v.ToString()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting set members: {Key}", key);
            throw;
        }
    }

    #endregion

    #region Sorted Set Operations (Leaderboard/Rankings)

    public async Task<bool> AddToSortedSetAsync(string key, string member, double score)
    {
        try
        {
            return await _db.SortedSetAddAsync(key, member, score);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding to sorted set: {Key}", key);
            throw;
        }
    }

    public async Task<List<(string member, double score)>> GetSortedSetRangeAsync(
        string key, long start = 0, long stop = -1, bool ascending = true)
    {
        try
        {
            var order = ascending ? Order.Ascending : Order.Descending;
            var values = await _db.SortedSetRangeByRankWithScoresAsync(key, start, stop, order);
            return values.Select(v => (v.Element.ToString(), v.Score)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sorted set range: {Key}", key);
            throw;
        }
    }

    public async Task<long?> GetRankAsync(string key, string member, bool ascending = true)
    {
        try
        {
            var order = ascending ? Order.Ascending : Order.Descending;
            return await _db.SortedSetRankAsync(key, member, order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rank: {Key}.{Member}", key, member);
            throw;
        }
    }

    public async Task<double?> GetScoreAsync(string key, string member)
    {
        try
        {
            return await _db.SortedSetScoreAsync(key, member);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting score: {Key}.{Member}", key, member);
            throw;
        }
    }

    #endregion

    #region Pub/Sub Operations

    public async Task PublishMessageAsync(string channel, string message)
    {
        try
        {
            var subscriber = _redis.GetSubscriber();
            await subscriber.PublishAsync(RedisChannel.Literal(channel), message);
            _logger.LogInformation("Published message to channel {Channel}", channel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing message to channel: {Channel}", channel);
            throw;
        }
    }

    public async Task SubscribeAsync(string channel, Action<string> messageHandler)
    {
        try
        {
            var subscriber = _redis.GetSubscriber();
            await subscriber.SubscribeAsync(RedisChannel.Literal(channel), (ch, message) =>
            {
                messageHandler(message.ToString());
            });
            _logger.LogInformation("Subscribed to channel {Channel}", channel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to channel: {Channel}", channel);
            throw;
        }
    }

    #endregion

    #region Distributed Lock

    public async Task<bool> AcquireLockAsync(string resource, string token, TimeSpan expiry)
    {
        try
        {
            var lockKey = $"lock:{resource}";
            return await _db.StringSetAsync(lockKey, token, expiry, When.NotExists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acquiring lock: {Resource}", resource);
            throw;
        }
    }

    public async Task<bool> ReleaseLockAsync(string resource, string token)
    {
        try
        {
            var lockKey = $"lock:{resource}";
            var script = @"
                if redis.call('get', KEYS[1]) == ARGV[1] then
                    return redis.call('del', KEYS[1])
                else
                    return 0
                end";
            
            var result = await _db.ScriptEvaluateAsync(script, new RedisKey[] { lockKey }, new RedisValue[] { token });
            return (int)result == 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing lock: {Resource}", resource);
            throw;
        }
    }

    #endregion

    #region Utility Operations

    public async Task<long> IncrementAsync(string key, long value = 1)
    {
        try
        {
            return await _db.StringIncrementAsync(key, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing key: {Key}", key);
            throw;
        }
    }

    public async Task<long> DecrementAsync(string key, long value = 1)
    {
        try
        {
            return await _db.StringDecrementAsync(key, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrementing key: {Key}", key);
            throw;
        }
    }

    public async Task<TimeSpan?> GetTimeToLiveAsync(string key)
    {
        try
        {
            return await _db.KeyTimeToLiveAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting TTL for key: {Key}", key);
            throw;
        }
    }

    #endregion
}
