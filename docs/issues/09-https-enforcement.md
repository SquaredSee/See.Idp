# 09 — Forwarded Headers & Transport Security

## Context

Containers should not handle TLS directly. HTTPS termination belongs at the ingress/reverse
proxy layer (nginx, Traefik, or a Kubernetes ingress controller), which forwards decrypted
traffic to the container over plain HTTP. However, the app still needs to know the original
request was HTTPS — OpenIddict enforces this and will reject requests it considers insecure.

The fix is `ForwardedHeaders` middleware, which teaches the app to trust
`X-Forwarded-Proto: https` and `X-Forwarded-For` headers set by the ingress. Once that is
in place, `DisableTransportSecurityRequirement()` can be removed from the production
OpenIddict config.

`UseHttpsRedirection` and `UseHsts` are explicitly **not** used — the ingress handles
redirects and HSTS at the edge.

## User Story

As the operator, I want the IDP to correctly identify incoming requests as HTTPS when
running behind an ingress, so that OpenIddict does not reject them and redirect URIs are
built with the correct scheme.

## Acceptance Criteria

- `ForwardedHeaders` middleware is registered and configured to trust `X-Forwarded-Proto`
  and `X-Forwarded-For`
- The app correctly identifies requests as HTTPS when the ingress sets `X-Forwarded-Proto: https`
- `DisableTransportSecurityRequirement()` is not called in production
- `UseHttpsRedirection` and `UseHsts` are not added — TLS is terminated at the ingress
- Configuration allows restricting which proxy IPs are trusted (important for security)
- Development workflow is unaffected

## Technical Notes

- Add `services.Configure<ForwardedHeadersOptions>(...)` in `Program.cs`:
  ```csharp
  builder.Services.Configure<ForwardedHeadersOptions>(options =>
  {
      options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
      // Lock down to the ingress IP range in production via options.KnownNetworks or KnownProxies
      // In Kubernetes, clearing KnownNetworks and KnownProxies with AllowedHosts lets any
      // in-cluster proxy through — acceptable when the cluster network is trusted
      options.KnownNetworks.Clear();
      options.KnownProxies.Clear();
  });
  ```
- Call `app.UseForwardedHeaders()` as the **first** middleware, before anything else
- Remove `aspNetCoreBuilder.DisableTransportSecurityRequirement()` from the production
  OpenIddict config — it is only needed in development
- The TLS certificate itself lives in the Kubernetes ingress (cert-manager + Let's Encrypt)
  and is configured in issue 13

## Dependencies

- `13-kubernetes` — the ingress that sets `X-Forwarded-Proto` is defined there
