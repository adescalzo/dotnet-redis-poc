using System.Threading.RateLimiting;
using RedisApp.Api.Endpoints;
using RedisApp.Api.Hubs;
using RedisApp.Api.Services;
using Scalar.AspNetCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// OpenAPI
builder.Services.AddOpenApi();

// Validation for Minimal APIs (.NET 10)
builder.Services.AddValidation();

// ProblemDetails configuration
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
        context.ProblemDetails.Extensions.TryAdd("requestId", context.HttpContext.TraceIdentifier);
    };
});

// Redis connection
var redisConnectionString = builder.Configuration.GetValue<string>("Redis:ConnectionString") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));

// Hybrid caching with Redis as L2 cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = "RedisPoC:";
});

builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromMinutes(1)
    };
});

// SignalR with Redis backplane
builder.Services.AddSignalR()
    .AddStackExchangeRedis(redisConnectionString, options =>
    {
        options.Configuration.ChannelPrefix = RedisChannel.Literal("RedisPoC");
    });

// Background service for Redis Pub/Sub
builder.Services.AddHostedService<RedisSubscriberService>();

// Redis lock service
builder.Services.AddSingleton<RedisLockService>();

// Rate limiting with Redis
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/problem+json";

        var retryAfter = "5";
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue))
        {
            retryAfter = ((int)retryAfterValue.TotalSeconds).ToString();
            context.HttpContext.Response.Headers.RetryAfter = retryAfter;
        }

        var problemDetails = new
        {
            type = "https://httpstatuses.com/429",
            title = "Too Many Requests",
            status = 429,
            detail = $"Rate limit exceeded. Try again in {retryAfter} seconds.",
            instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}"
        };

        await context.HttpContext.Response.WriteAsJsonAsync(problemDetails, token);
    };

    options.AddPolicy<string, RedisRateLimiterPolicy>("redis-sliding-window");
});

// CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Global exception handler with ProblemDetails
app.UseExceptionHandler();
app.UseStatusCodePages();

// OpenAPI + Scalar UI (development only)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Redis PoC API");
        options.WithTheme(ScalarTheme.BluePlanet);
        options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.UseHttpsRedirection();
app.UseCors();
app.UseRateLimiter();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }))
   .WithName("HealthCheck")
   .WithTags("Health")
   .WithDescription("Returns the health status of the API");

// Map all endpoint groups
app.MapCachingEndpoints();
app.MapPubSubEndpoints();
app.MapLockingEndpoints();
app.MapRateLimitingEndpoints();

// SignalR hub
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();

// Make Program class accessible for integration tests
// public partial class Program { }
