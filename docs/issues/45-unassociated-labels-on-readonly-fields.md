# 45 — Read-only and display fields have unassociated labels

## Context

Several pages render labels with no programmatic association to their field.

**`Areas/Admin/Pages/Clients/Edit.cshtml`**:

```html
<label class="field-label">Client ID</label>
<input value="@Model.Input.ClientId" class="field-input" readonly />
```

No `for` attribute on the label; no `id` on the input.

**`Areas/Identity/Pages/Account/Manage/Index.cshtml`**:

```html
<label class="field-label">Email</label>
<input
    value="@Model.Username"
    class="field-input bg-slate-50 text-slate-500"
    disabled
/>
```

Same problem: no `for`/`id` pairing.

**`Areas/Admin/Pages/Users/Edit.cshtml`**:

```html
<span class="field-label">Email</span>
<p class="mt-1 text-sm">@(Model.Email ?? "(none)")</p>
```

A `<span>` cannot be a label element. The email value is display-only, not an input,
so a `<label>` is not appropriate here at all.

Screen readers announce these inputs without any context for their purpose.

## Fix

For read-only inputs, add explicit `id` and `for` pairing:

```html
<label for="client-id-display" class="field-label">Client ID</label>
<input
    id="client-id-display"
    value="@Model.Input.ClientId"
    class="field-input"
    readonly
/>
```

For display-only values, replace the `<span>` + `<p>` pattern with a description list:

```html
<dl>
    <dt class="field-label">Email</dt>
    <dd class="mt-1 text-sm">@(Model.Email ?? "(none)")</dd>
</dl>
```

## Acceptance Criteria

- Every visible `<input>` has a programmatically associated `<label>`
- No `<span>` is used in place of a `<label>` or heading
- Display-only key/value pairs use a semantic structure

## Dependencies

None.
