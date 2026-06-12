# 04 — Email Provider Integration

## Context

`IEmailSender<ApplicationUser>` is currently wired to `NoOpEmailSender`, which silently
discards all emails. This means email confirmation links and password reset links are never
delivered in any non-development environment. Registration and password recovery are
effectively broken outside of local dev.

## User Story

As a user, I want to receive an email confirmation link when I register and a password reset
link when I request one, so that I can verify my account and recover access.

## Acceptance Criteria

- A real email provider is integrated and active in non-development environments
- `SendConfirmationLinkAsync` sends an email containing the confirmation URL
- `SendPasswordResetLinkAsync` sends an email containing the reset URL
- Provider credentials are loaded from configuration (not hardcoded)
- In development, the existing on-page link display behaviour is preserved (no emails sent)
- Sending failures are logged but do not crash the request
- The implementation lives in `See.Idp.Infrastructure` and is registered via DI

## Technical Notes

- Recommended provider: [Resend](https://resend.com) — free tier covers 3,000 emails/month,
  has a clean REST API, and an official .NET SDK (`Resend.Client`)
- Alternative: SMTP via `MailKit` (works with any SMTP relay including Gmail)
- Implement `IEmailSender<ApplicationUser>` in `See.Idp.Infrastructure/Services/EmailSender.cs`
- Add a config section `Email` with `ApiKey` and `FromAddress`
- Register conditionally in `Program.cs`:
  - Development → keep `NoOpEmailSender`
  - Production → real sender
- Email templates can be plain text or simple HTML — no need for a templating engine yet

## Dependencies

None — can be implemented independently of the auth controller work.

## Implementation Notes

- Added `EmailOptions` configuration model in `See.Idp.Core/Configuration/EmailOptions.cs`
- Added `Resend` v0.5.1 NuGet package to `See.Idp.Infrastructure`
- Implemented `ResendEmailSender` in `See.Idp.Infrastructure/Services/ResendEmailSender.cs` using `IResend` from the Resend SDK
- Conditionally registered in `Program.cs`: `NoOpEmailSender` in Development, `ResendEmailSender` + `AddResend` in all other environments
- Added `Email` section placeholder (`ApiKey`, `FromAddress`) to `appsettings.json`
- All three send methods catch exceptions, log them, and do not rethrow
- Tests in `tests/See.Idp.Tests/Services/ResendEmailSenderTests.cs` cover happy path for all three send methods and exception swallowing
