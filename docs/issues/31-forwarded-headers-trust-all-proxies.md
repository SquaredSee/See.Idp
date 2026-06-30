# 31 — ForwardedHeadersOptions trusts all proxies unconditionally

## Context

`Program.cs` clears both `KnownIPNetworks` and `KnownProxies` before enabling forwarded
header processing:

```csharp
// src/See.Idp.Web/Program.cs:40–43
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});
```

ASP.NET Core's forwarded headers middleware will only rewrite `RemoteIpAddress` from
an `X-Forwarded-For` value if the request originates from a known proxy or network.
Clearing both lists removes this guard entirely: **any source IP can inject an arbitrary
`X-Forwarded-For` header and have it treated as the real client IP**.

This directly undermines the rate limiter, which partitions by
`context.Connection.RemoteIpAddress` (resolved after forwarded-header rewriting). An
attacker sending repeated login attempts can rotate spoofed IPs in `X-Forwarded-For` to
circumvent the fixed-window rate limit on the `"login"` policy entirely.

The inline comment acknowledges the risk:

```csharp
// restrict these in environments where the network boundary is untrusted.
```

but leaves the decision unresolved.

## Fix

Add a configuration option for trusted proxy CIDRs and apply it at startup:

```json
// appsettings.json
{
    "ReverseProxy": {
        "TrustedNetworks": ["10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16"]
    }
}
```

```csharp
// Program.cs
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    var networks = builder.Configuration
        .GetSection("ReverseProxy:TrustedNetworks")
        .Get<string[]>();

    if (networks is { Length: > 0 })
    {
        foreach (var cidr in networks)
        {
            var parts = cidr.Split('/');
            if (parts.Length == 2
                && IPAddress.TryParse(parts[0], out var prefix)
                && int.TryParse(parts[1], out var length))
            {
                options.KnownNetworks.Add(new IPNetwork(prefix, length));
            }
        }
    }
    else
    {
        // No trusted networks configured: accept only localhost (default ASP.NET Core
        // behaviour). Log a warning so this state is visible in production.
    }
});
```

Log a startup warning when `TrustedNetworks` is empty and the application is not in
Development, so the misconfiguration is visible in production before an attack occurs.

Update `appsettings.Development.json` to trust loopback and Docker bridge networks.
Update `k8s/` manifests or deployment docs with the expected in-cluster CIDR.

## Acceptance Criteria

- `ForwardedHeadersOptions` does not have both `KnownIPNetworks` and `KnownProxies`
  cleared in the same configuration block without a replacement trust list
- Trusted proxy networks are configurable via `appsettings.json` /
  `REVERSEPROXY__TRUSTEDNETWORKS` environment variable
- A startup warning is emitted when no trusted networks are configured in non-Development
  environments
- `appsettings.Development.json` includes a trust entry for loopback (`127.0.0.1/8`)
- `dotnet build` and `dotnet test` pass clean

## Dependencies

None.
