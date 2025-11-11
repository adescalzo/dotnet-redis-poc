# dotnet-redis-poc

.NET Core Web API application demonstrating various Redis usage scenarios using StackExchange.Redis.

## Features

This POC demonstrates the following Redis scenarios:

1. **Simple Key-Value Caching** - Basic string operations with TTL support
2. **Session Management** - Hash operations for storing session data
3. **Queue/Stack Operations** - List operations for FIFO queues and LIFO stacks
4. **Leaderboard** - Sorted set operations for ranking and scoring
5. **Pub/Sub Messaging** - Publish/Subscribe pattern for real-time messaging
6. **Distributed Locking** - Distributed locks for critical sections
7. **Counters** - Atomic increment/decrement operations

## Prerequisites

- .NET 9.0 SDK
- Docker and Docker Compose (for running Redis)
- Redis 7.x (or use the provided docker-compose.yml)

## Getting Started

### 1. Start Redis

Using Docker Compose:

```bash
docker-compose up -d
```

Or install Redis locally and ensure it's running on `localhost:6379`.

### 2. Configure Redis Connection

Update `appsettings.json` if your Redis instance is not on `localhost:6379`:

```json
{
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}
```

### 3. Run the Application

```bash
dotnet run
```

The API will be available at `https://localhost:5001` (or the port specified in your launch settings).

## API Endpoints

### Cache Controller (`/api/cache`)

Simple key-value caching operations:

- `POST /api/cache` - Store a value with optional expiry
- `GET /api/cache/{key}` - Retrieve a cached value
- `DELETE /api/cache/{key}` - Delete a cached value
- `HEAD /api/cache/{key}` - Check if a key exists

Example:
```bash
# Set a cache value
curl -X POST https://localhost:5001/api/cache \
  -H "Content-Type: application/json" \
  -d '{"key": "user:123", "value": "John Doe", "expirySeconds": 300}'

# Get a cache value
curl https://localhost:5001/api/cache/user:123
```

### Session Controller (`/api/session`)

Session management using Redis hashes:

- `POST /api/session/{sessionId}` - Set a session field
- `GET /api/session/{sessionId}` - Get all session data
- `GET /api/session/{sessionId}/{field}` - Get a specific field
- `DELETE /api/session/{sessionId}/{field}` - Delete a session field
- `DELETE /api/session/{sessionId}` - Delete entire session

Example:
```bash
# Set session data
curl -X POST https://localhost:5001/api/session/abc123 \
  -H "Content-Type: application/json" \
  -d '{"key": "session:abc123", "field": "username", "value": "johndoe"}'

# Get session
curl https://localhost:5001/api/session/abc123
```

### Queue Controller (`/api/queue`)

Queue (FIFO) and Stack (LIFO) operations:

- `POST /api/queue/enqueue/{queueName}` - Add item to queue
- `POST /api/queue/dequeue/{queueName}` - Remove item from queue
- `POST /api/queue/push/{stackName}` - Push item to stack
- `POST /api/queue/pop/{stackName}` - Pop item from stack
- `GET /api/queue/{listName}?type=queue|stack` - View all items

Example:
```bash
# Enqueue items
curl -X POST https://localhost:5001/api/queue/enqueue/tasks \
  -H "Content-Type: application/json" \
  -d '{"key": "queue:tasks", "value": "Process order #123"}'

# Dequeue item
curl -X POST https://localhost:5001/api/queue/dequeue/tasks
```

### Leaderboard Controller (`/api/leaderboard`)

Leaderboard operations using sorted sets:

- `POST /api/leaderboard/{leaderboardName}` - Add/update player score
- `GET /api/leaderboard/{leaderboardName}/top/{count}` - Get top N players
- `GET /api/leaderboard/{leaderboardName}/player/{playerName}` - Get player rank
- `GET /api/leaderboard/{leaderboardName}/around/{playerName}?range=5` - Get players around a player

Example:
```bash
# Add scores
curl -X POST https://localhost:5001/api/leaderboard/global \
  -H "Content-Type: application/json" \
  -d '{"key": "leaderboard:global", "member": "player1", "score": 1500}'

# Get top 10
curl https://localhost:5001/api/leaderboard/global/top/10
```

### Messaging Controller (`/api/messaging`)

Pub/Sub messaging:

- `POST /api/messaging/publish` - Publish a message to a channel
- `POST /api/messaging/subscribe/{channel}` - Subscribe to a channel

Example:
```bash
# Publish message
curl -X POST https://localhost:5001/api/messaging/publish \
  -H "Content-Type: application/json" \
  -d '{"channel": "notifications", "message": "New order received"}'
```

### Lock Controller (`/api/lock`)

Distributed locking:

- `POST /api/lock/acquire` - Acquire a distributed lock
- `POST /api/lock/release` - Release a distributed lock
- `POST /api/lock/critical-section/{resource}` - Execute a critical section with automatic locking

Example:
```bash
# Acquire lock
curl -X POST https://localhost:5001/api/lock/acquire \
  -H "Content-Type: application/json" \
  -d '{"resource": "order-processing", "token": "unique-token-123", "expirySeconds": 30}'

# Release lock
curl -X POST https://localhost:5001/api/lock/release \
  -H "Content-Type: application/json" \
  -d '{"resource": "order-processing", "token": "unique-token-123", "expirySeconds": 30}'
```

### Counter Controller (`/api/counter`)

Atomic counter operations:

- `POST /api/counter/increment` - Increment a counter
- `POST /api/counter/decrement` - Decrement a counter
- `GET /api/counter/{key}` - Get counter value
- `DELETE /api/counter/{key}` - Reset counter

Example:
```bash
# Increment counter
curl -X POST https://localhost:5001/api/counter/increment \
  -H "Content-Type: application/json" \
  -d '{"key": "page-views", "value": 1}'

# Get counter
curl https://localhost:5001/api/counter/page-views
```

## Project Structure

```
RedisPoC/
├── Controllers/           # API Controllers for each Redis scenario
│   ├── CacheController.cs
│   ├── SessionController.cs
│   ├── QueueController.cs
│   ├── LeaderboardController.cs
│   ├── MessagingController.cs
│   ├── LockController.cs
│   └── CounterController.cs
├── Services/             # Redis service implementation
│   ├── IRedisService.cs
│   └── RedisService.cs
├── Models/               # Request/Response models
│   └── RedisModels.cs
├── Program.cs            # Application entry point
├── appsettings.json      # Configuration
└── docker-compose.yml    # Redis container setup
```

## Redis Data Structures Used

- **String**: Key-value cache, counters
- **Hash**: Session storage
- **List**: Queues and stacks
- **Set**: Unique collections
- **Sorted Set**: Leaderboards and rankings
- **Pub/Sub**: Real-time messaging

## Testing with OpenAPI/Swagger

When running in development mode, you can access the OpenAPI UI at:
- `https://localhost:5001/openapi/v1.json`

## Clean Up

To stop and remove the Redis container:

```bash
docker-compose down -v
```

## License

This is a proof-of-concept project for educational purposes.
