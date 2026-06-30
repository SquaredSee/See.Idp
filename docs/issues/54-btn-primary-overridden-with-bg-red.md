# 54 — Destructive buttons override `.btn-primary` with `bg-red-600` in markup

## Context

Two buttons in the Identity Manage area patch `.btn-primary` with inline utilities:

```html
<!-- DisableTwoFactorAuthentication.cshtml -->
<button type="submit" class="btn-primary bg-red-600 hover:bg-red-700">
    Disable 2FA
</button>

<!-- ResetAuthenticator.cshtml -->
<button type="submit" class="btn-primary bg-red-600 hover:bg-red-700">
    Reset authenticator
</button>
```

The Admin CSS already defines `.btn-danger` for exactly this purpose. The Identity
CSS does not, which is why `.btn-primary` is being patched. Overriding component
classes with inline utilities undermines the component system.

## Fix

Define `.btn-danger` in `identity.css`:

```css
@layer components {
    .btn-danger {
        @apply inline-flex items-center justify-center rounded-lg bg-red-600 px-4 py-2 text-sm font-semibold text-white hover:bg-red-500;
    }
}
```

Replace the patched classes on both buttons:

```html
<button type="submit" class="btn-danger">Disable 2FA</button>
<button type="submit" class="btn-danger">Reset authenticator</button>
```

## Acceptance Criteria

- `.btn-danger` is defined in `identity.css`
- No button in either area uses `btn-primary` with `bg-red-*` overrides

## Dependencies

None.
