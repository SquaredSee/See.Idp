# 17a — Auth, Password & Registration

Sub-issue of #17. Implement all changes in the Auth, Password, and Registration domains.
See issue #17 for full context, DTO field definitions, and the complete target service
structure.

## Scope

### Interfaces (Core)

- Rename `IUserAuthenticationCommandService` → `IAuthenticationCommandService`
- Rename `IUserPasswordCommandService` → `IPasswordCommandService`
- Rename `IUserRegistrationCommandService` → `IRegistrationCommandService`
- Remove `RefreshSignInAsync` from `IAuthenticationCommandService`
- Add `GeneratePasswordResetToken` to `IPasswordCommandService` returning
  `GeneratePasswordResetTokenResult`
- Remove `GeneratePasswordResetTokenAsync` from `IRegistrationCommandService`
- Add `GenerateEmailConfirmationToken` to `IRegistrationCommandService` returning
  `GenerateEmailConfirmationTokenResult`

### DTOs (Core)

Create/update per the target layout in issue #17:

- `Dtos/Auth/AuthenticationCommands.cs` — `PasswordSignInCommand`, `PasswordSignInResult`
- `Dtos/Auth/PasswordCommands.cs` — new file: `ChangePasswordCommand`,
  `ResetPasswordCommand`, `GeneratePasswordResetTokenCommand`,
  `GeneratePasswordResetTokenResult`
- `Dtos/Users/UserRegistrationCommands.cs` — add `GenerateEmailConfirmationTokenCommand`,
  `GenerateEmailConfirmationTokenResult`; remove `GeneratePasswordResetTokenCommand`
- `Dtos/Users/UserProfileCommands.cs` — delete this file; `ChangePasswordCommand` and
  `ResetPasswordCommand` move to `Dtos/Auth/PasswordCommands.cs`;
  `UpdatePhoneNumberCommand` moves to `Dtos/Users/UserCommands.cs`
- `Dtos/Users/UserQueries.cs` — remove `GenerateEmailConfirmationTokenQuery` and
  `GeneratePasswordResetTokenQuery`

### Implementations (Infrastructure)

- Split `UserAccountService` into two classes:
    - `AuthenticationCommandService` implements `IAuthenticationCommandService`:
      `PasswordSignIn`, `TwoFactorSignIn`, `RecoveryCodeSignIn`, `SignOut`
    - `PasswordCommandService` implements `IPasswordCommandService`:
      `GeneratePasswordResetToken`, `ResetPassword`, `ChangePassword`. Retains
      `SignInManager` for the internal `RefreshSignInAsync` call inside `ChangePassword`.
- Rename `UserRegistrationService` → `RegistrationCommandService`; add
  `GenerateEmailConfirmationTokenAsync` implementation; remove
  `GeneratePasswordResetTokenAsync`
- Delete `UserAccountService.cs`
- Delete `UserRegistrationService.cs`
- Update `Program.cs` DI registrations for all renamed/new classes

### Pages (Web)

- `ConfirmEmail.cshtml.cs` — `OnGet` binds `UserId` and `Code`, renders page;
  `OnPostAsync` calls `ConfirmEmailAsync`
- `ConfirmEmail.cshtml` — add confirmation form with hidden inputs and submit button
- `ForgotPassword.cshtml.cs` — inject `IPasswordCommandService`; call
  `GeneratePasswordResetTokenAsync`; check `result.Succeeded`
- `RegisterConfirmation.cshtml.cs` — inject `IRegistrationCommandService` for
  `GenerateEmailConfirmationTokenAsync`; keep `IUserQueryService` for `FindUserByEmail`
- `Manage/Index.cshtml.cs` — remove `IAuthenticationCommandService` injection; remove
  `authService.RefreshSignInAsync()` call
- `Login.cshtml.cs`, `LoginWith2fa.cshtml.cs`, `LoginWithRecoveryCode.cshtml.cs`,
  `Logout.cshtml.cs`, `Register.cshtml.cs`, `Manage/ChangePassword.cshtml.cs`,
  `ResetPassword.cshtml.cs` — update injected interface name where changed

### Tests

- Delete `UserRegistrationServiceTests.cs`
- Create `RegistrationCommandServiceTests.cs` — all existing registration tests plus
  new tests for `GenerateEmailConfirmationTokenAsync` (success and not-found paths)
- Delete `UserAccountServiceTests.cs` if it exists
- Create `AuthenticationCommandServiceTests.cs`
- Create `PasswordCommandServiceTests.cs` — include tests for
  `GeneratePasswordResetTokenAsync` (success, user-not-found, email-unconfirmed)

## Acceptance Criteria

- `IAuthenticationCommandService`, `IPasswordCommandService`, `IRegistrationCommandService`
  exist with exactly the methods listed in issue #17
- `IAuthenticationCommandService` has no `RefreshSignInAsync`
- `UserAccountService.cs` does not exist
- `UserRegistrationService.cs` does not exist
- `AuthenticationCommandService`, `PasswordCommandService`, `RegistrationCommandService`
  exist as separate classes
- `GeneratePasswordResetTokenAsync` is on `IPasswordCommandService` returning
  `GeneratePasswordResetTokenResult`
- `GenerateEmailConfirmationTokenAsync` is on `IRegistrationCommandService` returning
  `GenerateEmailConfirmationTokenResult`
- No `string?` returns on any of the three interfaces
- `ConfirmEmailAsync` is only called from `OnPostAsync` on `ConfirmEmail`
- `Manage/Index` does not inject `IAuthenticationCommandService`
- `Dtos/Auth/PasswordCommands.cs` exists and contains the password command records
- `UserProfileCommands.cs` does not exist
- `GenerateEmailConfirmationTokenQuery` and `GeneratePasswordResetTokenQuery` do not exist
- `dotnet build` and `dotnet test` pass clean

## Dependencies

None. Implement against current codebase state.
