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

## Dependencies

- `05-persistent-keys` — key configuration must be environment-variable-driven before
  the container is considered production-ready
- `06-data-protection-redis` — Redis connection must be injectable
