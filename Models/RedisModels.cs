namespace RedisPoC.Models;

public record CacheRequest(string Key, string Value, int? ExpirySeconds = null);

public record CacheResponse(bool Success, string? Value = null, string? Message = null);

public record HashRequest(string Key, string Field, string Value);

public record SessionData(string SessionId, Dictionary<string, string> Data);

public record ListRequest(string Key, string Value, bool LeftSide = true);

public record SetRequest(string Key, string Value);

public record SortedSetRequest(string Key, string Member, double Score);

public record LeaderboardEntry(string Member, double Score, long? Rank = null);

public record PublishRequest(string Channel, string Message);

public record LockRequest(string Resource, string Token, int ExpirySeconds);

public record CounterRequest(string Key, long Value = 1);

public record OperationResult(bool Success, string Message, object? Data = null);
