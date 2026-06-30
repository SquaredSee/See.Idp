using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using See.Idp.Infrastructure.Cors;

namespace See.Idp.Web.Cors;

/// <summary>
///     Builds CORS policies dynamically from the registered OpenIddict client redirect URIs,
///     so any browser SPA client whose redirect URI is in the registry is automatically allowed.
///     Results are cached for 60 seconds; the cache is invalidated on client mutations.
/// </summary>
public sealed class DynamicCorsPolicyProvider(IMemoryCache cache, IServiceProvider services)
    : ICorsPolicyProvider
{
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);

    public async Task<CorsPolicy?> GetPolicyAsync(HttpContext context, string? policyName)
    {
        if (cache.TryGetValue(CorsCacheKeys.DynamicPolicy, out CorsPolicy? cached))
            return cached;

        await using var scope = services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        var origins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await foreach (
            var application in manager.ListAsync(cancellationToken: context.RequestAborted)
        )
        {
            ImmutableArray<string> redirectUris = await manager.GetRedirectUrisAsync(
                application,
                context.RequestAborted
            );

            foreach (var uri in redirectUris)
            {
                if (Uri.TryCreate(uri, UriKind.Absolute, out var parsed))
                    origins.Add($"{parsed.Scheme}://{parsed.Authority}");
            }
        }

        if (origins.Count == 0)
            return null;

        var policy = new CorsPolicyBuilder([.. origins])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .Build();

        cache.Set(CorsCacheKeys.DynamicPolicy, policy, Ttl);
        return policy;
    }
}
