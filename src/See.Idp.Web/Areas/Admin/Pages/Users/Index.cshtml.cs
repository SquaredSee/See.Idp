using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using See.Idp.Web.Auth;

namespace See.Idp.Web.Areas.Admin.Pages.Users;

public sealed class IndexModel(UserManager<IdentityUser> userManager) : PageModel
{
    private static readonly DateTimeOffset LockoutUntil = DateTimeOffset.UtcNow.AddYears(100);

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
        if (string.IsNullOrWhiteSpace(userId))
        {
            SetStatusError("User id is required.");
            return RedirectToPage();
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            SetStatusError("User not found.");
            return RedirectToPage();
        }

        var currentUserId = userManager.GetUserId(User);
        var isAdmin = await userManager.IsInRoleAsync(user, Roles.Admin);

        if (isAdmin && string.Equals(user.Id, currentUserId, StringComparison.Ordinal))
        {
            SetStatusError("You cannot remove your own admin role.");
            return RedirectToPage();
        }

        IdentityResult result;
        if (isAdmin)
        {
            result = await userManager.RemoveFromRoleAsync(user, Roles.Admin);
            if (result.Succeeded)
            {
                SetStatusSuccess($"Removed admin role from {DisplayName(user)}.");
            }
        }
        else
        {
            result = await userManager.AddToRoleAsync(user, Roles.Admin);
            if (result.Succeeded)
            {
                SetStatusSuccess($"Granted admin role to {DisplayName(user)}.");
            }
        }

        if (!result.Succeeded)
        {
            SetStatusError(string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleLockAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            SetStatusError("User id is required.");
            return RedirectToPage();
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            SetStatusError("User not found.");
            return RedirectToPage();
        }

        var currentUserId = userManager.GetUserId(User);
        if (string.Equals(user.Id, currentUserId, StringComparison.Ordinal))
        {
            SetStatusError("You cannot lock your own account.");
            return RedirectToPage();
        }

        if (!user.LockoutEnabled)
        {
            user.LockoutEnabled = true;
            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                SetStatusError(string.Join(" ", updateResult.Errors.Select(e => e.Description)));
                return RedirectToPage();
            }
        }

        var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow;
        var result = await userManager.SetLockoutEndDateAsync(user, isLocked ? null : LockoutUntil);

        if (result.Succeeded)
        {
            SetStatusSuccess(
                isLocked ? $"Unlocked {DisplayName(user)}." : $"Locked {DisplayName(user)}."
            );
        }
        else
        {
            SetStatusError(string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            SetStatusError("User id is required.");
            return RedirectToPage();
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            SetStatusError("User not found.");
            return RedirectToPage();
        }

        var currentUserId = userManager.GetUserId(User);
        if (string.Equals(user.Id, currentUserId, StringComparison.Ordinal))
        {
            SetStatusError("You cannot delete your own account.");
            return RedirectToPage();
        }

        var isAdmin = await userManager.IsInRoleAsync(user, Roles.Admin);
        if (isAdmin)
        {
            var admins = await userManager.GetUsersInRoleAsync(Roles.Admin);
            if (admins.Count <= 1)
            {
                SetStatusError("Cannot delete the last admin user.");
                return RedirectToPage();
            }
        }

        var result = await userManager.DeleteAsync(user);
        if (result.Succeeded)
        {
            SetStatusSuccess($"Deleted {DisplayName(user)}.");
        }
        else
        {
            SetStatusError(string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var currentUserId = userManager.GetUserId(User);

        var users = await userManager
            .Users.OrderBy(u => u.Email)
            .ThenBy(u => u.UserName)
            .ToListAsync();

        foreach (var user in users)
        {
            var isAdmin = await userManager.IsInRoleAsync(user, Roles.Admin);
            var isLocked =
                user.LockoutEnabled
                && user.LockoutEnd.HasValue
                && user.LockoutEnd.Value > DateTimeOffset.UtcNow;

            Users.Add(
                new UserRow(
                    UserId: user.Id,
                    UserName: user.UserName,
                    Email: user.Email,
                    EmailConfirmed: user.EmailConfirmed,
                    IsAdmin: isAdmin,
                    IsLockedOut: isLocked,
                    IsCurrentUser: string.Equals(user.Id, currentUserId, StringComparison.Ordinal)
                )
            );
        }
    }

    private static string DisplayName(IdentityUser user) => user.Email ?? user.UserName ?? user.Id;

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
