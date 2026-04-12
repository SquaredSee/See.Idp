using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using See.Idp.Core.Dtos.Users;
using See.Idp.Core.Services.Users;

namespace See.Idp.Web.Areas.Admin.Pages.Users;

public sealed class IndexModel(
    IUserQueryService userQueryService,
    IUserCommandService userCommandService
) : PageModel
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
        var currentUserId = GetCurrentUserId();
        var result = await userCommandService.ToggleAdminAsync(
            new ToggleUserAdminCommand(userId, currentUserId)
        );

        if (result.Succeeded)
            SetStatusSuccess(result.Message ?? "Admin role updated.");
        else
            SetStatusError(result.Error ?? "Unable to update admin role.");

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleLockAsync(string userId)
    {
        var currentUserId = GetCurrentUserId();
        var result = await userCommandService.ToggleLockAsync(
            new ToggleUserLockCommand(userId, currentUserId)
        );

        if (result.Succeeded)
            SetStatusSuccess(result.Message ?? "Lock state updated.");
        else
            SetStatusError(result.Error ?? "Unable to update lock state.");

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string userId)
    {
        var currentUserId = GetCurrentUserId();
        var result = await userCommandService.DeleteUserAsync(
            new DeleteUserCommand(userId, currentUserId)
        );

        if (result.Succeeded)
            SetStatusSuccess(result.Message ?? "User deleted.");
        else
            SetStatusError(result.Error ?? "Unable to delete user.");

        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var currentUserId = GetCurrentUserId();
        var users = await userQueryService.ListUsersAsync(new ListUsersQuery());

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
                    IsCurrentUser: string.Equals(
                        user.UserId,
                        currentUserId,
                        StringComparison.Ordinal
                    )
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

    private string? GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
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
