# 44 — Active manage nav item has no `aria-current`

## Context

`_ManageNav.cshtml` uses visual styling to indicate the active page but provides no
semantic signal:

```html
<a asp-page="./Index"
   class="rounded-lg px-3 py-2 text-sm no-underline hover:bg-border-soft
          @(currentPage.EndsWith("/Index") ? "bg-border-soft font-medium" : "")">
  Profile
</a>
```

The active state is communicated only through colour and font weight. Screen readers
cannot distinguish the active link from inactive ones.

## Fix

Add `aria-current` conditionally on each link:

```html
<a asp-page="./Index"
   aria-current="@(currentPage.EndsWith("/Index") ? "page" : null)"
   class="...">
  Profile
</a>
```

Razor omits attributes whose value is `null`, so non-active links emit no attribute.

## Acceptance Criteria

- The active link in `_ManageNav.cshtml` has `aria-current="page"`
- Non-active links have no `aria-current` attribute

## Dependencies

#55 (active nav detection) — fix the `EndsWith` detection logic for both the CSS
class and `aria-current` at the same time.
