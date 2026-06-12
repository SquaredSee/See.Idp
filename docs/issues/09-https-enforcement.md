# 09 — HTTPS Enforcement

## Context

HTTPS redirection and HSTS are not configured for production. An IDP transmitting
authentication tokens over plain HTTP is a critical security risk. OpenIddict's transport
security requirement is currently disabled via `DisableTransportSecurityRequirement()`,
which is only appropriate in development.

## User Story

As a user, I want all communication with the IDP to be encrypted so that my credentials
and tokens cannot be intercepted.

## Acceptance Criteria

- All HTTP requests in production are redirected to HTTPS
- HSTS headers are sent in production with a minimum `max-age` of 1 year
- `DisableTransportSecurityRequirement()` is only called in development
- The application is compatible with TLS termination at the ingress layer
- Development workflow is unaffected (HTTP still works locally)

## Technical Notes

- Add `app.UseHttpsRedirection()` and `app.UseHsts()` inside the `!IsDevelopment` branch
- HSTS is not needed in development (avoid `localhost` HSTS pollution)
- In Kubernetes, TLS will typically be terminated at the ingress (cert-manager + Let's Encrypt).
  Configure `UseForwardedHeaders` so the app sees `https` from the `X-Forwarded-Proto` header
  and does not double-redirect
- Add `ForwardedHeadersOptions` to trust the ingress's IP range

## Dependencies

- `13-kubernetes` — full HTTPS story comes together at the ingress level
