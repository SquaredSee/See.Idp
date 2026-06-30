# 33 — Rate limiter OnRejected uses service location for logging

## Context

The `OnRejected` delegate in the rate limiter configuration resolves an `ILogger` via
service location on every rejection event:

```csharp
// src/See.Idp.Web/Program.cs:~220
options.OnRejected = async (context, ct) =>
{
    var logger = context
        .HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
        .CreateLogger("RateLimiting");

    logger.LogWarning(
        new EventId(EventIds.RateLimitExceeded, nameof(EventIds.RateLimitExceeded)),
        "Rate limit exceeded for {Path} from {IP}",
        context.HttpContext.Request.Path,
        context.HttpContext.Connection.RemoteIpAddress
    );
    ...
};
```

This is inconsistent with every other logging call in the codebase, which either uses
constructor-injected `ILogger<T>` instances or `[LoggerMessage]` source generation.
Service-locating a logger inside a hot path (rate limit rejections can be frequent under
an attack) creates a new `ILoggerFactory.CreateLogger` call on every rejection, allocating
a new logger instance each time.

The broader inconsistency is that `Program.cs` itself has no typed logger identity —
it uses the `app.Logger` property (category `See.Idp.Web.Program`) for the Data Protection
warning — while this delegate uses a string category `"RateLimiting"`. Both approaches
work but differ from each other and from the rest of the codebase.

## Fix

Resolve the logger once at configuration time by closing over a pre-built instance:

```csharp
var rateLimitLogger = LoggerFactory
    .Create(b => b.AddConfiguration(builder.Configuration.GetSection("Logging")))
    .CreateLogger("RateLimiting");

options.OnRejected = async (context, ct) =>
{
    rateLimitLogger.LogWarning(
        new EventId(EventIds.RateLimitExceeded, nameof(EventIds.RateLimitExceeded)),
        "Rate limit exceeded for {Path} from {IP}",
        context.HttpContext.Request.Path,
        context.HttpContext.Connection.RemoteIpAddress
    );
    ...
};
```

Alternatively, resolve the logger from `app.Services` after `var app = builder.Build()`
and before `app.Run()`, which is the standard approach for loggers needed in middleware
configuration:

```csharp
var app = builder.Build();
var rateLimitLogger = app.Services
    .GetRequiredService<ILoggerFactory>()
    .CreateLogger("RateLimiting");
```

Then update `AddRateLimiter` to use a method group or local function that closes over
`rateLimitLogger`.

## Acceptance Criteria

- `OnRejected` does not call `context.HttpContext.RequestServices.GetRequiredService<...>()`
- The logger used in `OnRejected` is resolved once, not per-rejection
- Rate limit exceeded events are still logged with `{Path}` and `{IP}` structured fields
- `dotnet build` and `dotnet test` pass clean

## Dependencies

None.
