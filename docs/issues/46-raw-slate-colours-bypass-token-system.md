# 46 — Admin pages use raw `slate-*` colours instead of design tokens

## Context

The Admin `@theme` block defines semantic tokens: `--color-app-fg`, `--color-app-bg`,
`--color-surface`, `--color-border-soft`, `--color-brand-500/600`. Despite this, Admin
pages use Tailwind's default `slate` palette directly in approximately 20 places:

```html
<!-- sample occurrences across Clients/Index, Clients/Edit, Clients/Create,
     Users/Index, Users/Edit, Admin/Index, and the Admin layout footer -->
<p class="text-sm text-slate-600">...</p>
<div class="text-xs text-slate-500">@user.UserId</div>
<div class="border-t border-slate-200 pt-4">...</div>
```

Raw `slate-*` values bypass the token system. If the admin palette changes, these
references must be updated individually rather than by changing a single token.

## Fix

Define a muted text token in `admin.css`:

```css
@theme {
    --color-muted: oklch(0.5 0.015 230);
}
```

Replace occurrences:

| Raw class          | Replacement                                      |
| ------------------ | ------------------------------------------------ |
| `text-slate-600`   | `text-muted`                                     |
| `text-slate-500`   | `text-muted` or `text-app-fg/50`                 |
| `border-slate-200` | `border-border-soft` (already a token)           |
| `bg-slate-50`      | `bg-app-bg` or a new `--color-surface-alt` token |

## Acceptance Criteria

- No `text-slate-*`, `bg-slate-*`, or `border-slate-*` classes appear in any `.cshtml` file
- All muted/secondary text uses a token-based utility
- `dotnet build` succeeds and visual output is unchanged

## Dependencies

None.
