# 19 — RegisterConfirmation: command invoked from GET handler

## Context

`RegisterConfirmation.OnGetAsync` calls `registrationService.GenerateEmailConfirmationTokenAsync`
inside an `if (env.IsDevelopment())` block, which persists a new confirmation token to the
database and updates the user's security stamp. That is a write operation invoked from an HTTP
GET handler, violating the CQRS convention that `OnGet` handlers call query services only.

The violation only fires in development — in production `IsDevelopment()` is false, the block
is skipped, and no service call is made.

There is also a secondary bug: `Register.OnPostAsync` already generates a token, builds a
confirmation link, and emails it. When `RegisterConfirmation.OnGetAsync` then calls
`GenerateEmailConfirmationTokenAsync` again, ASP.NET Core Identity rotates the security stamp
and invalidates the token that was already emailed. In development, the emailed link is broken
by the time the confirmation page renders.

## Fix

In `Register.OnPostAsync`, store the already-built `confirmationLink` in TempData before
redirecting. In `RegisterConfirmation.OnGetAsync`, retain the `env.IsDevelopment()` display
guard but read the URL from TempData instead of generating a new token. No service call of
any kind in `RegisterConfirmation`.

```csharp
// Register.cshtml.cs — OnPostAsync, after building confirmationLink:
TempData["EmailConfirmationUrl"] = confirmationLink;
return RedirectToPage("./RegisterConfirmation", new { email = Input.Email, returnUrl });

// RegisterConfirmation.cshtml.cs — OnGetAsync:
Email = email;
DisplayConfirmAccountLink = env.IsDevelopment();
if (DisplayConfirmAccountLink)
    EmailConfirmationUrl = TempData["EmailConfirmationUrl"] as string;
// No service call. IRegistrationCommandService injection removed.
```

> **Do not** replace the `IsDevelopment()` guard with TempData presence. Email confirmation
> exists to prove the registrant owns the address. Showing the link unconditionally in
> production would let an attacker register with a victim's email, click the on-screen link,
> and confirm it without ever accessing the victim's inbox.

Production behavior is unchanged: users see "Check your email" with no link and must confirm
via their inbox.

## Acceptance Criteria

- `RegisterConfirmation.OnGetAsync` contains no service calls
- `RegisterConfirmation` does not inject `IRegistrationCommandService`
- `Register.OnPostAsync` stores the confirmation link in TempData before redirecting
- `DisplayConfirmAccountLink` is still gated on `env.IsDevelopment()`; TempData only supplies the URL value when the gate is open
- The confirmation link is displayed correctly in development after a successful registration
- The emailed link and the on-screen link are the same token (no second token generated)
- `dotnet build` and `dotnet test` pass clean
