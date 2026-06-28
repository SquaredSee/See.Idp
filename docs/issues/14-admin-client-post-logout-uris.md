# 14 — Admin: Client PostLogoutRedirectUris Support

## Context

`CreateClientIfMissingCommand` (the seeding path) accepts `PostLogoutRedirectUris` and
correctly writes them to OpenIddict. However, `CreateClientCommand`, `UpdateClientCommand`,
and `ClientDetailsDto` all omit this field entirely.

As a result:

- Clients created through the admin UI can never have post-logout redirect URIs set.
- Admins cannot view a client's existing post-logout redirect URIs.
- Admins cannot update them — `TryConfigureClient` doesn't touch
  `descriptor.PostLogoutRedirectUris`, so `UpdateClientAsync` silently preserves whatever
  value is already in the database but provides no way to change or inspect it.

The `see-client-web` client (seeded in `appsettings.Development.json`) has
`PostLogoutRedirectUris: ["http://localhost:5050/"]`. These can't be managed through the
admin UI at all.

## User Story

As an admin, I want to view and set post-logout redirect URIs when creating or editing a
client, so that the end-session (logout) flow correctly redirects users after sign-out.

## Acceptance Criteria

- `CreateClientCommand` includes a `PostLogoutRedirectUris` field
- `UpdateClientCommand` includes a `PostLogoutRedirectUris` field
- `ClientDetailsDto` includes a `PostLogoutRedirectUris` field
- `ClientCommandService.CreateClientAsync` passes the URIs to the OpenIddict descriptor
- `ClientCommandService.UpdateClientAsync` replaces (not merges) the existing
  `PostLogoutRedirectUris` with the values from the command
- `ClientQueryService.GetClientByIdAsync` populates `PostLogoutRedirectUris` in the
  returned `ClientDetailsDto`
- The admin **Create** page exposes a post-logout redirect URIs input (same style as the
  existing redirect URIs textarea)
- The admin **Edit** page exposes a post-logout redirect URIs input and pre-fills it from
  `ClientDetailsDto`
- URI format is validated the same way redirect URIs are

## Technical Notes

- Follow the exact pattern used for `RedirectUris` throughout the stack — the same
  parse/validate helper (`TryParseRedirectUris`) can be reused; add an analogous
  `TryConfigurePostLogoutUris` call inside `TryConfigureClient`, or inline the same logic.
- `TryConfigureClient` currently calls `descriptor.RedirectUris.Clear()` before repopulating;
  do the same for `descriptor.PostLogoutRedirectUris` in `UpdateClientAsync` to ensure a
  clean replace (no stale URIs survive an edit).
- The `Create.cshtml` and `Edit.cshtml` pages already have a `RedirectUris` textarea using a
  newline-delimited format — use the same pattern for `PostLogoutRedirectUris`.

## Dependencies

None — all service and infrastructure code is already in place; this is a gap in the
command/query DTOs and the two admin Razor pages.

## Implementation

**Status:** ✅ Done

**Files changed:**
- `src/See.Idp.Core/Dtos/Clients/ClientCommands.cs` — added `PostLogoutRedirectUris` to `CreateClientCommand` and `UpdateClientCommand`
- `src/See.Idp.Core/Dtos/Clients/ClientQueries.cs` — added `PostLogoutRedirectUris` to `ClientDetailsDto`
- `src/See.Idp.Infrastructure/Services/ClientCommandService.cs` — `TryConfigureClient` clears and repopulates `descriptor.PostLogoutRedirectUris`; both `CreateClientAsync` and `UpdateClientAsync` callers updated
- `src/See.Idp.Infrastructure/Services/ClientQueryService.cs` — `GetClientByIdAsync` populates `PostLogoutRedirectUris` from the descriptor
- `src/See.Idp.Web/Areas/Admin/Pages/Clients/Create.cshtml[.cs]` — added `PostLogoutRedirectUrisText` field and textarea
- `src/See.Idp.Web/Areas/Admin/Pages/Clients/Edit.cshtml[.cs]` — added `PostLogoutRedirectUrisText` field, pre-filled from DTO, textarea in form
