# 20 — Admin/Users/Edit: command invoked from GET handler

## Context

`Admin/Users/Edit.OnGetGenerateConfirmationLinkAsync` calls
`registrationService.GenerateEmailConfirmationTokenAsync`, which persists a new
confirmation token to the database. That is a write operation behind an HTTP GET request,
triggered by an `<a>` tag in the view.

This was introduced by issue #17c, which renamed `OnPostGenerateConfirmationLinkAsync` to
`OnGetGenerateConfirmationLinkAsync` and replaced the form with an anchor tag. The rename
was a mistake — the operation is a command and must live behind a POST.

## Fix

In `Edit.cshtml.cs`, rename the handler back:

```csharp
public async Task<IActionResult> OnPostGenerateConfirmationLinkAsync(string userId)
```

In `Edit.cshtml`, replace the `<a>` tag with a form:

```html
<form method="post" asp-page-handler="GenerateConfirmationLink">
    <input type="hidden" name="userId" value="@Model.UserId" />
    <button type="submit" class="btn-primary">
        Generate confirmation link
    </button>
</form>
```

## Acceptance Criteria

- `OnPostGenerateConfirmationLinkAsync` exists; `OnGetGenerateConfirmationLinkAsync` does not
- The confirmation link button in `Edit.cshtml` is a `<form method="post">`, not an `<a>` tag
- `dotnet build` and `dotnet test` pass clean
