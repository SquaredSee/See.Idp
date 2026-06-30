# 39 — Root layout is an unstyled stub; Error and Privacy pages render with browser defaults

## Context

`Pages/Shared/_Layout.cshtml` links no stylesheet, has an empty `<nav>`, and applies
no typography or spacing. `Pages/Error.cshtml` and `Pages/Privacy.cshtml` both use
`Pages/_ViewStart.cshtml`, which points to this layout. Both pages render as raw,
unstyled HTML.

An unstyled error page is the page most likely to be seen by a distressed user.

## Fix

Two options:

**Option A** — Update `Pages/_ViewStart.cshtml` to point at the Identity area layout:

```csharp
@{
    Layout = "/Areas/Identity/Pages/Shared/_Layout.cshtml";
}
```

**Option B** — Give `Pages/Shared/_Layout.cshtml` a real implementation: link
`identity.out.css` and add the same header/footer shell used in the Identity area.

Once styled, update `Error.cshtml` to use `.panel` and consistent typography.

## Acceptance Criteria

- `Error.cshtml` and `Privacy.cshtml` render with the site stylesheet applied
- Both pages display a header and footer consistent with the Identity area

## Dependencies

None.
