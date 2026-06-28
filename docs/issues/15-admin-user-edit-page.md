# 15 — Admin: User Detail & Edit Page

## Context

The admin Users area (`Areas/Admin/Pages/Users/Index.cshtml`) lists users with actions for
toggle-admin, toggle-lock, and delete. Three additional service methods are fully
implemented in the service layer but are never called from any admin page:

| Method | Location | Purpose |
|---|---|---|
| `GetUserProfileAsync` | `IUserQueryService` / `UserQueryService` | Returns `UserProfileDto(Email, PhoneNumber)` |
| `UpdatePhoneNumberAsync` | `IUserCommandService` / `UserCommandService` | Sets or clears a user's phone number |
| `GenerateEmailConfirmationTokenAsync` | `IUserQueryService` / `UserQueryService` | Produces a Base64Url-encoded confirmation token |

There is no admin page at `Users/Edit.cshtml` or `Users/Detail.cshtml`. Admins have no
way to view a user's profile details, update their phone number, or manually generate a
confirmation link for a user whose email is unconfirmed.

## User Story

As an admin, I want a detail/edit page for each user so that I can view their profile,
update their phone number, and generate an email confirmation link for accounts that are
still unconfirmed.

## Acceptance Criteria

- `Areas/Admin/Pages/Users/Edit.cshtml[.cs]` exists and is reachable via an **Edit** link
  on the Users index page
- The page displays the user's email address (read-only) and email confirmation status
- The page displays the user's current phone number and allows the admin to update it
  (calls `UpdatePhoneNumberAsync`)
- For users whose email is unconfirmed, the page shows a **Generate confirmation link**
  action that calls `GenerateEmailConfirmationTokenAsync` and displays the resulting
  `/Identity/Account/ConfirmEmail?userId=...&code=...` URL so the admin can copy and send
  it manually
- All mutations follow the existing PRG pattern with `TempData` status messages
- The Edit page is linked from the Users index table (alongside the existing Toggle Admin,
  Toggle Lock, and Delete actions)

## Technical Notes

- Follow the established page model pattern from `Clients/Edit.cshtml.cs`:
  load on GET, POST handlers per action, `RedirectToPage` after mutations.
- `GetUserProfileAsync(new GetUserProfileQuery(userId))` — returns `UserProfileDto?`; 404
  if null.
- `UpdatePhoneNumberAsync(new UpdatePhoneNumberCommand(userId, phoneNumber))` — accepts
  `null` or empty string to clear the phone number.
- `GenerateEmailConfirmationTokenAsync(new GenerateEmailConfirmationTokenQuery(userId))`
  — returns a Base64Url-encoded token; build the full confirmation URL with
  `Url.Page("/Account/ConfirmEmail", new { area = "Identity", userId, code })`.
- The confirmation URL is display-only (copy to clipboard); no email is sent from this page.

## Dependencies

None — all three service methods are fully implemented and just need a Razor page wired to
them.

## Implementation

**Status:** ✅ Done

**Scope:** `UserProfileDto` only carries `Email` and `PhoneNumber` (no `EmailConfirmed`), so
the page shows email (read-only), editable phone number, and a Generate Confirmation Link
button that is always visible rather than conditionally shown.

**Files changed:**
- `src/See.Idp.Web/Areas/Admin/Pages/Users/Edit.cshtml[.cs]` — new page; GET loads profile,
  POST `Save` updates phone number, POST `GenerateConfirmationLink` renders the URL inline
- `src/See.Idp.Web/Areas/Admin/Pages/Users/Index.cshtml` — added Edit link as first action
  in the actions column
