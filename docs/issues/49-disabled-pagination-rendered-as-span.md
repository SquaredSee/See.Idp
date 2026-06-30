# 49 — Disabled pagination rendered as `<span class="btn-primary">`

## Context

When a pagination direction is unavailable, both `Clients/Index.cshtml` and
`Users/Index.cshtml` render it as a styled span:

```html
<span class="btn-primary text-xs opacity-50">Previous</span>
<span class="btn-primary text-xs opacity-50">Next</span>
```

Using `.btn-primary` on a `<span>` is semantically misleading: the element is not
focusable, not keyboard-reachable, and is announced by screen readers as static text
rather than a control. There is no signal that this is a disabled navigation action.

## Fix

Use a disabled `<button>` to preserve interactive semantics while communicating the
disabled state:

```html
<button
    type="button"
    disabled
    class="btn-primary text-xs opacity-50 cursor-not-allowed"
>
    Previous
</button>
```

The `disabled` attribute prevents interaction, communicates state to assistive
technology, and is consistent with the active pagination being a clickable control.

## Acceptance Criteria

- Disabled pagination controls are `<button disabled>` elements, not `<span>` elements
- Active and disabled pagination controls have consistent visual weight

## Dependencies

None.
