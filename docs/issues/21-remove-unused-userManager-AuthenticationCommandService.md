# 21 — Remove unused `UserManager` from `AuthenticationCommandService`

## Context

`AuthenticationCommandService` injects `UserManager<ApplicationUser>` in its primary
constructor but never references it. All operations delegate to `SignInManager`. The
unused parameter produces compiler warning `CS9113`.

## Fix

Remove `UserManager<ApplicationUser> userManager` from the primary constructor.

```csharp
// Before:
public sealed partial class AuthenticationCommandService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ILogger<AuthenticationCommandService> logger
)

// After:
public sealed partial class AuthenticationCommandService(
    SignInManager<ApplicationUser> signInManager,
    ILogger<AuthenticationCommandService> logger
)
```

## Acceptance Criteria

- `AuthenticationCommandService` primary constructor has no `UserManager` parameter
- `dotnet build` produces no `CS9113` warning for this file
- `dotnet test` passes clean
