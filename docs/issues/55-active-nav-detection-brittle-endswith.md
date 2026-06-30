# 55 — Active nav detection in `_ManageNav.cshtml` uses brittle string matching

## Context

The manage sidebar determines the active link by checking if the current page route
ends with a specific string:

```csharp
@(currentPage.EndsWith("/Index") ? "bg-border-soft font-medium" : "")
@(currentPage.EndsWith("/ChangePassword") ? "bg-border-soft font-medium" : "")
@(currentPage.EndsWith("/TwoFactorAuthentication") ? "bg-border-soft font-medium" : "")
```

This will silently break if a page is moved, renamed, or if a new page is added whose
name ends with an existing suffix. The match is also case-sensitive with no error if
the suffix is wrong.

## Fix

Compare against the full page route value:

```csharp
@(currentPage == "/Account/Manage/Index" ? "bg-border-soft font-medium" : "")
```

Or extract the active check to a local function in the partial to avoid repeating
the conditional across every link.

## Acceptance Criteria

- Active link detection does not use `EndsWith` or partial string matching
- The correct link is highlighted for each manage page

## Dependencies

#44 (aria-current) — fix the detection logic for both the CSS class and `aria-current`
at the same time.
