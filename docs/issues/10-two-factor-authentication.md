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
