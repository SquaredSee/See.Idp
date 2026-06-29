# 10 — Two-Factor Authentication (TOTP)

## Context

No MFA support exists. For an IDP that gates access to multiple applications, 2FA is not
optional — a compromised IDP account compromises every downstream application. ASP.NET Core
Identity has full TOTP support built in; the pages just need to be scaffolded and wired.

## User Story

As a user, I want to secure my account with an authenticator app so that my account
cannot be accessed with a password alone.

## Acceptance Criteria

- Users can enrol an authenticator app (Google Authenticator, Authy, 1Password, etc.)
  from the account management area
- The enrolment page displays a QR code and a manual entry key
- Users can disable 2FA from the management area
- Login prompts for a TOTP code when 2FA is enabled for the account
- Recovery codes are generated at enrolment and can be regenerated
- Logging in with a recovery code works when the authenticator app is unavailable
- The 2FA manage page shows whether 2FA is currently enabled

## Technical Notes

- Scaffold the following Identity pages:
    - `Areas/Identity/Pages/Account/Manage/EnableAuthenticator.cshtml`
    - `Areas/Identity/Pages/Account/Manage/DisableTwoFactorAuthentication.cshtml`
    - `Areas/Identity/Pages/Account/Manage/TwoFactorAuthentication.cshtml`
    - `Areas/Identity/Pages/Account/Manage/GenerateRecoveryCodes.cshtml`
    - `Areas/Identity/Pages/Account/Manage/ResetAuthenticator.cshtml`
    - `Areas/Identity/Pages/Account/LoginWith2fa.cshtml`
    - `Areas/Identity/Pages/Account/LoginWithRecoveryCode.cshtml`
- QR code generation requires a JavaScript QR library (e.g. `qrcodejs` via CDN,
  or server-side via `QRCoder` NuGet)
- `UserManager.GetTwoFactorEnabledAsync`, `VerifyTwoFactorTokenAsync`, etc. are already
  available via the existing `UserManager<ApplicationUser>` dependency
- Wire the 2FA manage page link into `Areas/Identity/Pages/Account/Manage/Index.cshtml`

## Dependencies

- `04-email-provider` — account recovery emails need to work before 2FA enrolment
  is safe (users could otherwise lock themselves out)

## Implementation

**Status:** ✅ Done

**New Core files:**

- `Dtos/Auth/TwoFactorCommands.cs` — `TwoFactorSignInCommand`, `RecoveryCodeSignInCommand`,
  `TwoFactorSignInResult`, `EnableTwoFactorCommand`, `DisableTwoFactorCommand`,
  `ResetAuthenticatorKeyCommand`, `GenerateRecoveryCodesCommand`, `GenerateRecoveryCodesResult`.
- `Dtos/Auth/TwoFactorQueries.cs` — `GetTwoFactorInfoQuery`, `TwoFactorInfo`,
  `GetAuthenticatorSetupQuery`, `AuthenticatorSetupInfo`.
- `Services/Auth/ITwoFactorCommandService.cs` — Enable, Disable, ResetKey, GenerateCodes.
- `Services/Auth/ITwoFactorQueryService.cs` — GetTwoFactorInfo, GetAuthenticatorSetup.

**Updated Core files:**

- `Dtos/Auth/AuthenticationCommands.cs` — added `RequiresTwoFactor` to `PasswordSignInResult`.
- `Services/Auth/IUserAuthenticationCommandService.cs` — added `TwoFactorSignInAsync` and
  `RecoveryCodeSignInAsync`.

**New Infrastructure files:**

- `Services/TwoFactorService.cs` — implements both interfaces; uses `UserManager` for all key/code
  operations; `FormatKey` groups base32 key in 4-char chunks; `GenerateQrCodeUri` produces
  standard `otpauth://totp/` URI; QR code rendered client-side via `qrcodejs` CDN.

**Updated Infrastructure files:**

- `Services/UserAccountService.cs` — `PasswordSignInAsync` now returns `TwoFactorRequired()` when
  `SignInResult.RequiresTwoFactor`; added `TwoFactorSignInAsync` and `RecoveryCodeSignInAsync`.
- `Logging/EventIds.cs` — event IDs 1711–1719 for 2FA sign-in and management events.

**New Web files:**

- `Account/LoginWith2fa.cshtml[.cs]` — TOTP code entry; passes `RememberMe` + `RememberClient`.
- `Account/LoginWithRecoveryCode.cshtml[.cs]` — recovery code sign-in.
- `Account/Manage/TwoFactorAuthentication.cshtml[.cs]` — 2FA status dashboard.
- `Account/Manage/EnableAuthenticator.cshtml[.cs]` — QR code setup + code verification.
- `Account/Manage/DisableTwoFactorAuthentication.cshtml[.cs]` — disable confirmation.
- `Account/Manage/GenerateRecoveryCodes.cshtml[.cs]` — generate confirmation.
- `Account/Manage/ShowRecoveryCodes.cshtml[.cs]` — one-time display of new codes.
- `Account/Manage/ResetAuthenticator.cshtml[.cs]` — reset key confirmation.
- `Account/Manage/_ManageNav.cshtml` — added Two-factor authentication link.

**Updated Web files:**

- `Account/Login.cshtml.cs` — redirects to `LoginWith2fa` when `RequiresTwoFactor`.
- `Program.cs` — registers `TwoFactorService` as both `ITwoFactorCommandService` and
  `ITwoFactorQueryService`.

**New tests:** `TwoFactorServiceTests.cs` (10 tests), `UserAccountServiceTests.cs` (5 tests);
`IdentityTestFactory` extended with `CreateSignInManager`.
