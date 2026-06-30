# 43 — Navigation links are not in `<nav>` landmark elements

## Context

Both area headers contain navigation links inside `<div>` elements, not `<nav>` landmarks.

Admin layout:

```html
<!-- Areas/Admin/Pages/Shared/_Layout.cshtml -->
<div class="flex items-center gap-4">
    <!-- should be <nav> -->
    <a asp-area="Admin" asp-page="/Clients/Index">Clients</a>
    <a asp-area="Admin" asp-page="/Users/Index">Users</a>
</div>
```

Identity layout:

```html
<!-- Areas/Identity/Pages/Shared/_Layout.cshtml -->
<div class="flex items-center gap-4 text-sm">
    <!-- should be <nav> -->
    <a asp-area="Identity" asp-page="/Account/Login">Sign in</a>
    ...
</div>
```

Screen reader users navigating by landmark cannot reach site navigation.

## Fix

Replace the `<div>` wrappers around navigation links with `<nav aria-label="...">`:

```html
<!-- Admin layout -->
<nav aria-label="Admin navigation" class="flex items-center gap-4">
    <a asp-area="Admin" asp-page="/Clients/Index" class="text-sm">Clients</a>
    <a asp-area="Admin" asp-page="/Users/Index" class="text-sm">Users</a>
</nav>

<!-- Identity layout -->
<nav aria-label="Account" class="flex items-center gap-4 text-sm">...</nav>
```

## Acceptance Criteria

- Navigation links in both area layouts are wrapped in `<nav>` elements
- Each `<nav>` has a descriptive `aria-label`

## Dependencies

None.
