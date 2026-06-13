using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;

namespace See.Idp.Web.Cors;

/// <summary>
///     Builds CORS policies dynamically from the registered OpenIddict client redirect URIs,
///     so any browser SPA client whose redirect URI is in the registry is automatically allowed.
/// </summary>
public sealed class DynamicCorsPolicyProvider : ICorsPolicyProvider
{
    public async Task<CorsPolicy?> GetPolicyAsync(HttpContext context, string? policyName)
    {
        var manager = context.RequestServices.GetRequiredService<IOpenIddictApplicationManager>();
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

        return new CorsPolicyBuilder([.. origins])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .Build();
    }
}
