# 08 — Rate Limiting

## Context

The login page and token endpoint are currently unprotected against brute-force and
credential-stuffing attacks. An IDP is a high-value target — compromising it grants access
to every application that trusts it. Rate limiting is a basic but essential mitigation.

## User Story

As the IDP operator, I want login attempts and token requests to be rate-limited so that
automated attacks cannot brute-force user credentials.

## Acceptance Criteria

- `POST /Identity/Account/Login` is rate-limited per IP address
- `POST /connect/token` is rate-limited per IP address
- Requests that exceed the limit receive a `429 Too Many Requests` response
- Limits are configurable via `appsettings.json`
- Rate limiting does not affect normal interactive users (limits are generous enough
  for legitimate use)
- Rejected requests are logged

## Technical Notes

- Use ASP.NET Core's built-in `AddRateLimiter` / `UseRateLimiter`
- Use a fixed window or sliding window limiter — a starting point:
    - Login: 10 requests / 1 minute per IP
    - Token endpoint: 30 requests / 1 minute per IP
- Apply via `[EnableRateLimiting("policy-name")]` on the controller action or via
  endpoint metadata in `Program.cs`
- Load limits from configuration under `RateLimiting:Login` and `RateLimiting:Token`
- Use `context.Connection.RemoteIpAddress` as the partition key; account for
  reverse-proxy scenarios by configuring `ForwardedHeaders` middleware

## Dependencies

- `01-authorization-controller` — token endpoint controller must exist before applying
  rate limiting to it

## Implementation

**Status:** ✅ Done

**Files changed:**

- `src/See.Idp.Web/appsettings.json` — added `RateLimiting.Login` (10 req/60 s) and
  `RateLimiting.Token` (30 req/60 s) configuration sections.
- `src/See.Idp.Infrastructure/Logging/EventIds.cs` — added `RateLimitExceeded = 1900`.
- `src/See.Idp.Web/Program.cs` — `AddRateLimiter` with named policies `"login"` and `"token"`
  (fixed-window, IP-partitioned, limits read from config); `OnRejected` logs the event and
  returns 429; `UseRateLimiter()` placed after `UseRouting` and before `UseCors`.
- `src/See.Idp.Web/Controllers/AuthorizationController.cs` — `[EnableRateLimiting("token")]`
  on the `Exchange` action.
- `src/See.Idp.Web/Areas/Identity/Pages/Account/Login.cshtml.cs` —
  `[EnableRateLimiting("login")]` on `LoginModel`.
