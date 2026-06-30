# 36 — `.field-input` removes the browser focus ring without a replacement

## Context

Both CSS entry points define `.field-input` with `focus:outline-none`:

```css
/* identity.css and admin.css */
.field-input {
    @apply ... focus:border-brand-500 focus:outline-none;
}
```

`focus:outline-none` removes the browser default focus ring. The only remaining focus
indicator is a border colour change from `border-soft` to `brand-500`. This affects
every text input, password field, and textarea in the application — login, register,
2FA, change password, forgot password, reset password, and all admin forms.

A colour-change-only indicator does not meet WCAG 2.1 SC 1.4.11 (Non-text Contrast)
or WCAG 2.2 SC 2.4.11 (Focus Appearance). Keyboard users receive no reliable visual
indication of which field is focused.

## Fix

Add `focus:ring-2 focus:ring-brand-500 focus:ring-offset-1` to `.field-input` in both
`identity.css` and `admin.css`. Keep `focus:outline-none` to suppress the default
outline so the ring utility controls appearance exclusively.

## Acceptance Criteria

- `.field-input` in both CSS files includes a `focus:ring-*` replacement
- All form inputs display a visible focus ring on keyboard focus
- No other interactive element suppresses the focus ring without a visible replacement

## Dependencies

None.
