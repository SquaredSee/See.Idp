# 28 — Web layer constructs Infrastructure entities directly

## Context

Two Razor Pages construct `ApplicationUser` — an EF Core Identity entity defined in
`See.Idp.Infrastructure` — to satisfy the `IEmailSender<ApplicationUser>` parameter type:

```csharp
// src/See.Idp.Web/Areas/Identity/Pages/Account/Register.cshtml.cs:76
await emailSender.SendConfirmationLinkAsync(
    new ApplicationUser { Email = Input.Email, UserName = Input.Email },
    Input.Email,
    HtmlEncoder.Default.Encode(confirmationLink)
);

// src/See.Idp.Web/Areas/Identity/Pages/Account/ForgotPassword.cshtml.cs:64
await emailSender.SendPasswordResetLinkAsync(
    new ApplicationUser { Email = Input.Email, UserName = Input.Email },
    Input.Email,
    HtmlEncoder.Default.Encode(resetUrl)
);
```

Neither implementation (`NoOpEmailSender`, `ResendEmailSender`) uses the `user` parameter
for anything — both only read the `email` string. The `ApplicationUser` instance exists
solely to satisfy the generic constraint.

This is a compile-time dependency from the Web project onto the Infrastructure entity. The
`using See.Idp.Infrastructure` import in both files is required only by these two lines.
If `ApplicationUser` ever requires constructor arguments or a mandatory navigation property,
both Razor Pages break at compile time for reasons unrelated to their domain.

## Fix

Introduce a thin facade in Core that does not mention `ApplicationUser`:

```csharp
// See.Idp.Core/Services/Auth/IRegistrationEmailService.cs
public interface IRegistrationEmailService
{
    Task SendConfirmationLinkAsync(
        string email,
        string confirmationLink,
        CancellationToken ct = default
    );

    Task SendPasswordResetLinkAsync(
        string email,
        string resetLink,
        CancellationToken ct = default
    );
}
```

Implement it in Infrastructure, delegating to the existing `IEmailSender<ApplicationUser>`:

```csharp
// See.Idp.Infrastructure/Services/RegistrationEmailService.cs
public sealed class RegistrationEmailService(
    IEmailSender<ApplicationUser> emailSender
) : IRegistrationEmailService
{
    public Task SendConfirmationLinkAsync(string email, string confirmationLink, ...)
        => emailSender.SendConfirmationLinkAsync(
            new ApplicationUser { Email = email, UserName = email },
            email,
            confirmationLink,
            ct
        );

    public Task SendPasswordResetLinkAsync(string email, string resetLink, ...)
        => emailSender.SendPasswordResetLinkAsync(
            new ApplicationUser { Email = email, UserName = email },
            email,
            resetLink,
            ct
        );
}
```

Replace `IEmailSender<ApplicationUser>` injections in `Register.cshtml.cs` and
`ForgotPassword.cshtml.cs` with `IRegistrationEmailService`. Remove
`using See.Idp.Infrastructure` from both files.

Register `IRegistrationEmailService → RegistrationEmailService` as `Scoped` in
`Program.cs`.

## Acceptance Criteria

- `Register.cshtml.cs` and `ForgotPassword.cshtml.cs` do not contain
  `using See.Idp.Infrastructure`
- Neither Razor Page constructs an `ApplicationUser` instance
- `IRegistrationEmailService` is defined in `See.Idp.Core/Services/Auth/`
- `RegistrationEmailService` is implemented in `See.Idp.Infrastructure/Services/`
- Confirmation email and password-reset email are still sent correctly in both
  development (NoOp) and production (Resend) paths
- `dotnet build` and `dotnet test` pass clean

## Dependencies

Issue #26 (email sender failure handling) should be implemented alongside this issue,
as both touch the same email-sending call sites.
