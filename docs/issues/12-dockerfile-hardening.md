# 12 — Dockerfile Hardening

## Context

The existing `Dockerfile` is a functional multi-stage build but is missing a health check,
has no mechanism for injecting secrets at runtime, and has not been validated against a
correct build context. These gaps will cause problems in a Kubernetes deployment.

## User Story

As the operator, I want the IDP container to report its own health so that Kubernetes
can route traffic only to ready instances and restart unhealthy ones automatically.

## Acceptance Criteria

- `HEALTHCHECK` is defined in the Dockerfile using the ASP.NET Core health endpoint
- A `/health` endpoint is registered in the application and returns `200 OK` when healthy
- The container starts successfully from a clean `docker build` + `docker run`
- Secrets (connection string, cert paths, API keys) are injected via environment variables
  and not baked into the image
- Required environment variables are documented
- `docker-compose.yml` includes a `see-idp-web` service so `docker-compose up` starts the
  full local stack (IDP + client + postgres + redis + otel); resolves the broken
  `Oidc__Authority=http://see-idp-web:8080` reference in the `client-web` service

## Technical Notes

- Add `app.MapHealthChecks("/health")` in `Program.cs` (requires `AddHealthChecks()` in DI)
- Add optional database health check via `AddDbContextCheck<ApplicationDbContext>()`
  (NuGet: `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`)
- Dockerfile `HEALTHCHECK`:
  ```dockerfile
  HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1
  ```
- Verify `COPY` paths are correct relative to the expected `docker build` context
- Document required environment variables in `docs/configuration.md`
- Add `see-idp-web` service to `docker-compose.yml` wired to the IDP Dockerfile with the
  required environment variables (`ConnectionStrings__DefaultConnection`,
  `ConnectionStrings__Redis`, `OpenIddict__SigningKey`, `OpenIddict__EncryptionKey`,
  `Email__ApiKey`, `Email__FromAddress`); set `ASPNETCORE_ENVIRONMENT=Production`

## Implementation

**Status:** ✅ Done

**Files changed:**
- `src/See.Idp.Web/See.Idp.Web.csproj` — added
  `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` 10.0.5
- `src/See.Idp.Web/Program.cs` — added `AddHealthChecks().AddDbContextCheck<ApplicationDbContext>()`
  in DI registration; added `app.MapHealthChecks("/health")`
- `src/See.Idp.Web/Dockerfile` — installs `curl` (for HEALTHCHECK), adds all three `.csproj`
  files before `dotnet restore` so project references resolve correctly, adds
  `HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3`
- `docker-compose.yml` — added `see-idp-web` service (build context `./src`, Development
  environment, connection strings pointing to the compose postgres/redis services,
  OTLP endpoint pointing to otel-lgtm); resolves the broken `Oidc__Authority` reference
  in `client-web`
- `docs/configuration.md` — new file documenting all required and optional environment
  variables, key generation instructions, and local dev notes
