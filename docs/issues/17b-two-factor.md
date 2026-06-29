# 17b — Two-Factor Authentication

Sub-issue of #17. Implement all changes in the Two-Factor Authentication domain.
See issue #17 for full context, DTO field definitions, and the complete target service
structure.

## Scope

### DTOs (Core)

Create/update per the target layout in issue #17:

- `Dtos/Auth/TwoFactorCommands.cs` — split `TwoFactorSignInResult` and
  `RecoveryCodeSignInResult` into separate types; split `EnableTwoFactorResult` from
  `GenerateRecoveryCodesResult`; add `ProvisionAuthenticatorKeyCommand`
- `Dtos/Auth/TwoFactorQueries.cs` — `GetTwoFactorInfoQuery`, `GetAuthenticatorSetupQuery`
  (unchanged)
- `Dtos/Auth/TwoFactorDtos.cs` — new file: `TwoFactorInfo`, `AuthenticatorSetupInfo`
  (moved from `TwoFactorQueries.cs`)

### Interfaces (Core)

- `ITwoFactorCommandService` — add `ProvisionAuthenticatorKeyAsync` returning
  `CommandResult`; change `EnableTwoFactorAsync` return type from
  `GenerateRecoveryCodesResult` to `EnableTwoFactorResult`
- `ITwoFactorQueryService` — `GetAuthenticatorSetupAsync` doc updated to state it returns
  `null` if no key is provisioned (signature unchanged)

### Implementations (Infrastructure)

- Split `TwoFactorService` into two classes:
    - `TwoFactorCommandService` implements `ITwoFactorCommandService`:
      `ProvisionAuthenticatorKey` (idempotent — no-op if key exists),
      `EnableTwoFactor`, `DisableTwoFactor`, `ResetAuthenticatorKey`,
      `GenerateRecoveryCodes`. Dependencies: `UserManager`, `ILogger`.
    - `TwoFactorQueryService` implements `ITwoFactorQueryService`:
      `GetTwoFactorInfo`, `GetAuthenticatorSetup` (pure read — no lazy init).
      Dependencies: `UserManager`, `SignInManager`, `ILogger`.
- `ProvisionAuthenticatorKeyAsync` implementation:
    ```
    var key = await userManager.GetAuthenticatorKeyAsync(user);
    if (!string.IsNullOrEmpty(key)) return CommandResult.Success();
    await userManager.ResetAuthenticatorKeyAsync(user);
    return CommandResult.Success();
    ```
- `GetAuthenticatorSetupAsync` implementation — remove the lazy-init block:
    ```
    var key = await userManager.GetAuthenticatorKeyAsync(user);
    if (string.IsNullOrEmpty(key)) return null;
    // build and return AuthenticatorSetupInfo
    ```
- Delete `TwoFactorService.cs`
- Update `Program.cs` DI registrations

### Pages (Web)

- `TwoFactorAuthentication.cshtml.cs` — add `ITwoFactorCommandService` constructor
  dependency; add `OnPostProvisionAuthenticatorAsync` handler that calls
  `ProvisionAuthenticatorKeyAsync` then `RedirectToPage("./EnableAuthenticator")`
- `TwoFactorAuthentication.cshtml` — change "Enable authenticator app" from an `<a>` tag
  to a `<form method="post" asp-page-handler="ProvisionAuthenticator">`; change "Reset
  authenticator app" link target from `./EnableAuthenticator` to `./ResetAuthenticator`
- `EnableAuthenticator.cshtml.cs` — `OnGetAsync` returns `NotFound()` if
  `GetAuthenticatorSetupAsync` returns `null`; `OnPostAsync` updated to use
  `EnableTwoFactorResult` instead of `GenerateRecoveryCodesResult`
- `LoginWith2fa.cshtml.cs` — update result handling to use `TwoFactorSignInResult`
  (interface unchanged, but verify no compile errors from result type split)
- `LoginWithRecoveryCode.cshtml.cs` — update to use `RecoveryCodeSignInResult`

### Tests

- Delete `TwoFactorServiceTests.cs` if it exists
- Create `TwoFactorCommandServiceTests.cs` — cover `ProvisionAuthenticatorKeyAsync`
  (provisions when no key exists; no-op when key already exists), `EnableTwoFactorAsync`,
  `DisableTwoFactorAsync`, `ResetAuthenticatorKeyAsync`, `GenerateRecoveryCodesAsync`
- Create `TwoFactorQueryServiceTests.cs` — cover `GetTwoFactorInfoAsync`,
  `GetAuthenticatorSetupAsync` (returns info when key exists; returns null when no key)

## Acceptance Criteria

- `TwoFactorService.cs` does not exist
- `TwoFactorCommandService` and `TwoFactorQueryService` exist as separate classes
- `TwoFactorSignInResult` and `RecoveryCodeSignInResult` are separate types
- `EnableTwoFactorResult` and `GenerateRecoveryCodesResult` are separate types
- `TwoFactorDtos.cs` exists with `TwoFactorInfo` and `AuthenticatorSetupInfo`
- `GetAuthenticatorSetupAsync` contains no calls to `ResetAuthenticatorKeyAsync`;
  returns `null` when no key is provisioned
- `ProvisionAuthenticatorKeyAsync` is idempotent — does not overwrite an existing key
- `TwoFactorAuthentication` page: "Enable authenticator app" posts to
  `ProvisionAuthenticatorKeyAsync` then redirects; "Reset authenticator app" links to
  `./ResetAuthenticator`
- `EnableAuthenticator.OnGetAsync` returns `NotFound()` when setup info is null
- `dotnet build` and `dotnet test` pass clean

## Dependencies

Can be implemented independently of 17a. Implement against current codebase state.
