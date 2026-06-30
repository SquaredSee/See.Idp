# 47 — Status alert pattern is copy-pasted as raw utilities across Admin pages

## Context

The success and error alert pattern appears as inline utility strings across five
Admin pages rather than as named component classes:

```html
<!-- Repeated verbatim (or near-verbatim) in:
     Clients/Index, Clients/Edit, Clients/Create, Users/Index, Users/Edit -->
"rounded-md border border-emerald-200 bg-emerald-50 px-3 py-2 text-sm
text-emerald-700" "rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm
text-red-700"
```

Some pages construct the class string via a Razor ternary; others hardcode it. The
warning variant (`border-yellow-200 bg-yellow-50`) also appears in the Manage pages.
None of these are extracted to `@layer components`.

Because the strings are copy-pasted, any visual change to the alert style must be
applied manually to every occurrence.

## Fix

Extract component classes in `admin.css` (and mirror in `identity.css`):

```css
@layer components {
    .alert-success {
        @apply rounded-md border border-emerald-200 bg-emerald-50 px-3 py-2 text-sm text-emerald-700;
    }
    .alert-error {
        @apply rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700;
    }
    .alert-warning {
        @apply rounded-md border border-yellow-200 bg-yellow-50 px-3 py-2 text-sm text-yellow-800;
    }
}
```

Replace all inline alert utility strings with these classes.

## Acceptance Criteria

- `.alert-success`, `.alert-error`, `.alert-warning` are defined in `@layer components`
  in both CSS entry points
- No inline alert utility strings remain in any `.cshtml` file

## Dependencies

#46 (token system) — once `emerald-*` and `amber-*` are replaced with tokens, update
the component definitions accordingly.
