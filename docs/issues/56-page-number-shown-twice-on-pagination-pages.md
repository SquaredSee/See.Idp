# 56 — Page number shown twice on pagination pages

## Context

Both `Clients/Index.cshtml` and `Users/Index.cshtml` display the current page number
in two separate locations:

1. In the page header area:

    ```html
    <span class="text-sm text-slate-600">Page @Model.CurrentPage</span>
    ```

2. In the pagination footer:
    ```html
    <span class="text-sm text-slate-600">Page @Model.CurrentPage</span>
    ```

The header occurrence serves no purpose that the pagination footer does not already
provide. It adds noise without conveying additional information.

## Fix

Remove the `Page @Model.CurrentPage` span from the header area of both pages.
The pagination footer already shows the current page between the Previous and Next
controls, which is the conventional location.

## Acceptance Criteria

- The current page number appears exactly once per page, in the pagination footer
- The header area shows only the page title and action buttons

## Dependencies

None.
