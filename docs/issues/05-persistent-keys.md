# 05 — Persistent Signing & Encryption Keys

## Context

OpenIddict is currently configured with `AddDevelopmentEncryptionCertificate()` and
`AddDevelopmentSigningCertificate()` in all environments. These generate ephemeral in-memory
certificates on every startup. In production this means every restart invalidates all
previously issued tokens and breaks active user sessions. The production branch is a
`// TODO` comment.

## User Story

As a user, I want my session to survive a server restart so that I am not unexpectedly
logged out of applications.

## Acceptance Criteria

- Development continues to use ephemeral development certificates (no change)
- Production uses certificates that persist across restarts
- Certificates are loaded from configuration, not hardcoded in source
- The solution works in a containerized/Kubernetes environment (no reliance on the Windows
  certificate store)
- Certificate rotation can be performed without downtime

## Technical Notes

- Recommended approach: **EF Core key storage** via OpenIddict's built-in support
  - Keys are stored in the database alongside OpenIddict's other tables
  - No external secret store needed initially
- Alternative for later: load a PFX from a Kubernetes Secret mounted as a file, using
  `X509Certificate2` loaded from a configured path
- Remove the production `TODO` block and replace with real configuration
- Add an `appsettings.Production.json` section for certificate configuration

## Dependencies

- `01-authorization-controller` — validate tokens work correctly before locking in key strategy
