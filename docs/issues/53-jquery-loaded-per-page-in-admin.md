# 53 — jQuery loaded per page in Admin rather than in the layout

## Context

The Admin layout does not include jQuery globally. Each Admin page that requires
client-side validation loads it individually in `@section Scripts`:

```html
<!-- Clients/Create.cshtml, Clients/Edit.cshtml, Users/Edit.cshtml -->
@section Scripts {
<script src="~/lib/jquery/dist/jquery.min.js"></script>
<partial name="_ValidationScriptsPartial" />
}
```

The Identity layout loads jQuery globally at the bottom of `<body>` for all pages in
that area. Admin has no equivalent, requiring each form page to opt in manually.

If a developer adds a new Admin form page and omits the jQuery script tag, unobtrusive
validation silently breaks with no build-time warning.

## Fix

Add jQuery to `Areas/Admin/Pages/Shared/_Layout.cshtml` before
`@await RenderSectionAsync("Scripts", required: false)`:

```html
<script src="~/lib/jquery/dist/jquery.min.js"></script>
@await RenderSectionAsync("Scripts", required: false)
```

Remove the per-page jQuery `<script>` tags from `Clients/Create.cshtml`,
`Clients/Edit.cshtml`, and `Users/Edit.cshtml`.

## Acceptance Criteria

- jQuery is loaded once in the Admin layout
- No Admin page loads jQuery individually in `@section Scripts`
- Client-side validation continues to work on all Admin form pages

## Dependencies

None.
