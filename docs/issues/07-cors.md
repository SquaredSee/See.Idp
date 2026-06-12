# 07 — CORS Configuration

## Context

No CORS policy is configured. Any browser-based client application (SPA, Next.js, etc.)
attempting to make requests to the token endpoint, userinfo endpoint, or any other IDP
API will be blocked by the browser's same-origin policy. CORS must be configured before
any frontend application can use this IDP.

## User Story

As a developer building a browser-based application, I want the IDP to return correct CORS
headers so that my application can call the token and userinfo endpoints from the browser.

## Acceptance Criteria

- A named CORS policy allows configurable origins to call the IDP
- Allowed origins are loaded from configuration (not hardcoded)
- The policy is applied to `connect/token`, `connect/userinfo`, and the authorization endpoint
- Preflight (`OPTIONS`) requests are handled correctly
- Credentials (cookies) are allowed when needed
- The default configuration allows `localhost` variants in development
- Invalid or unlisted origins are rejected

## Technical Notes

- Add `builder.Services.AddCors(...)` in `Program.cs` with a named policy `"IdpCors"`
- Load allowed origins from `appsettings.json` under `Cors:AllowedOrigins` (string array)
- Apply with `app.UseCors("IdpCors")` — must be placed between `UseRouting` and
  `UseAuthentication` in the middleware pipeline
- Verify CORS headers appear on OpenIddict responses; add explicit `[EnableCors]` to the
  authorization controller if needed
- `appsettings.Development.json` should include `http://localhost:3000`, `https://localhost:3000`,
  and other common local dev ports

## Dependencies

- `02-userinfo-endpoint` — need the endpoints to exist before testing CORS against them
