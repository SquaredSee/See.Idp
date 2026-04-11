using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using See.Idp.Core.Services;

namespace See.Idp.Web.Areas.Admin.Pages.Users;

public sealed class IndexModel(IUserManagementService userManagementService) : PageModel
{
    public List<UserRow> Users { get; } = [];

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? StatusKind { get; set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostToggleAdminAsync(string userId)
    {
        var currentUserId = User.FindFirst("sub")?.Value;
        var result = await userManagementService.ToggleAdminAsync(userId, currentUserId);

        if (result.Succeeded)
            SetStatusSuccess(result.Message ?? "Admin role updated.");
        else
            SetStatusError(result.Error ?? "Unable to update admin role.");

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleLockAsync(string userId)
    {
        var currentUserId = User.FindFirst("sub")?.Value;
        var result = await userManagementService.ToggleLockAsync(userId, currentUserId);

        if (result.Succeeded)
            SetStatusSuccess(result.Message ?? "Lock state updated.");
        else
            SetStatusError(result.Error ?? "Unable to update lock state.");

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string userId)
    {
        var currentUserId = User.FindFirst("sub")?.Value;
        var result = await userManagementService.DeleteUserAsync(userId, currentUserId);

        if (result.Succeeded)
            SetStatusSuccess(result.Message ?? "User deleted.");
        else
            SetStatusError(result.Error ?? "Unable to delete user.");

        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var currentUserId = User.FindFirst("sub")?.Value;
        var users = await userManagementService.ListUsersAsync(currentUserId);

        foreach (var user in users)
        {
            Users.Add(
                new UserRow(
                    UserId: user.UserId,
                    UserName: user.UserName,
                    Email: user.Email,
                    EmailConfirmed: user.EmailConfirmed,
                    IsAdmin: user.IsAdmin,
                    IsLockedOut: user.IsLockedOut,
                    IsCurrentUser: user.IsCurrentUser
                )
            );
        }
    }

    private void SetStatusSuccess(string message)
    {
        StatusKind = "success";
        StatusMessage = message;
    }

    private void SetStatusError(string message)
    {
        StatusKind = "error";
        StatusMessage = message;
    }

    public sealed record UserRow(
        string UserId,
        string? UserName,
        string? Email,
        bool EmailConfirmed,
        bool IsAdmin,
        bool IsLockedOut,
        bool IsCurrentUser
    );
}
