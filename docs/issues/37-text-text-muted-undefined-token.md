# 37 — `text-text-muted` references an undefined CSS custom property

## Context

Three pages in the Identity area use `text-text-muted`:

```
Areas/Identity/Pages/Account/LoginWith2fa.cshtml:7
Areas/Identity/Pages/Account/LoginWithRecoveryCode.cshtml:7
Areas/Identity/Pages/Account/Manage/EnableAuthenticator.cshtml:30
```

The Identity `@theme` block defines no `--color-text-muted` token. Tailwind v4
generates `color: var(--color-text-muted)` but since the custom property is never
declared, the browser discards the declaration. The text inherits `--color-app-fg`
rather than the intended muted treatment. The failure is silent — no build error,
just wrong output.

## Fix

Either define the token in `identity.css`:

```css
@theme {
    --color-muted: oklch(0.55 0.015 255);
}
```

and replace `text-text-muted` with `text-muted` on all three pages, or replace with
`text-app-fg/60` using Tailwind's opacity modifier syntax (no new token required).

## Acceptance Criteria

- `text-text-muted` does not appear anywhere in the codebase
- The replacement renders visibly lighter than body text
- `dotnet build` succeeds

## Dependencies

None.
