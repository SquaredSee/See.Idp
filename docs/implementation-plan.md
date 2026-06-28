# Implementation Plan

Issues in dependency order. Each issue should be implemented, tested, the issue doc updated
with completion notes, and committed before moving to the next.

---

## Phase 1 — Core OIDC (done)

These make the IDP functional end-to-end for a basic auth code flow.

| #   | Issue                                                                      | Status  |
| --- | -------------------------------------------------------------------------- | ------- |
| 01  | [Authorization Controller & Claims](issues/01-authorization-controller.md) | ✅ done |
| 02  | [Userinfo Endpoint](issues/02-userinfo-endpoint.md)                        | ✅ done |
| 03  | [Implicit Consent for All Clients](issues/03-consent-page.md)              | ✅ done |

---

## Phase 2 — Security & Hardening

These make the IDP safe to expose and stable to run.

| #   | Issue                                                                         | Status  | Notes                             |
| --- | ----------------------------------------------------------------------------- | ------- | --------------------------------- |
| 04  | [Email Provider Integration](issues/04-email-provider.md)                     | ✅ done | No deps — do first; unblocks 10   |
| 07  | [CORS Configuration](issues/07-cors.md)                                       | ✅ done | Unblocks 11 (client app)          |
| 08  | [Rate Limiting](issues/08-rate-limiting.md)                                   | ✅ done | Auth controller must exist (done) |
| 05  | [Persistent Signing & Encryption Keys](issues/05-persistent-keys.md)          | ✅ done | Unblocks 06                       |
| 06  | [Data Protection & Redis Key Persistence](issues/06-data-protection-redis.md) | ✅ done | Needs 05 first                    |
| 10  | [Two-Factor Authentication (TOTP)](issues/10-two-factor-authentication.md)    | ✅ done | Needs 04 first                    |

---

## Phase 3 — Client Validation

Build a real consumer to validate the full OIDC flow end-to-end.

| #   | Issue                                                           | Status  | Notes                |
| --- | --------------------------------------------------------------- | ------- | -------------------- |
| 11  | [Client Test Application](issues/11-client-test-application.md) | ✅ done | Needs 01, 02, 03, 07 |

---

## Phase 4 — Deployment

Everything needed to run on a home Kubernetes cluster.

| #   | Issue                                                                    | Status | Notes           |
| --- | ------------------------------------------------------------------------ | ------ | --------------- |
| 09  | [Forwarded Headers & Transport Security](issues/09-https-enforcement.md) | ⬜     | Do before 12/13 |
| 12  | [Dockerfile Hardening](issues/12-dockerfile-hardening.md)                | ⬜     | Needs 05, 06    |
| 13  | [Kubernetes Manifests](issues/13-kubernetes.md)                          | ⬜     | Needs 09, 12    |

---

## Dependency graph

```
01 ✅ ─┬─ 02 ✅ ─── 07 ─── 11
       ├─ 03 ✅
       └─ 08

04 ────────────────────────── 10

05 ─── 06 ─┬─ 12 ─── 13
           └─ (12)

09 ──────────────────────────13
```
