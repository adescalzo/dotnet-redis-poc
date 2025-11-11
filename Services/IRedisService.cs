namespace RedisPoC.Services;

/// <summary>
/// Redis service interface providing various Redis operations
/// </summary>
public interface IRedisService
{
    // String operations (Key-Value Cache)
    Task<bool> SetStringAsync(string key, string value, TimeSpan? expiry = null);
    Task<string?> GetStringAsync(string key);
    Task<bool> DeleteKeyAsync(string key);
    Task<bool> KeyExistsAsync(string key);
    
    // Hash operations (Session Management)
    Task<bool> SetHashAsync(string key, string hashField, string value);
    Task<string?> GetHashAsync(string key, string hashField);
    Task<Dictionary<string, string>> GetAllHashAsync(string key);
    Task<bool> DeleteHashFieldAsync(string key, string hashField);
    
    // List operations (Queue/Stack)
    Task<long> PushToListAsync(string key, string value, bool leftPush = true);
    Task<string?> PopFromListAsync(string key, bool leftPop = true);
    Task<List<string>> GetListRangeAsync(string key, long start = 0, long stop = -1);
    Task<long> GetListLengthAsync(string key);
    
    // Set operations (Unique collections)
    Task<bool> AddToSetAsync(string key, string value);
    Task<bool> RemoveFromSetAsync(string key, string value);
    Task<bool> IsMemberOfSetAsync(string key, string value);
    Task<List<string>> GetSetMembersAsync(string key);
    
    // Sorted Set operations (Leaderboard/Rankings)
    Task<bool> AddToSortedSetAsync(string key, string member, double score);
    Task<List<(string member, double score)>> GetSortedSetRangeAsync(string key, long start = 0, long stop = -1, bool ascending = true);
    Task<long?> GetRankAsync(string key, string member, bool ascending = true);
    Task<double?> GetScoreAsync(string key, string member);
    
    // Pub/Sub operations
    Task PublishMessageAsync(string channel, string message);
    Task SubscribeAsync(string channel, Action<string> messageHandler);
    
    // Distributed Lock
    Task<bool> AcquireLockAsync(string resource, string token, TimeSpan expiry);
    Task<bool> ReleaseLockAsync(string resource, string token);
    
    // Utility operations
    Task<long> IncrementAsync(string key, long value = 1);
    Task<long> DecrementAsync(string key, long value = 1);
    Task<TimeSpan?> GetTimeToLiveAsync(string key);
}
