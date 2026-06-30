# 25 — AuthorizationController bypasses the service layer

## Context

`AuthorizationController` (Web layer) directly injects `UserManager<ApplicationUser>` and
`SignInManager<ApplicationUser>` — concrete Infrastructure types — rather than going through
the service interfaces defined in Core:

```csharp
// src/See.Idp.Web/Controllers/AuthorizationController.cs:23–24
public sealed class AuthorizationController(
    IOpenIddictApplicationManager applicationManager,
    IOpenIddictAuthorizationManager authorizationManager,
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager
)
```

This is the only place in the codebase where the Web layer reaches past Core interfaces into
an Infrastructure entity. The consequences are concrete:

- The 307-line controller is the only place that builds user identity claims
  (`BuildUserIdentityAsync`), handles the token exchange flow, and checks sign-in eligibility
  after refresh. None of this logic is reachable via the service interfaces and none of it
  has unit tests.
- `ApplicationUser` (an EF Core Identity entity) is a structural dependency of the Web
  project. Adding a required property or constructor parameter to `ApplicationUser` forces a
  change in the controller.
- The service abstraction layer advertised as the boundary between Web and Infrastructure is
  simply not used for the most security-sensitive flows: token issuance and userinfo.
- `GetDestinations` — which controls which claims appear in access tokens vs. identity
  tokens — is a private static method in the controller and cannot be tested or reused.

## Fix

Extract the identity-to-claims logic into a new Core interface
`ITokenClaimsService` with a single method:

```csharp
// See.Idp.Core/Services/Auth/ITokenClaimsService.cs
public interface ITokenClaimsService
{
    Task<ClaimsIdentity?> BuildUserIdentityAsync(
        string userId,
        ImmutableArray<string> scopes,
        CancellationToken ct = default
    );
}
```

The implementation in Infrastructure wraps `UserManager` and `SignInManager`, replicates
the existing `BuildUserIdentityAsync` + `GetDestinations` logic, and can be tested in
isolation without an HTTP context.

The controller then injects `ITokenClaimsService` instead of `UserManager` and
`SignInManager`:

- `Authorize` endpoint: call `ITokenClaimsService.BuildUserIdentityAsync` with the
  authenticated user's ID and the requested scopes.
- `Token` endpoint (exchange flow): same — resolve the user ID from the refresh grant's
  principal, call `BuildUserIdentityAsync`.
- `Token` endpoint (client credentials flow): unchanged — no user involved, identity is
  built inline from the client ID.
- `Logout` endpoint: replace `signInManager.SignOutAsync()` with
  `IAuthenticationCommandService.SignOutAsync()`.
- `UserInfo` endpoint: replace direct `UserManager` calls with `IUserQueryService`
  (`GetUserProfileAsync`, `FindUserIdByEmailAsync`) and `ITokenClaimsService` for the roles
  claim.

Delete `using See.Idp.Infrastructure;` from `AuthorizationController.cs`.

## Acceptance Criteria

- `AuthorizationController` does not inject `UserManager<ApplicationUser>` or
  `SignInManager<ApplicationUser>`
- `using See.Idp.Infrastructure` does not appear in `AuthorizationController.cs`
- `ITokenClaimsService` is defined in `See.Idp.Core/Services/Auth/`
- `TokenClaimsService` is implemented in `See.Idp.Infrastructure/Services/`
- `GetDestinations` logic lives in `TokenClaimsService`, not in the controller
- `Logout` delegates to `IAuthenticationCommandService.SignOutAsync()`
- `TokenClaimsService` is covered by unit tests: at minimum one test per supported scope
  (`email`, `profile`, `roles`) and one for sign-in eligibility rejection
- `dotnet build` and `dotnet test` pass clean

## Dependencies

None. Can be implemented independently of other open issues.
