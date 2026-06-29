# 16 — CQRS Cleanup

## Context

An audit of the CQRS implementation identified three categories of problems: actual
violations of the command/query segregation rule in Razor Pages, a misplaced method on the
wrong service interface, and inconsistencies in how DTO files are organised across domains.

---

## Issues

### 1. `ConfirmEmail.OnGetAsync` calls a state-mutating command

`ConfirmEmailAsync` writes to the database (calls `userManager.ConfirmEmailAsync`). It is
invoked from `OnGetAsync` because email confirmation links are clicked in a browser. This
is a CQRS violation and a real-world risk: link prefetchers, crawlers, and back-button
replays can all trigger the side effect.

**Fix:** `OnGetAsync` should validate the parameters and render a "Confirm your email"
page. A form button submits to `OnPostAsync`, which executes `ConfirmEmailAsync`.

---

### 2. `GeneratePasswordResetTokenAsync` is on the wrong interface

`IUserRegistrationCommandService.GeneratePasswordResetTokenAsync` has two problems:

- `UserManager.GeneratePasswordResetTokenAsync` is a pure Data Protection token
  derivation — no DB write. It has no side effects, so it belongs on `IUserQueryService`
  alongside `GenerateEmailConfirmationTokenAsync`, which does the same thing for a
  different token type.
- It returns a raw `string?` instead of a typed result record, inconsistent with every
  other method in the codebase.

**Fix:**

- Add `GeneratePasswordResetTokenQuery(string Email)` to `UserQueries.cs`
- Add `GeneratePasswordResetTokenAsync(GeneratePasswordResetTokenQuery, CancellationToken)`
  to `IUserQueryService`
- Move the implementation from `UserRegistrationService` to `UserQueryService`
- Remove `GeneratePasswordResetTokenCommand` and the method from
  `IUserRegistrationCommandService` / `UserRegistrationService`
- Update `ForgotPassword.cshtml.cs` to inject `IUserQueryService` instead of
  `IUserRegistrationCommandService`
- Move the three `GeneratePasswordResetTokenAsync` tests from
  `UserRegistrationServiceTests` to `UserQueryServiceTests`

---

### 3. `Admin/Users/Edit.OnPostGenerateConfirmationLinkAsync` is a POST that only queries

The handler calls only `userQueryService.GenerateEmailConfirmationTokenAsync` — no command
is issued. A POST that only reads violates the intent of `OnPost`.

**Fix:** Rename to `OnGetGenerateConfirmationLinkAsync`. Replace the `<form method="post">`
in `Edit.cshtml` with an `<a>` tag using `asp-page-handler="GenerateConfirmationLink"` and
`asp-route-userId`.

---

### 4. DTO file organisation inconsistencies

The DTO files are inconsistent across domains in three ways:

**Domain-specific result records in `Common/`**

`CreateClientResult`, `RotateClientSecretResult`, and `CreateUserIfMissingResult` live in
`Common/CommandResults.cs` alongside the genuinely shared `CommandResult` and
`CreateIfMissingResult`. They are domain-specific and should move to their respective
domain folders.

**`UserCommands.cs` / `UserProfileCommands.cs` arbitrary split**

Both files contain command records for `IUserCommandService`. The split between "admin
ops" and "profile ops" maps to no interface or service boundary. Merge into a single
`UserCommands.cs`.

**Response DTOs mixed into query files**

`UserSummaryDto` and `UserProfileDto` live in `UserQueries.cs`. `ClientSummaryDto` and
`ClientDetailsDto` live in `ClientQueries.cs`. `TwoFactorInfo` and `AuthenticatorSetupInfo`
live in `TwoFactorQueries.cs`. These are output types, not input types, and should be
separated into their own files.

**Fix:**

- Move `CreateClientResult` and `RotateClientSecretResult` to `Dtos/Clients/`
- Move `CreateUserIfMissingResult` to `Dtos/Users/`
- Merge `UserProfileCommands.cs` into `UserCommands.cs`; delete the old file
- Extract response DTOs out of query files:
    - `UserSummaryDto`, `UserProfileDto` → `Dtos/Users/UserDtos.cs`
    - `ClientSummaryDto`, `ClientDetailsDto` → `Dtos/Clients/ClientDtos.cs`
    - `TwoFactorInfo`, `AuthenticatorSetupInfo` → `Dtos/Auth/TwoFactorDtos.cs`

---

## Acceptance Criteria

- `ConfirmEmail` renders a confirmation page on GET; `ConfirmEmailAsync` is called from
  `OnPostAsync` only
- `GeneratePasswordResetTokenAsync` lives on `IUserQueryService`; `ForgotPassword` injects
  `IUserQueryService`; no trace of the method remains on `IUserRegistrationCommandService`
- `OnPostGenerateConfirmationLinkAsync` is replaced by `OnGetGenerateConfirmationLinkAsync`;
  the cshtml uses a link, not a form
- `Common/CommandResults.cs` contains only `CommandResult` and `CreateIfMissingResult`
- Domain-specific result records live in their domain `Dtos/` folder
- `UserProfileCommands.cs` is deleted; its records are in `UserCommands.cs`
- Response DTOs are in their own `*Dtos.cs` files, not mixed into query files
- All existing tests pass; moved tests are in the correct test class
- `dotnet build` and `dotnet test` pass clean

## Dependencies

None.
