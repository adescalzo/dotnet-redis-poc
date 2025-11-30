# .NET Redis PoC

This project is a Proof of Concept (PoC) demonstrating four different use cases for Redis in a modern web application.

## Tech Stack

- **Backend**: ASP.NET Core 10, .NET 10
- **Frontend**: React 19, TypeScript, Vite
- **Cache/Message Broker**: Redis
- **Real-time**: SignalR with Redis backplane

## Project Structure

```
src/
├── backend/          # ASP.NET Core Web API
│   ├── Endpoints/    # Minimal API endpoint groups
│   ├── Hubs/         # SignalR hubs
│   └── Services/     # Redis services (lock, rate limiter, subscriber)
└── frontend/         # React application
    ├── src/
    │   ├── api/      # API client and types
    │   └── components/ # UI components for each demo
    └── ...
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- [Redis](https://redis.io/) (or Docker)

## Getting Started

### 1. Start Redis

Using Docker:
```bash
docker run -d -p 6379:6379 --name redis redis:latest
```

Or use your local Redis installation.

### 2. Run the Backend

```bash
cd src/backend
dotnet run
```

The API will be available at `http://localhost:5000` with Scalar API documentation at `/scalar/v1`.

### 3. Run the Frontend

```bash
cd src/frontend
npm install
npm run dev
```

The UI will be available at `http://localhost:5173`.

## Redis Use Cases

This PoC covers the following four use cases for Redis:

### 1. Hybrid Caching

Demonstrates using Redis for distributed caching combined with in-memory caching (L1 + L2).

| Cache Layer | TTL | Description |
|-------------|-----|-------------|
| L1 (Memory) | 10 seconds | Fast, in-process cache |
| L2 (Redis)  | 30 seconds | Distributed cache |

**Endpoints:**
- `GET /api/caching/data` - Returns cached data with source info (L1, L2, or generated)
- `DELETE /api/caching/invalidate` - Invalidates the cache

The response includes `CacheInfo` showing which layer served the data.

### 2. Publish/Subscribe with SignalR

Showcases Redis Pub/Sub for real-time communication:

1. Frontend connects via SignalR to `/hubs/notifications`
2. `POST /api/pubsub/trigger` publishes two messages to Redis
3. A background service receives messages and pushes them to clients via SignalR

**Endpoints:**
- `POST /api/pubsub/trigger` - Publishes two notifications

### 3. Distributed Locking

Demonstrates Redis distributed locks for exclusive resource access using Lua scripts for atomic operations.

**Endpoints:**
- `POST /api/locking/execute` - Acquires a 5-second lock and executes work
- `GET /api/locking/status` - Returns current lock status

Concurrent requests will receive a 409 Conflict if the lock is held.

### 4. Rate Limiting

Implements Redis-backed sliding window rate limiting.

**Configuration:** 3 requests per 5-second window per client IP.

**Endpoints:**
- `GET /api/ratelimit/test` - Rate-limited endpoint
- `GET /api/ratelimit/unlimited` - Comparison endpoint without limits

Exceeded requests receive 429 Too Many Requests with `Retry-After` header.

## API Documentation

When running in development mode, access the Scalar API documentation at:
- `http://localhost:5000/scalar/v1`

## Configuration

Redis connection is configured in `src/backend/appsettings.json`:

```json
{
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}
```

## License

MIT
