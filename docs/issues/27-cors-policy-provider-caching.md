# 27 — DynamicCorsPolicyProvider queries the database on every CORS-checked request

## Context

`DynamicCorsPolicyProvider` is registered as a `Singleton` but calls
`IOpenIddictApplicationManager.ListAsync` on every invocation of `GetPolicyAsync`,
iterating the full client table to build the allowed-origins set:

```csharp
// src/See.Idp.Web/Cors/DynamicCorsPolicyProvider.cs:20–40
public async Task<CorsPolicy?> GetPolicyAsync(HttpContext context, string? policyName)
{
    var manager = context.RequestServices.GetRequiredService<IOpenIddictApplicationManager>();
    var origins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    await foreach (var application in manager.ListAsync(...))
    {
        // ... extract redirect URI origins ...
    }
    ...
}
```

This means:

- Every browser CORS preflight (`OPTIONS`) request triggers a full database table scan.
  For OIDC endpoints that SPA clients hit repeatedly, this is a DB read on every
  interaction cycle.
- At 50 registered clients with 3 redirect URIs each, each preflight performs 150 or more
  individual OpenIddict async calls against the database.
- A single client making rapid preflight requests (intentional or not) can drive
  unbounded read load on the database with no rate limiting at the CORS layer.
- The `Singleton` registration is misleading: the provider holds no state between calls
  and computes the same result every time.

## Fix

Cache the computed `CorsPolicy` (or the derived origins set) using `IMemoryCache` with a
short absolute expiry (e.g. 60 seconds). Introduce a simple cache key:

```csharp
public sealed class DynamicCorsPolicyProvider(
    IMemoryCache cache,
    IServiceProvider services
) : ICorsPolicyProvider
{
    private const string CacheKey = "DynamicCorsPolicy";
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);

    public async Task<CorsPolicy?> GetPolicyAsync(HttpContext context, string? policyName)
    {
        if (cache.TryGetValue(CacheKey, out CorsPolicy? cached))
            return cached;

        await using var scope = services.CreateAsyncScope();
        var manager = scope.ServiceProvider
            .GetRequiredService<IOpenIddictApplicationManager>();

        var origins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await foreach (var application in manager.ListAsync(...))
        {
            // ... existing origin extraction logic ...
        }

        if (origins.Count == 0)
            return null;

        var policy = new CorsPolicyBuilder([.. origins])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .Build();

        cache.Set(CacheKey, policy, Ttl);
        return policy;
    }
}
```

Register `IMemoryCache` in `Program.cs` if not already present (`AddMemoryCache()`).

Invalidate the cache entry on client create, update, and delete. The cleanest approach is
to inject `IMemoryCache` into `ClientCommandService` and call
`cache.Remove(DynamicCorsPolicyProvider.CacheKey)` (or a shared constant) at the end of
`CreateClientAsync`, `UpdateClientAsync`, and `DeleteClientAsync`.

Alternatively, publish an `IMemoryCache`-based eviction message. The invalidation-in-command
service approach is simpler and sufficient for this scale.

## Acceptance Criteria

- `DynamicCorsPolicyProvider.GetPolicyAsync` queries the database at most once per 60-second
  window under normal operation
- The cache is invalidated when a client is created, updated, or deleted
- `IMemoryCache` is injected into `DynamicCorsPolicyProvider` (not service-located per
  request)
- Behaviour when no clients are registered is unchanged (returns `null`)
- `dotnet build` and `dotnet test` pass clean

## Dependencies

None.
