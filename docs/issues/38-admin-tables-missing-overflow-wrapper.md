# 38 — Admin tables have no `overflow-x-auto` wrapper

## Context

Both admin list pages render a `<table class="table-admin">` with no containing
scroll wrapper:

```
Areas/Admin/Pages/Clients/Index.cshtml
Areas/Admin/Pages/Users/Index.cshtml
```

On viewports narrower than the table content width, the tables overflow and cause
horizontal scrolling of the entire page. The Users table is particularly wide — five
columns plus inline action forms (Edit, Toggle Admin, Toggle Lock, Delete) per row.

The admin portal is desktop-first but intended to work on mobile.

## Fix

Wrap each table:

```html
<div class="overflow-x-auto">
    <table class="table-admin">
        ...
    </table>
</div>
```

## Acceptance Criteria

- Both admin tables are wrapped in `overflow-x-auto` containers
- On a narrow viewport the table scrolls without affecting the page layout

## Dependencies

None.
