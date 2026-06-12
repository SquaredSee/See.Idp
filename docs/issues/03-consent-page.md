# 03 — Consent Page

## Context

The OAuth 2.0 authorization code flow requires a consent step where the user explicitly
approves the scopes a client application is requesting. Without a consent page, OpenIddict
cannot complete the authorization flow for clients whose `consent_type` is not set to
`implicit`. This blocks any third-party or multi-application scenario.

## User Story

As a user, when an application requests access to my account, I want to see what permissions
it is asking for and be able to approve or deny the request, so that I remain in control of
what data I share.

## Acceptance Criteria

- A consent page is shown during the authorization code flow when a new authorization is required
- The page displays the requesting application's display name and the list of requested scopes
- The user can approve or deny the request
- Approving records an authorization in the OpenIddict store and redirects back to the client
  with an authorization code
- Denying redirects back to the client with an `access_denied` error
- Previously approved authorizations are remembered — user is not prompted again on repeat
  visits for the same client + scope combination
- The consent page is styled consistently with the rest of the Identity UI

## Technical Notes

- The authorization controller (issue 01) checks for an existing authorization via
  `IOpenIddictAuthorizationManager.FindAsync(...)` before redirecting to consent
- If no existing authorization is found, redirect to a Razor Page at
  `Areas/Identity/Pages/Connect/Authorize.cshtml` (or similar)
- The Razor Page posts back to `connect/authorize` with the user's decision
- Use `OpenIddictConstants.ConsentTypes` and `OpenIddictConstants.AuthorizationTypes`
- Store the approved authorization with `IOpenIddictAuthorizationManager.CreateAsync(...)`
- Scope display names should be human-readable (e.g. "email" → "Access your email address")

## Dependencies

- `01-authorization-controller` — the controller orchestrates the consent redirect
