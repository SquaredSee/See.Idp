# 02 — Userinfo Endpoint

## Context

The OpenID Connect userinfo endpoint (`GET /connect/userinfo`) is how client applications
retrieve user profile information using an access token. It is called by most OIDC client
libraries after the token exchange and is required for `profile` and `email` scope claims
to be accessible without embedding everything in the id_token. Currently, the endpoint is
not registered in OpenIddict and no handler exists.

## User Story

As a client application, I want to call the userinfo endpoint with an access token so that
I can retrieve the authenticated user's profile information (name, email, roles) without
having to decode the token myself.

## Acceptance Criteria

- `GET /connect/userinfo` returns a JSON object containing claims for the authenticated user
- Response includes `sub`, `email`, `email_verified` when the `email` scope was granted
- Response includes `name`, `preferred_username` when the `profile` scope was granted
- Response includes `roles` when the `roles` scope was granted
- Endpoint returns `401` when called without a valid access token
- Endpoint is registered in OpenIddict's server configuration
- The endpoint appears in the OIDC discovery document at `/.well-known/openid-configuration`

## Technical Notes

- Register the endpoint: `options.SetUserinfoEndpointUris("connect/userinfo")` in `Program.cs`
- Add a `UserInfoController` (or extend `AuthorizationController`) with a `[HttpGet("connect/userinfo")]` action
- Authenticate the request using `OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme`
- Add `AddValidation` to the OpenIddict builder in `Program.cs` with `UseAspNetCore()`
  and `UseLocalServer()` so the validation handler trusts tokens issued by this server
- Extract claims from the validated principal and return as `OkObjectResult`

## Dependencies

- `01-authorization-controller` — tokens must carry claims before userinfo can return them

## Implementation

**Completed.** Commit: `feat(auth): add userinfo endpoint`

### Files changed

- `src/See.Idp.Web/Controllers/AuthorizationController.cs` — added `UserInfo` action
- `src/See.Idp.Web/Program.cs` — added `SetUserInfoEndpointUris`, `EnableUserInfoEndpointPassthrough`

### Notes

- No extra NuGet package required — `EnableUserInfoEndpointPassthrough()` on the server's
  ASP.NET Core builder is sufficient; the `OpenIddict.Validation.AspNetCore` package is not needed
- Method casing is `SetUserInfoEndpointUris` and `EnableUserInfoEndpointPassthrough`
  (capital `I` in `Info`) — inconsistent with `SetEndSessionEndpointUris` but matches the XML docs
- The action uses `[Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]`
  so OpenIddict validates the bearer token before the action runs; `User` is populated directly
- Scope-gated: `email` claims only returned when `email` scope granted, `profile` when `profile`
  scope granted, `roles` when `roles` scope granted
