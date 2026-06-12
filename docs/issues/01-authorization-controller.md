# 01 — Authorization Controller & Claims

## Context

OpenIddict does not populate token claims automatically. Without an authorization controller, the `connect/authorize` and `connect/token` endpoints have no handler, so no user identity is ever established and all issued tokens are empty. This is the single most important missing piece before any client application can use this IDP.

## User Story

As a developer building an application against this IDP, I want tokens to contain real user claims (subject, email, roles, etc.) so that my application can identify and authorize users.

## Acceptance Criteria

- A controller exists that handles `GET /connect/authorize`, `POST /connect/authorize`, `POST /connect/token`, and `GET+POST /connect/logout`
- Issued access tokens contain at minimum: `sub`, `email`, `email_verified`, `roles`
- Issued identity tokens contain at minimum: `sub`, `email`, `name`
- Claim destinations are set correctly — `email` and `profile` claims only flow to id_token and/or userinfo when the corresponding scope is requested
- Client credentials flow issues tokens scoped to the client (no user claims)
- Logout signs the user out of ASP.NET Identity and redirects correctly
- Existing admin portal and all Razor Page flows continue to work

## Technical Notes

- Add an MVC controller to `src/See.Idp.Web/Controllers/AuthorizationController.cs`
- Enable MVC in `Program.cs` (`AddControllersWithViews` + `MapControllers`)
- Use `OpenIddictServerAspNetCoreDefaults.AuthenticationScheme` to extract the OpenIddict request
- Use `IOpenIddictApplicationManager` and `IOpenIddictAuthorizationManager` from DI
- Fetch the `ApplicationUser` via `UserManager` to populate claims
- Build the `ClaimsIdentity` using `OpenIddictConstants.Claims` constants
- Set `claim.SetDestinations(...)` on each claim using `OpenIddictConstants.Destinations`
- For `connect/logout`, call `SignOutAsync` for both the Identity cookie scheme and the OpenIddict server scheme
- Reference the [OpenIddict samples](https://github.com/openiddict/openiddict-samples) (Velusia sample)

## Dependencies

None — this is the first issue.

## Implementation

**Completed.** Commit: `feat(auth): add authorization controller and claims`

### Files changed

- `src/See.Idp.Web/Controllers/AuthorizationController.cs` — new controller
- `src/See.Idp.Web/Program.cs` — `AddControllersWithViews`, endpoint passthrough, `MapControllers`

### Notes

- `OpenIddictServerAspNetCoreHelpers` (`GetOpenIddictServerRequest`) lives in the
  `Microsoft.AspNetCore` namespace, not `OpenIddict.Server.AspNetCore` — non-obvious, requires
  an explicit `using Microsoft.AspNetCore;`
- Consent is currently auto-approved (ad-hoc authorization created on every request).
  Issue 03 replaces this with the explicit consent page.
