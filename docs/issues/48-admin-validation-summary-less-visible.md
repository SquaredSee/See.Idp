# 48 — Admin validation summary is less visible than the Identity equivalent

## Context

Identity form pages render a fully styled validation summary:

```html
<div
    asp-validation-summary="ModelOnly"
    class="mb-4 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700"
></div>
```

Admin form pages (`Clients/Create`, `Clients/Edit`, `Users/Edit`) render:

```html
<div asp-validation-summary="ModelOnly" class="text-sm text-red-600"></div>
```

The Admin version has no background, no border, and no padding. It is a bare colour
change that is easy to miss, especially on pages with multiple fields. Model-level
errors are the most likely to go unnoticed.

## Fix

Update the Admin validation summary divs to match the Identity styling:

```html
<div
    asp-validation-summary="ModelOnly"
    class="mb-4 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700"
></div>
```

Or, once #47 is resolved, use `.alert-error`.

## Acceptance Criteria

- Validation summaries in Admin form pages have equivalent visual weight to those in
  Identity pages (background, border, padding)

## Dependencies

#47 (alert component) — if resolved first, use `.alert-error` here instead.
