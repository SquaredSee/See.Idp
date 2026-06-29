# 17 — CQRS Service Restructure

## Context

A full audit of the application's commands and queries, conducted without reference to the
existing service structure, produced a clean list of 27 commands and 7 queries. Mapping
these back to the existing 9 service interfaces revealed several structural problems:

- `RefreshSignInAsync` is a public interface method that is an implementation detail —
  called internally after password changes and unnecessarily from one Razor Page after a
  phone number update (`SetPhoneNumberAsync` does not update the SecurityStamp, so the
  refresh is a no-op)
- `GeneratePasswordResetTokenAsync` lives on `IUserRegistrationCommandService`, which has
  no relationship to password management; it belongs with the password lifecycle operations
- `GenerateEmailConfirmationTokenAsync` lives on `IUserQueryService` despite being a
  command-intent operation; it belongs on the registration service
- `GetAuthenticatorSetupAsync` on `ITwoFactorQueryService` contains a hidden DB write:
  `userManager.ResetAuthenticatorKeyAsync` is called lazily when no key exists, persisting
  a new TOTP secret and updating the SecurityStamp from inside a query method called from
  `OnGetAsync`
- The three seeding methods on `IUserCommandService` and `CreateClientIfMissing` on
  `IClientCommandService` are never called from a Razor Page and have no business on
  operational service interfaces
- `TwoFactorService` implements both `ITwoFactorCommandService` and
  `ITwoFactorQueryService` in one class — the structural condition that allowed the hidden
  mutation to accumulate unnoticed
- `UserAccountService` implements both `IUserAuthenticationCommandService` and
  `IUserPasswordCommandService` — the same structural problem; split for the same reason

Additionally, the DTO files are in a partially-applied state following issue #16 and
contain the following problems:

**Shared result types that should be unique per operation:**

- `TwoFactorSignInResult` is returned by both `TwoFactorSignIn` and `RecoveryCodeSignIn`;
  each operation should own its result type
- `GenerateRecoveryCodesResult` is returned by both `EnableTwoFactor` and
  `GenerateRecoveryCodes`; each operation should own its result type

**Missing result types — raw `string?` returns on service interfaces:**

- `GeneratePasswordResetTokenAsync` returns `string?`
- `GenerateEmailConfirmationTokenAsync` returns `string?`
- `FindUserByEmailAsync` returns `string?`

**Wrong-domain input records:**

- `ChangePasswordCommand` and `ResetPasswordCommand` are in `Dtos/Users/` but belong in
  `Dtos/Auth/` — they are credential operations, not user management operations
- `GeneratePasswordResetTokenCommand` is in `Dtos/Users/UserRegistrationCommands.cs` but
  belongs in `Dtos/Auth/PasswordCommands.cs`

**Misclassified and stale records:**

- `GenerateEmailConfirmationTokenQuery` has the wrong suffix; it is a command, not a query
- `GeneratePasswordResetTokenQuery` was added by a partial commit and should be deleted

**Interface naming:**

- `IUserAuthenticationCommandService`, `IUserPasswordCommandService`, and
  `IUserRegistrationCommandService` carry a redundant `User` prefix — the namespace
  already scopes them; drop it

**Sharing policy:**

- `CommandResult` and `CreateIfMissingResult` are intentionally shared — they are generic
  envelopes equivalent to a typed unit return. All other result types are unique per
  operation.

---

## Target Service Structure

10 interfaces replace the existing 9.

### `IAuthenticationCommandService`

_(renamed from `IUserAuthenticationCommandService`)_

Session management. Implemented by `AuthenticationCommandService` (split from
`UserAccountService`).

- `PasswordSignIn` → `PasswordSignInResult`
- `TwoFactorSignIn` → `TwoFactorSignInResult`
- `RecoveryCodeSignIn` → `RecoveryCodeSignInResult`
- `SignOut`

_Loses `RefreshSignIn` — not a public operation._

### `IPasswordCommandService`

_(renamed from `IUserPasswordCommandService`)_

Full password lifecycle. Implemented by `PasswordCommandService` (split from
`UserAccountService`). Retains `SignInManager` as a dependency because `ChangePassword`
calls `signInManager.RefreshSignInAsync` internally after updating the hash.

- `GeneratePasswordResetToken` → `GeneratePasswordResetTokenResult` _(moved from registration service)_
- `ResetPassword` → `CommandResult`
- `ChangePassword` → `CommandResult`

### `IRegistrationCommandService`

_(renamed from `IUserRegistrationCommandService`)_

Account creation and email verification. Implemented by `RegistrationCommandService`
(renamed from `UserRegistrationService`).

- `RegisterUser` → `RegisterUserResult`
- `ConfirmEmail` → `CommandResult`
- `GenerateEmailConfirmationToken` → `GenerateEmailConfirmationTokenResult` _(moved from `IUserQueryService`)_

### `ITwoFactorCommandService`

Implemented by `TwoFactorCommandService` (split from `TwoFactorService`).

- `ProvisionAuthenticatorKey` → `CommandResult` _(new — idempotent: no-op if a key already exists)_
- `EnableTwoFactor` → `EnableTwoFactorResult`
- `DisableTwoFactor` → `CommandResult`
- `ResetAuthenticatorKey` → `CommandResult`
- `GenerateRecoveryCodes` → `GenerateRecoveryCodesResult`

### `ITwoFactorQueryService`

Implemented by `TwoFactorQueryService` (split from `TwoFactorService`).

- `GetTwoFactorInfo` → `TwoFactorInfo?`
- `GetAuthenticatorSetup` → `AuthenticatorSetupInfo?` _(pure read; returns `null` if no key has been provisioned)_

### `IUserCommandService`

All writes to user data. Unchanged implementation class.

- `UpdatePhoneNumber` → `CommandResult`
- `ToggleUserAdmin` → `CommandResult`
- `ToggleUserLock` → `CommandResult`
- `DeleteUser` → `CommandResult`

_Loses `CreateRoleIfMissing`, `CreateUserIfMissing`, `AddUserToRoleIfMissing`._

### `IUserQueryService`

Unchanged implementation class.

- `GetUserProfile` → `UserProfileDto?`
- `ListUsers` → `IReadOnlyList<UserSummaryDto>`
- `FindUserByEmail` → `FindUserByEmailResult`

_Loses both `Generate*` methods — neither was a read._

### `IClientCommandService`

Unchanged implementation class.

- `CreateClient` → `CreateClientResult`
- `UpdateClient` → `CommandResult`
- `DeleteClient` → `CommandResult`
- `RotateClientSecret` → `RotateClientSecretResult`

_Loses `CreateClientIfMissing`._

### `IClientQueryService`

Unchanged.

- `ListClients` → `IReadOnlyList<ClientSummaryDto>`
- `GetClientById` → `ClientDetailsDto?`

### `IApplicationSeedCommandService` _(new)_

Idempotent initialisation — never called from a Razor Page. Implemented by
`ApplicationSeedCommandService`.

- `CreateRoleIfMissing` → `CreateIfMissingResult`
- `CreateUserIfMissing` → `CreateUserIfMissingResult`
- `AddUserToRoleIfMissing` → `CreateIfMissingResult`
- `CreateClientIfMissing` → `CreateIfMissingResult`

---

## Implementation Changes

### Rename interfaces and implementation classes

| Old name                            | New name                        |
| ----------------------------------- | ------------------------------- |
| `IUserAuthenticationCommandService` | `IAuthenticationCommandService` |
| `IUserPasswordCommandService`       | `IPasswordCommandService`       |
| `IUserRegistrationCommandService`   | `IRegistrationCommandService`   |
| `UserRegistrationService`           | `RegistrationCommandService`    |

Update all injection sites, `Program.cs` registrations, and test files accordingly.

### Split `UserAccountService`

`UserAccountService` currently implements both `IAuthenticationCommandService` and
`IPasswordCommandService`. Split into two classes:

- `AuthenticationCommandService` — `PasswordSignIn`, `TwoFactorSignIn`,
  `RecoveryCodeSignIn`, `SignOut`. Dependencies: `SignInManager`, `ILogger`.
- `PasswordCommandService` — `GeneratePasswordResetToken`, `ResetPassword`,
  `ChangePassword`. Dependencies: `UserManager`, `SignInManager` (for internal
  `RefreshSignInAsync` call after `ChangePassword`), `ILogger`.

### Split `TwoFactorService`

Split into:

- `TwoFactorCommandService` — all `ITwoFactorCommandService` methods including the new
  `ProvisionAuthenticatorKeyAsync`. Dependencies: `UserManager`, `ILogger`.
- `TwoFactorQueryService` — `GetTwoFactorInfo`, `GetAuthenticatorSetup`. Dependencies:
  `UserManager`, `SignInManager`, `ILogger`.

### Fix `GetAuthenticatorSetupAsync`

Remove the `userManager.ResetAuthenticatorKeyAsync` lazy-init block. The method becomes:

```
var key = await userManager.GetAuthenticatorKeyAsync(user);
if (string.IsNullOrEmpty(key))
    return null;
// build and return AuthenticatorSetupInfo
```

### Implement `ProvisionAuthenticatorKeyAsync`

Idempotent — only generates a key if none exists:

```
var key = await userManager.GetAuthenticatorKeyAsync(user);
if (!string.IsNullOrEmpty(key))
    return CommandResult.Success();
await userManager.ResetAuthenticatorKeyAsync(user);
return CommandResult.Success();
```

### Fix `EnableAuthenticator` page flow

`TwoFactorAuthentication.cshtml.cs` gains `OnPostProvisionAuthenticatorAsync`, which calls
`ProvisionAuthenticatorKeyAsync` then redirects to `EnableAuthenticator`. The page model
gains `ITwoFactorCommandService` as a constructor dependency.

In `TwoFactorAuthentication.cshtml`:

- "Enable authenticator app" — change from `<a asp-page>` to a `<form method="post">`
  with `asp-page-handler="ProvisionAuthenticator"`
- "Reset authenticator app" — change from `asp-page="./EnableAuthenticator"` to
  `asp-page="./ResetAuthenticator"` (this also fixes an existing navigation bug: the reset
  flow should go through the `ResetAuthenticator` confirmation page, not directly to
  `EnableAuthenticator`)

`EnableAuthenticator.OnGetAsync` — calls `GetAuthenticatorSetupAsync`; if `null`, the key
has not been provisioned and returns `NotFound()`.

`EnableAuthenticator.OnPostAsync` — unchanged; calls `EnableTwoFactorAsync`.

### Fix `ConfirmEmail` page

- `OnGet` — validates params, binds `UserId` and `Code` as `[BindProperty]`, renders
  confirmation page
- `OnPostAsync` — calls `ConfirmEmailAsync`

### Fix `ForgotPassword` page

Inject `IPasswordCommandService`. Call `GeneratePasswordResetTokenAsync` from there.
Check `result.Succeeded` instead of null check. The page still redirects to
`ForgotPasswordConfirmation` when the result is unsuccessful to prevent user enumeration.

### Fix `RegisterConfirmation` page

Inject `IRegistrationCommandService` alongside `IUserQueryService`. Call
`GenerateEmailConfirmationTokenAsync` from `IRegistrationCommandService` (dev mode only).
Call `FindUserByEmail` from `IUserQueryService`.

### Fix `Manage/Index` page

Remove `IAuthenticationCommandService` injection. Remove the `authService.RefreshSignInAsync()`
call — it is a no-op after a phone number update.

### Fix `Admin/Users/Edit` page

Inject `IRegistrationCommandService` for confirmation token generation. Rename
`OnPostGenerateConfirmationLinkAsync` to `OnGetGenerateConfirmationLinkAsync`. Replace the
form with an `<a>` tag using `asp-page-handler` and `asp-route-userId`.

### Create `ApplicationSeedCommandService`

New class injected into `ConfigurationApplicationInitializer` replacing the existing
`IUserCommandService` and `IClientCommandService` injections there.

---

## DTO Reorganisation

All files listed are the complete target state. Delete any existing files not in this list.

### Auth domain — `Dtos/Auth/`

#### `AuthenticationCommands.cs`

```
PasswordSignInCommand(string Email, string Password, bool RememberMe)

PasswordSignInResult(bool Succeeded, bool IsLockedOut, bool RequiresTwoFactor, string? Error)
  static Success()
  static LockedOut()
  static TwoFactorRequired()
  static Failure(string error)
```

#### `PasswordCommands.cs`

```
ChangePasswordCommand(string UserId, string OldPassword, string NewPassword)

ResetPasswordCommand(string Email, string Code, string NewPassword)

GeneratePasswordResetTokenCommand(string Email)

GeneratePasswordResetTokenResult(bool Succeeded, string? Token, string? Error = null)
  static Success(string token)
  static Failed()   // user not found or email unconfirmed — no error exposed to prevent enumeration
```

#### `TwoFactorCommands.cs`

```
TwoFactorSignInCommand(string Code, bool RememberMe, bool RememberClient)

TwoFactorSignInResult(bool Succeeded, bool IsLockedOut, bool IsNotAllowed, string? Error)
  static Success()
  static LockedOut()
  static NotAllowed()
  static Failure(string error)

RecoveryCodeSignInCommand(string Code)

RecoveryCodeSignInResult(bool Succeeded, bool IsLockedOut, bool IsNotAllowed, string? Error)
  static Success()
  static LockedOut()
  static NotAllowed()
  static Failure(string error)

ProvisionAuthenticatorKeyCommand(string UserId)

EnableTwoFactorCommand(string UserId, string VerificationCode)

EnableTwoFactorResult(bool Succeeded, IEnumerable<string> RecoveryCodes, string? Error = null)
  static Success(IEnumerable<string> codes)
  static Failure(string error)

DisableTwoFactorCommand(string UserId)

ResetAuthenticatorKeyCommand(string UserId)

GenerateRecoveryCodesCommand(string UserId)

GenerateRecoveryCodesResult(bool Succeeded, IEnumerable<string> Codes, string? Error = null)
  static Success(IEnumerable<string> codes)
  static Failure(string error)
```

#### `TwoFactorQueries.cs`

```
GetTwoFactorInfoQuery(string UserId)
GetAuthenticatorSetupQuery(string UserId)
```

#### `TwoFactorDtos.cs`

```
TwoFactorInfo(bool IsTwoFactorEnabled, bool HasAuthenticator, int RecoveryCodesLeft, bool IsMachineRemembered)
AuthenticatorSetupInfo(string SharedKey, string AuthenticatorUri)
```

---

### Users domain — `Dtos/Users/`

#### `UserCommands.cs`

```
ToggleUserAdminCommand(string TargetUserId, string? CurrentUserId)
ToggleUserLockCommand(string TargetUserId, string? CurrentUserId)
DeleteUserCommand(string TargetUserId, string? CurrentUserId)
UpdatePhoneNumberCommand(string UserId, string? PhoneNumber)
```

#### `UserRegistrationCommands.cs`

```
RegisterUserCommand(string Email, string Password)

RegisterUserResult(bool Succeeded, string? UserId, string? EmailConfirmationToken, IReadOnlyList<string> Errors)
  static Success(string userId, string emailConfirmationToken)
  static Failure(IEnumerable<string> errors)

ConfirmEmailCommand(string UserId, string EncodedToken)

GenerateEmailConfirmationTokenCommand(string UserId)

GenerateEmailConfirmationTokenResult(bool Succeeded, string? Token, string? Error = null)
  static Success(string token)
  static NotFound()
```

#### `UserSeedCommands.cs`

```
CreateRoleIfMissingCommand(string RoleName)
CreateUserIfMissingCommand(string Email, string? Password, bool EmailConfirmed = true)
AddUserToRoleIfMissingCommand(string UserId, string RoleName)
```

#### `UserQueries.cs`

```
ListUsersQuery(string? SearchTerm = null, int Skip = 0, int? Take = null)
FindUserByEmailQuery(string Email)
GetUserProfileQuery(string UserId)
```

#### `UserDtos.cs`

```
UserSummaryDto(string UserId, string? UserName, string? Email, bool EmailConfirmed, bool IsAdmin, bool IsLockedOut)
UserProfileDto(string? Email, string? PhoneNumber)
FindUserByEmailResult(string? UserId)
```

#### `UserResults.cs`

```
CreateUserIfMissingResult(bool Succeeded, bool Created, string? UserId, string? Error)
  static CreatedNew(string userId)
  static AlreadyExists(string userId)
  static Failure(string error)
```

---

### Clients domain — `Dtos/Clients/`

#### `ClientCommands.cs`

```
CreateClientCommand(string ClientId, string? DisplayName, bool AllowAuthorizationCodeFlow,
  bool AllowClientCredentialsFlow, bool AllowRefreshTokenFlow, bool GenerateClientSecret,
  IReadOnlyList<string> RedirectUris, IReadOnlyList<string> PostLogoutRedirectUris,
  IReadOnlyList<string> AdditionalPermissions)

UpdateClientCommand(string ClientId, string? DisplayName, bool AllowAuthorizationCodeFlow,
  bool AllowClientCredentialsFlow, bool AllowRefreshTokenFlow,
  IReadOnlyList<string> RedirectUris, IReadOnlyList<string> PostLogoutRedirectUris,
  IReadOnlyList<string> AdditionalPermissions)

DeleteClientCommand(string ClientId)

RotateClientSecretCommand(string ClientId)
```

#### `ClientSeedCommands.cs`

```
CreateClientIfMissingCommand(string ClientId, string? ClientSecret, string? DisplayName,
  IReadOnlyList<string> RedirectUris, IReadOnlyList<string> PostLogoutRedirectUris,
  IReadOnlyList<string> AdditionalPermissions)
```

#### `ClientQueries.cs`

```
ListClientsQuery(string? SearchTerm = null, int Skip = 0, int? Take = null)
GetClientByIdQuery(string ClientId)
```

#### `ClientDtos.cs`

```
ClientSummaryDto(string ClientId, string? DisplayName)
ClientDetailsDto(string ClientId, string? DisplayName, bool AllowAuthorizationCodeFlow,
  bool AllowClientCredentialsFlow, bool AllowRefreshTokenFlow,
  IReadOnlyList<string> RedirectUris, IReadOnlyList<string> PostLogoutRedirectUris,
  IReadOnlyList<string> Permissions, bool IsConfidential, bool HasClientSecret)
```

#### `ClientResults.cs`

```
CreateClientResult(bool Succeeded, string? ClientSecret, string? Error)
  static Success(string? clientSecret = null)
  static Failure(string error)

RotateClientSecretResult(bool Succeeded, string? ClientSecret, string? Error, bool PromotedToConfidential)
  static Success(string clientSecret, bool promotedToConfidential = false)
  static Failure(string error)
```

---

### Common — `Dtos/Common/`

#### `CommandResults.cs`

```
CommandResult(bool Succeeded, string? Message, string? Error)
  static Success(string? message = null)
  static Failure(string error)

CreateIfMissingResult(bool Succeeded, bool Created, string? Error)
  static CreatedNew()
  static AlreadyExists()
  static Failure(string error)
```

---

## Acceptance Criteria

### Service interfaces

- `IAuthenticationCommandService`, `IPasswordCommandService`, `IRegistrationCommandService`
  are the names — no `User` prefix
- `RefreshSignInAsync` does not exist on any public interface
- `IAuthenticationCommandService` has exactly: `PasswordSignIn`, `TwoFactorSignIn`,
  `RecoveryCodeSignIn`, `SignOut`
- `IPasswordCommandService` has exactly: `GeneratePasswordResetToken`, `ResetPassword`,
  `ChangePassword`
- `IRegistrationCommandService` has exactly: `RegisterUser`, `ConfirmEmail`,
  `GenerateEmailConfirmationToken`
- `ITwoFactorCommandService` has exactly: `ProvisionAuthenticatorKey`, `EnableTwoFactor`,
  `DisableTwoFactor`, `ResetAuthenticatorKey`, `GenerateRecoveryCodes`
- `IUserQueryService` has exactly: `GetUserProfile`, `ListUsers`, `FindUserByEmail`
- `IUserCommandService` has exactly: `UpdatePhoneNumber`, `ToggleUserAdmin`,
  `ToggleUserLock`, `DeleteUser`
- `IApplicationSeedCommandService` contains all four seeding operations

### Implementations

- `UserAccountService` does not exist; replaced by `AuthenticationCommandService` and
  `PasswordCommandService`
- `TwoFactorService` does not exist; replaced by `TwoFactorCommandService` and
  `TwoFactorQueryService`
- `UserRegistrationService` does not exist; replaced by `RegistrationCommandService`
- `ApplicationSeedCommandService` exists; `ConfigurationApplicationInitializer` injects it

### CQRS correctness

- `GetAuthenticatorSetupAsync` contains no writes; returns `null` if no key is provisioned
- `ProvisionAuthenticatorKeyAsync` is idempotent — no-op if a key already exists
- `ConfirmEmailAsync` is only called from `OnPostAsync` on the `ConfirmEmail` page
- No `Generate*` methods exist on any query interface
- No service interface method returns a raw primitive (`string?`, `bool`)

### Page wiring

- `TwoFactorAuthentication` page: "Enable authenticator app" is a form POST to
  `ProvisionAuthenticatorKeyAsync` then redirect; "Reset authenticator app" links to
  `ResetAuthenticator` page
- `Manage/Index` does not inject `IAuthenticationCommandService`
- `ForgotPassword` injects `IPasswordCommandService`
- `RegisterConfirmation` injects `IRegistrationCommandService` for token generation
- `Admin/Users/Edit` uses `OnGetGenerateConfirmationLinkAsync` and injects
  `IRegistrationCommandService`

### DTOs

- `TwoFactorSignInResult` and `RecoveryCodeSignInResult` are separate types
- `EnableTwoFactorResult` and `GenerateRecoveryCodesResult` are separate types
- `GeneratePasswordResetTokenResult`, `GenerateEmailConfirmationTokenResult`, and
  `FindUserByEmailResult` exist; no `string?` returns on any interface method
- `GenerateEmailConfirmationTokenQuery` does not exist
- `GeneratePasswordResetTokenQuery` does not exist
- `ChangePasswordCommand`, `ResetPasswordCommand`, `GeneratePasswordResetTokenCommand`
  live in `Dtos/Auth/PasswordCommands.cs`
- `Common/CommandResults.cs` contains only `CommandResult` and `CreateIfMissingResult`
- All domain-specific result types live in their domain `Dtos/` folder

### Tests

- All existing tests pass
- Tests renamed/updated to match new class names (`RegistrationCommandService`,
  `AuthenticationCommandService`, `PasswordCommandService`, `TwoFactorCommandService`,
  `TwoFactorQueryService`)
- New tests cover `ProvisionAuthenticatorKeyAsync` (both the provision and no-op paths)
- `GeneratePasswordResetTokenAsync` tests live in `PasswordCommandServiceTests`
- `GenerateEmailConfirmationTokenAsync` tests live in `RegistrationCommandServiceTests`
- `dotnet build` and `dotnet test` pass clean

## Dependencies

Issue #16 is superseded by this issue. Implement from the current state of the codebase.

## Sub-issues

This issue is implemented as three independent slices:

- **17a** — Auth, Password & Registration
- **17b** — Two-Factor Authentication (can run in parallel with 17a)
- **17c** — User, Client & Seed + DTO Cleanup (depends on 17a)
