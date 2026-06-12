# 03 — Implicit Consent for All Clients

## Context

This IDP operates on an **admin consent model**: all client applications are registered and
managed by the administrator (via the admin portal or `Initialization` config). Users never
need to approve scopes — the admin's decision to register a client with specific scopes
serves as the approval. This is the same model used by Entra ID in enterprise environments,
where IT pre-approves apps and users just log in.

The original issue proposed a user-facing consent screen. That model is appropriate for
public platforms (Google, GitHub OAuth apps) where arbitrary third parties can request
access to user accounts. It is not appropriate here.

The current code already auto-approves in the `AuthorizationController`, but OpenIddict
client records have no `ConsentType` set — defaulting to `explicit` internally. This should
be made explicit and correct.

## User Story

As the IDP administrator, I want all registered client applications to be pre-approved so
that users are never interrupted by a consent screen when logging in.

## Acceptance Criteria

- All clients are created with `ConsentType = ConsentTypes.Implicit`
- Existing clients in the database are updated to `implicit` consent on next startup
- The `AuthorizationController` auto-approve path is clean and intentional
- No consent page exists or is needed

## Technical Notes

- Set `ConsentType = OpenIddictConstants.ConsentTypes.Implicit` on the
  `OpenIddictApplicationDescriptor` in `TryConfigureClient` inside `ClientCommandService`
- The `CreateClientIfMissingCommand` path (used by `ConfigurationApplicationInitializer`)
  also goes through `ClientCommandService` — both paths are covered by fixing `TryConfigureClient`
- The `UpdateClientAsync` path should also set `ConsentType` so existing clients are corrected
  on next update

## Dependencies

- `01-authorization-controller` — the controller's auto-approve path relies on this being correct

## Implementation

**Completed.** Commit: `feat(clients): set implicit consent type on all clients`

### Files changed

- `src/See.Idp.Infrastructure/Services/ClientCommandService.cs` — added
  `descriptor.ConsentType = OpenIddictConstants.ConsentTypes.Implicit` in `TryConfigureClient`,
  covering both create and update paths
