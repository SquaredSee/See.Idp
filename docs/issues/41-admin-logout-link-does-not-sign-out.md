# 41 — Admin logout link navigates via GET and does not sign the user out

## Context

The Admin layout renders logout as an anchor tag:

```html
<!-- Areas/Admin/Pages/Shared/_Layout.cshtml -->
<a
    asp-area="Identity"
    asp-page="/Account/Logout"
    class="btn-primary text-sm no-underline"
    >Logout</a
>
```

This navigates to `/Account/Logout` via GET. `LogoutModel.OnGet` does not sign the
user out — it immediately redirects to `/Account/Login`:

```csharp
public IActionResult OnGet()
{
    return RedirectToPage("/Account/Login", new { area = "Identity" });
}
```

The actual sign-out is performed only by `OnPostAsync`, which calls
`authService.SignOutAsync()`. An admin who clicks "Logout" is redirected to the
login page with their session still active. Navigating back or using a bookmarked
admin URL restores their authenticated session without requiring credentials.

## Fix

Replace the anchor with a form POST, matching the Identity layout pattern:

```html
<form method="post" asp-area="Identity" asp-page="/Account/Logout">
    <button type="submit" class="btn-primary text-sm">Logout</button>
</form>
```

## Acceptance Criteria

- The Admin layout logout control is a `<form method="post">`, not an `<a>` tag
- After logout, the admin session is terminated
- Navigating to a protected admin page after logout requires re-authentication

## Dependencies

None.
