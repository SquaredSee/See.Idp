# 06 — Data Protection & Redis Key Persistence

## Context

ASP.NET Core Data Protection is used to encrypt authentication cookies, antiforgery tokens,
and other sensitive values. By default, keys are stored in memory and lost on restart,
invalidating all active sessions. Redis is already defined in `docker-compose.yml` but is
not wired into the application at all.

## User Story

As a user, I want my login session to persist across server restarts and to work correctly
when multiple instances of the IDP are running, so that I am not randomly logged out.

## Acceptance Criteria

- Data protection keys are persisted to Redis and survive application restarts
- Authentication cookies remain valid after a restart
- The Redis connection string is loaded from configuration
- In development, the existing Docker Compose Redis container is used
- The application starts successfully if Redis is unavailable (logs a warning, does not crash)
- Multiple IDP instances share the same key ring (prerequisite for horizontal scaling)

## Technical Notes

- Add NuGet packages: `Microsoft.AspNetCore.DataProtection.StackExchangeRedis`, `StackExchange.Redis`
- Register in `Program.cs`:
  ```csharp
  builder.Services
      .AddDataProtection()
      .PersistKeysToStackExchangeRedis(ConnectionMultiplexer.Connect(redisConnectionString), "DataProtection-Keys")
      .SetApplicationName("See.Idp");
  ```
- Add connection string key `ConnectionStrings:Redis` to `appsettings.json`
  (value: `localhost:6379` for local dev)
- Use `SetApplicationName` to ensure key isolation if Redis is shared with other apps later

## Dependencies

- `05-persistent-keys` — do keys/certs together to avoid partial persistence state

## Implementation

**Status:** ✅ Done

**Files changed:**
- `src/See.Idp.Web/See.Idp.Web.csproj` — added `Microsoft.AspNetCore.DataProtection.StackExchangeRedis`
  10.0.5 and `StackExchange.Redis` 2.8.41.
- `src/See.Idp.Infrastructure/Logging/EventIds.cs` — added `DataProtectionRedisUnavailable = 2000`.
- `src/See.Idp.Web/appsettings.json` — added `ConnectionStrings:Redis` (empty default).
- `src/See.Idp.Web/appsettings.Development.json` — set `ConnectionStrings:Redis` to `localhost:6379`.
- `src/See.Idp.Web/appsettings.Production.json` — added `ConnectionStrings:Redis` placeholder.
- `src/See.Idp.Web/Program.cs` — if `ConnectionStrings:Redis` is set, attempts
  `ConnectionMultiplexer.Connect` synchronously at startup; on success calls
  `PersistKeysToStackExchangeRedis` + `SetApplicationName("See.Idp")`; on
  `RedisConnectionException` falls back to in-memory Data Protection and logs event 2000
  after `builder.Build()` (the first point where an `ILogger` is available).
