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

## Implementation

**Status:** ✅ Done

**Files changed:**
- `src/See.Idp.Web/Program.cs` — replaced the `// TODO` production block with RSA key loading
  from config. Two 2048-bit RSA keys are expected: `OpenIddict:SigningKey` (for JWT signing)
  and `OpenIddict:EncryptionKey` (for access-token encryption). Each is an XML-serialised RSA
  key string (output of `RSA.ToXmlString(includePrivateParameters: true)`). Both throw
  `InvalidOperationException` at startup if missing in production, with instructions to set
  the corresponding `OPENIDDICT__SIGNINGKEY` / `OPENIDDICT__ENCRYPTIONKEY` environment
  variables.
- `src/See.Idp.Web/appsettings.Production.json` — new; documents the required production
  keys with placeholder values pointing to the env-var override pattern.

**Key generation (one-time setup):**
```csharp
using var rsa = RSA.Create(2048);
Console.WriteLine(rsa.ToXmlString(includePrivateParameters: true));
```
Run this once per key type, store the output in a Kubernetes Secret or secrets manager,
and expose via environment variables (`OPENIDDICT__SIGNINGKEY`, `OPENIDDICT__ENCRYPTIONKEY`).
