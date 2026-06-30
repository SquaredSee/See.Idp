# 42 — Validation summaries have no `role="alert"`

## Context

All form pages render an `asp-validation-summary` div that receives error content
on a failed server-side validation and page reload:

```html
<div
    asp-validation-summary="ModelOnly"
    class="mb-4 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700"
></div>
```

Without `role="alert"` the element is not a live region. When the page reloads with
errors, screen readers do not announce the new content. A keyboard-only or
screen-reader user who submits an invalid form receives no audible feedback that
the submission failed.

## Fix

Add `role="alert"` to every `asp-validation-summary` element across all form pages:

```html
<div asp-validation-summary="ModelOnly" role="alert" class="..."></div>
```

Affected pages: Login, Register, ForgotPassword, ResetPassword, LoginWith2fa,
LoginWithRecoveryCode, ChangePassword, EnableAuthenticator, Manage/Index,
Clients/Create, Clients/Edit, Users/Edit.

## Acceptance Criteria

- Every `asp-validation-summary` element has `role="alert"`
- On a failed form submission, screen readers announce the validation error

## Dependencies

None.
