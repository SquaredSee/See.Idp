# 32 — EmailConfirmationUrl written to TempData unconditionally in production

## Context

`Register.OnPostAsync` stores the confirmation link in TempData without any environment
guard:

```csharp
// src/See.Idp.Web/Areas/Identity/Pages/Account/Register.cshtml.cs:78
TempData["EmailConfirmationUrl"] = confirmationLink;
return RedirectToPage("./RegisterConfirmation", ...);
```

The confirmation link contains the raw email confirmation token. In production, TempData
is backed by a cookie (the default ASP.NET Core TempData provider). The cookie is
HttpOnly and protected by Data Protection, so it is not directly readable by JavaScript
or a passive observer. However:

- If Data Protection keys are in-memory (the degraded fallback documented in issue #06),
  the token is effectively cleartext across restarts.
- Any code on `RegisterConfirmation.cshtml.cs` that reads `TempData["EmailConfirmationUrl"]`
  without an `IsDevelopment()` guard could display the token in the page HTML, making it
  readable by XSS.
- The intent of this TempData entry — as established in issue #19 — is development
  convenience only. Storing it in production creates unnecessary exposure with no benefit.

Contrast with `ForgotPassword.cshtml.cs`, which correctly gates the development shortcut
behind `env.IsDevelopment()`.

## Fix

Gate the TempData write behind `IsDevelopment()`:

```csharp
// Register.cshtml.cs — inject IWebHostEnvironment env in constructor
if (env.IsDevelopment())
    TempData["EmailConfirmationUrl"] = confirmationLink;

return RedirectToPage("./RegisterConfirmation", new { email = Input.Email, returnUrl });
```

Verify that `RegisterConfirmation.cshtml.cs` only reads and displays
`TempData["EmailConfirmationUrl"]` when `env.IsDevelopment()` is true. If the existing
read is already gated, no change is needed there.

## Acceptance Criteria

- `Register.OnPostAsync` only writes `TempData["EmailConfirmationUrl"]` when
  `IWebHostEnvironment.IsDevelopment()` is true
- `RegisterConfirmation.OnGetAsync` only reads and displays the URL when
  `IsDevelopment()` is true (no change required if already gated)
- Production registration flow is unaffected: users still receive a confirmation email
  and are redirected to the confirmation page
- Development registration flow is unaffected: the on-screen confirmation link still
  appears
- `dotnet build` and `dotnet test` pass clean

## Dependencies

Issue #19 (RegisterConfirmation CQRS violation) should be reviewed alongside this issue;
both touch the same TempData pattern.
