# 13 — Kubernetes Manifests

## Context

The long-term goal is to run this IDP (and applications that depend on it) on a home
Kubernetes cluster as a self-hosted development portfolio. This issue produces the
Kubernetes manifests needed to deploy the full stack: IDP, PostgreSQL, and Redis.

## User Story

As the operator, I want to deploy the IDP and its dependencies to a Kubernetes cluster
with a single `kubectl apply` so that I can run my own hosted identity infrastructure.

## Acceptance Criteria

- `k8s/` directory contains manifests for all components
- IDP `Deployment` with configurable replica count, resource limits, and liveness/readiness
  probes using the `/health` endpoint
- IDP `Service` (ClusterIP) and `Ingress` with TLS via cert-manager + Let's Encrypt
- PostgreSQL deployed via [CloudNativePG](https://cloudnative-pg.io/) operator
- Redis deployed as a `Deployment` with a `PersistentVolumeClaim`
- `ConfigMap` for non-sensitive configuration
- `Secret` for connection strings, API keys, and certificates
- No sensitive values in committed manifests
- A `README` in `k8s/` documents the deployment steps

## Technical Notes

- Use `cert-manager` with the `letsencrypt-prod` `ClusterIssuer` for automatic TLS
- Ingress annotation: `cert-manager.io/cluster-issuer: letsencrypt-prod`
- Configure `ForwardedHeaders` middleware in the IDP to trust the ingress IP range
  so `X-Forwarded-Proto: https` is respected (required for issue 09)
- Resource requests/limits starting point: `memory: 256Mi`, `cpu: 250m`
- Consider a `HorizontalPodAutoscaler` once Redis and persistent keys are in place
- CloudNativePG is recommended for PostgreSQL — handles backups, failover, and connection
  pooling with minimal configuration

## Dependencies

- `12-dockerfile-hardening`
- `09-https-enforcement`
- `06-data-protection-redis`
- `05-persistent-keys`
