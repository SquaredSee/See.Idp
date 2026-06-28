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

## Phase 3b — Admin Completeness

Fill gaps in the admin UI discovered during the post-Phase-3 audit. Neither issue blocks
deployment but both should be done before the admin area is considered complete.

| #   | Issue                                                                      | Status | Notes                          |
| --- | -------------------------------------------------------------------------- | ------ | ------------------------------ |
| 14  | [Admin: Client PostLogoutRedirectUris](issues/14-admin-client-post-logout-uris.md) | ✅ done | No deps; DTOs + 2 Razor pages |
| 15  | [Admin: User Detail & Edit Page](issues/15-admin-user-edit-page.md)        | ✅ done     | No deps; 3 service methods already exist |

---

## Phase 4 — Deployment

Everything needed to run on a home Kubernetes cluster. Issues 09 and 12 are independent of
each other and can be worked in parallel; 13 needs both.

| #   | Issue                                                                    | Status | Notes                                              |
| --- | ------------------------------------------------------------------------ | ------ | -------------------------------------------------- |
| 09  | [Forwarded Headers & Transport Security](issues/09-https-enforcement.md) | ⬜     | No deps on 12; unblocks 13                         |
| 12  | [Dockerfile Hardening](issues/12-dockerfile-hardening.md)                | ⬜     | Needs 05 ✅, 06 ✅; adds IDP to docker-compose     |
| 13  | [Kubernetes Manifests](issues/13-kubernetes.md)                          | ⬜     | Needs 09, 12                                       |

---

## Gap found during audit

`docker-compose.yml` references `http://see-idp-web:8080` in the `client-web` service but
no `see-idp-web` service is defined — `docker-compose up` produces a broken local stack.
Folded into issue 12's acceptance criteria (adds the IDP service to compose).

---

## Dependency graph

```
01 ✅ ─┬─ 02 ✅ ─── 07 ✅ ─── 11 ✅
       ├─ 03 ✅
       └─ 08 ✅

04 ✅ ─────────────────────────── 10 ✅

14 ⬜  (no deps)
15 ⬜  (no deps)

05 ✅ ─── 06 ✅ ─── 12 ⬜ ─── 13 ⬜

09 ⬜ ──────────────────────── 13 ⬜
```

Remaining work in dependency order:
1. **14** and **15** — parallel, no deps; complete the admin area
2. **09** and **12** — parallel, no dependency between them
3. **13** — after both 09 and 12
