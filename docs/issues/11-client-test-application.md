# 11 — Client Test Application

## Context

There is no application consuming this IDP. Building a real client is the most effective
way to validate the end-to-end OIDC flow — every gap in claims, CORS headers, consent
behaviour, and session handling surfaces immediately when a real browser makes real requests.
This also becomes a second portfolio piece demonstrating the IDP in use.

## User Story

As a developer, I want a working example application that authenticates via the IDP so
that I can demonstrate the full OIDC flow and have confidence the IDP works correctly.

## Acceptance Criteria

- A new ASP.NET Core Razor Pages application exists under `clients/See.Client.Web/`
- The application requires login to access a protected page
- Clicking login redirects to the IDP's authorization endpoint
- After successful login, the user is redirected back and the protected page displays
  their `sub`, `email`, and `roles` claims from the token
- Logout signs the user out of both the client app and the IDP (end-session flow)
- The client is registered in the IDP via the `Initialization` config section
- The application runs alongside the IDP in `docker-compose.yml`

## Technical Notes

- Create the project: `dotnet new webapp -n See.Client.Web -o clients/See.Client.Web`
- Add `Microsoft.AspNetCore.Authentication.OpenIdConnect` NuGet package
- Register in `Program.cs`:
    ```csharp
    builder.Services
        .AddAuthentication(...)
        .AddCookie()
        .AddOpenIdConnect(options =>
        {
            options.Authority = "https://localhost:5001";
            options.ClientId = "see.client";
            options.ClientSecret = "...";
            options.ResponseType = "code";
            options.Scope.Add("email");
            options.Scope.Add("profile");
            options.Scope.Add("roles");
            options.SaveTokens = true;
            options.GetClaimsFromUserInfoEndpoint = true;
        });
    ```
- Add the client to `appsettings.json` under the IDP's `Initialization:Clients` section
- A single protected `/Profile` page that displays claims is sufficient

## Dependencies

- `01-authorization-controller`
- `02-userinfo-endpoint`
- `03-consent-page`
- `07-cors`

## Implementation

**Status:** ✅ Done

**Scope:** Minimal auth-test client only — no UI polish, just a working OIDC round-trip.

**Files:**

- `clients/See.Client.Web/` — new ASP.NET Core Razor Pages app (net10.0).
- `Program.cs` — `AddAuthentication` with cookie + OIDC (`code` flow, `email`/`profile`/`roles`
  scopes, `GetClaimsFromUserInfoEndpoint = true`, `MapInboundClaims = false`); `GET /logout`
  signs out of both cookie and OIDC (end-session flow).
- `appsettings.json` — `Oidc:Authority`, `Oidc:ClientId`, `Oidc:ClientSecret` keys.
- `Pages/Profile.cshtml[.cs]` — `[Authorize]` page displaying `sub`, `email`, and `roles`.
- `Dockerfile` — multi-stage build for docker-compose use.
- `src/See.Idp.Web/appsettings.Development.json` — added `see-client-web` client registration
  with redirect URI `https://localhost:7002/signin-oidc`.
- `docker-compose.yml` — added `client-web` service on port 7002.
