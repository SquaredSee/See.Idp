# 26 — ResendEmailSender silently swallows delivery failures

## Context

`ResendEmailSender` catches `Exception` in all three send methods and logs at `Error` before
returning normally:

```csharp
// src/See.Idp.Infrastructure/Services/ResendEmailSender.cs:34
catch (Exception ex)
{
    logger.LogError(ex, "Failed to send confirmation email to {Email}", email);
}
```

Because `IEmailSender<T>` returns `Task` (no result type), the callers —
`Register.cshtml.cs` and `ForgotPassword.cshtml.cs` — have no signal that delivery failed.
Both pages proceed to redirect as if the email was sent:

- A user who registers during a Resend outage is redirected to "Check your email" and
  waits forever. Their account is confirmed only if they happen to retry later when the
  outage is resolved — or never, if the token expires.
- A user requesting a password reset during an outage is silently told to check their
  email, cannot reset their password, and has no recourse.

For an IdP whose account flows gate access behind email confirmation, a transient third-party
outage silently breaks registration and password reset for all affected users with no retry,
no user feedback, and no alert beyond a single log line.

## Fix

Remove the `catch` blocks in `ResendEmailSender`. Let delivery exceptions propagate to the
caller. The Razor Pages own the error handling:

**`Register.cshtml.cs`**

Wrap the `emailSender.SendConfirmationLinkAsync` call:

```csharp
try
{
    await emailSender.SendConfirmationLinkAsync(...);
}
catch (Exception)
{
    // Delivery failed; the TempData confirmation URL is still set above,
    // so development users can confirm manually. In production, surface an error.
    ModelState.AddModelError(string.Empty,
        "Your account was created but we could not send the confirmation email. "
        + "Please contact support.");
    return Page();
}
```

**`ForgotPassword.cshtml.cs`**

Similarly, wrap the `emailSender.SendPasswordResetLinkAsync` call. Because the page
already redirects to `ForgotPasswordConfirmation` to prevent user enumeration, the
catch block should also redirect there rather than surfacing an error — the email
delivery failure is an operator concern, not a user concern:

```csharp
try
{
    await emailSender.SendPasswordResetLinkAsync(...);
}
catch (Exception ex)
{
    logger.LogError(ex, "Failed to send password reset email");
    // Redirect anyway — do not reveal delivery failure to the requester.
}
return RedirectToPage("./ForgotPasswordConfirmation");
```

Add a structured log call at `Critical` level (not just `Error`) in both catch sites so
an alert rule can fire on email delivery failures without trawling logs.

## Acceptance Criteria

- `ResendEmailSender` does not contain any `catch` block
- `Register.OnPostAsync` handles email delivery failure and returns a user-visible error
  rather than redirecting to the confirmation page when delivery fails
- `ForgotPassword.OnPostAsync` handles email delivery failure; the user is still redirected
  to `ForgotPasswordConfirmation` (enumeration protection is preserved)
- Email delivery failures are logged at `Critical` level with a structured `{Email}` field
  in both Razor Page catch sites
- `dotnet build` and `dotnet test` pass clean

## Dependencies

None.
