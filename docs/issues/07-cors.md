# 07 — CORS Configuration

## Context

No CORS policy is configured. Any browser-based client application (SPA, etc.) attempting
to make requests to the token endpoint or userinfo endpoint will be blocked by the browser's
same-origin policy.

CORS should be driven automatically by the OpenIddict client registry — if an app has been
properly registered with the IDP, its origin should be allowed without any additional
configuration. Maintaining a separate static list in `appsettings.json` would mean every
new client registration requires a config change in two places, which is error-prone and
unnecessary.

**Note:** CORS is only relevant for browser-based JavaScript clients (SPAs) making direct
API calls. Server-side clients (ASP.NET Core apps using `AddOpenIdConnect`) make
server-to-server requests that never send an `Origin` header — CORS does not apply to them.

## User Story

As a developer registering a client application in the IDP, I want CORS to be automatically
satisfied for my app's origin so that I don't need to update any additional configuration
beyond registering the client.

## Acceptance Criteria

- Allowed origins are derived from registered client redirect URIs in the OpenIddict store
- No static origin list in `appsettings.json` is required
- Registering a new client with a redirect URI of `https://app.example.com/callback`
  automatically allows `https://app.example.com` as a CORS origin
- Preflight (`OPTIONS`) requests are handled correctly
- Origins not matching any registered client are rejected
- Development workflow is unaffected

## Technical Notes

- Implement a custom `ICorsPolicyProvider` that queries `IOpenIddictApplicationManager`
  to derive allowed origins dynamically:
    ```csharp
    public sealed class OpenIddictCorsPolicyProvider(
        IOpenIddictApplicationManager applicationManager
    ) : ICorsPolicyProvider
    {
        public async Task<CorsPolicy?> GetPolicyAsync(HttpContext context, string? policyName)
        {
            var origin = context.Request.Headers.Origin.ToString();
            if (string.IsNullOrEmpty(origin))
                return null;

            // Check if any registered client has a redirect URI matching this origin
            await foreach (var application in applicationManager.ListAsync())
            {
                await foreach (var uri in applicationManager.GetRedirectUrisAsync(application))
                {
                    if (new Uri(uri).GetLeftPart(UriPartial.Authority)
                            .Equals(origin, StringComparison.OrdinalIgnoreCase))
                    {
                        return new CorsPolicyBuilder()
                            .WithOrigins(origin)
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials()
                            .Build();
                    }
                }
            }

            return null;
        }
    }
    ```
- Register in `Program.cs`:
    ```csharp
    builder.Services.AddCors();
    builder.Services.AddSingleton<ICorsPolicyProvider, OpenIddictCorsPolicyProvider>();
    ```
- Call `app.UseCors()` between `UseRouting` and `UseAuthentication`
- The provider lives in `See.Idp.Web` (it's a web concern, not infrastructure)
- `IOpenIddictApplicationManager.ListAsync()` and `GetRedirectUrisAsync()` are available
  via the existing OpenIddict core registration

## Dependencies

- `02-userinfo-endpoint` — endpoints must exist before CORS is needed

## Implementation

**Status:** ✅ Done

**Files changed:**

- `src/See.Idp.Web/Cors/DynamicCorsPolicyProvider.cs` — new; iterates all OpenIddict applications
  via `IOpenIddictApplicationManager.ListAsync()`, extracts origins from redirect URIs, builds a
  `CorsPolicy` allowing all discovered origins with any header/method + credentials.
- `src/See.Idp.Web/Program.cs` — `builder.Services.AddCors()` + singleton registration of
  `DynamicCorsPolicyProvider` as `ICorsPolicyProvider`; `app.UseCors()` inserted between
  `app.UseRouting()` and `app.UseAuthentication()`.

**Design decisions:**

- Provider is `AddSingleton` — it resolves `IOpenIddictApplicationManager` per-request via
  `context.RequestServices` (scoped services resolved at request time, not at construction time).
- Returns `null` when no redirect URIs are registered, which causes the CORS middleware to allow
  the request without origin restriction (same as no CORS middleware). This is safe because the
  IDP has no registered clients at that point anyway.
- All registered client origins are allowed unconditionally (any header, any method, credentials).
  Access control is enforced by OpenIddict token validation, not CORS policy.
